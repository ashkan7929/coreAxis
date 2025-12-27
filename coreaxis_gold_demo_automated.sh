#!/usr/bin/env bash
set -u

# CoreAxis automated gold purchase flow
BASE_URL="${BASE_URL:-http://localhost:5077}"
TENANT_ID="${TENANT_ID:-default}"
FIXED_OTP="123456"

echo "BASE_URL=$BASE_URL"
echo "TENANT_ID=$TENANT_ID"
echo "FIXED_OTP=$FIXED_OTP"
echo

# Helper function to extract JSON string value
extract_json_value() {
    local json="$1"
    local key="$2"
    echo "$json" | grep -o "\"$key\":\"[^\"]*\"" | head -1 | cut -d'"' -f4
}

###############################################################################
# 1) ADMIN/OPERATOR bootstrap
###############################################################################

echo "== 1) Register ADMIN user =="
RESP=$(curl -sS -X POST "$BASE_URL/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "nationalCode": "1111111111",
    "mobileNumber": "09120000001",
    "birthDate": "13690101",
    "firstName": "Admin",
    "lastName": "User",
    "referralCode": null
  }')
echo "Response: $RESP"

# Check if we got a token directly (legacy) or need OTP
ADMIN_TOKEN=$(extract_json_value "$RESP" "token")

if [ -z "$ADMIN_TOKEN" ]; then
    echo "Token not found in register response. Sending OTP for Login..."
    curl -sS -X POST "$BASE_URL/api/auth/send-otp" \
      -H "Content-Type: application/json" \
      -d "{ \"mobileNumber\": \"09120000001\", \"purpose\": \"Login\" }"
      
    echo "Attempting Login with OTP..."
    RESP=$(curl -sS -X POST "$BASE_URL/api/auth/login-with-otp" \
      -H "Content-Type: application/json" \
      -d "{
        \"mobileNumber\": \"09120000001\",
        \"otpCode\": \"$FIXED_OTP\"
      }")
    echo "Login Response: $RESP"
    ADMIN_TOKEN=$(extract_json_value "$RESP" "token")
fi

# Extract User ID (it might be in 'userId' from register or 'id' inside 'user' from login)
# We try 'userId' first (register response), then 'id' (login response)
ADMIN_USER_ID=$(extract_json_value "$RESP" "userId")
if [ -z "$ADMIN_USER_ID" ]; then
     ADMIN_USER_ID=$(extract_json_value "$RESP" "id")
fi

if [ -z "$ADMIN_TOKEN" ] || [ -z "$ADMIN_USER_ID" ]; then
    echo "Error: Failed to register/login admin user."
    exit 1
fi

export ADMIN_TOKEN
export ADMIN_USER_ID
echo "ADMIN_TOKEN=$ADMIN_TOKEN"
echo "ADMIN_USER_ID=$ADMIN_USER_ID"
echo

echo "== 1.1) List roles to find Admin roleId =="
RESP=$(curl -sS "$BASE_URL/api/roles" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "X-Tenant-Id: $TENANT_ID")
echo "Response: $RESP"

# Extract ID for Admin role. 
# We look for "name":"Admin" and try to find the "id" associated with it.
# This regex looks for: {"id":"UUID","name":"Admin"
ADMIN_ROLE_ID=$(echo "$RESP" | sed -n 's/.*"id":"\([^"]*\)","name":"Admin".*/\1/p')

if [ -z "$ADMIN_ROLE_ID" ]; then
    # Try alternate order: {"name":"Admin","id":"UUID"
    ADMIN_ROLE_ID=$(echo "$RESP" | sed -n 's/.*"name":"Admin".*"id":"\([^"]*\)".*/\1/p')
fi

# Fallback: if regex failed, try to just grab the first ID if it looks like Admin role is first or only one.
if [ -z "$ADMIN_ROLE_ID" ]; then
    # HACK: Just grab the first ID found.
    ADMIN_ROLE_ID=$(extract_json_value "$RESP" "id")
fi

if [ -z "$ADMIN_ROLE_ID" ]; then
    echo "Error: Failed to find Admin role ID."
    exit 1
fi

export ADMIN_ROLE_ID
echo "ADMIN_ROLE_ID=$ADMIN_ROLE_ID"
echo

echo "== 1.2) Assign Admin role to ADMIN user =="
curl -sS -X POST "$BASE_URL/api/users/$ADMIN_USER_ID/roles" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d "{\"roleId\":\"$ADMIN_ROLE_ID\"}"
echo -e "\n"

###############################################################################
# 2) Create Dynamic Forms
###############################################################################

echo "== 2) Create Request Form =="
RESP=$(curl -sS -X POST "$BASE_URL/api/forms" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d '{
    "tenantId": "default",
    "name": "Gold Purchase - Request",
    "description": "Collect user info and requested grams",
    "schemaJson": "{\"version\": \"1.0\", \"title\": \"Gold Purchase - Request\", \"fields\": [{\"id\": \"firstName\", \"name\": \"firstName\", \"type\": \"Text\", \"label\": \"First Name\", \"isRequired\": true}, {\"id\": \"lastName\", \"name\": \"lastName\", \"type\": \"Text\", \"label\": \"Last Name\", \"isRequired\": true}, {\"id\": \"nationalCode\", \"name\": \"nationalCode\", \"type\": \"Text\", \"label\": \"National Code\", \"isRequired\": true}, {\"id\": \"mobileNumber\", \"name\": \"mobileNumber\", \"type\": \"Text\", \"label\": \"Mobile Number\", \"isRequired\": true}, {\"id\": \"grams\", \"name\": \"grams\", \"type\": \"Number\", \"label\": \"Grams of gold\", \"isRequired\": true}]}"
  }')
echo "Response: $RESP"
FORM_REQUEST_ID=$(extract_json_value "$RESP" "id")
export FORM_REQUEST_ID
echo "FORM_REQUEST_ID=$FORM_REQUEST_ID"
echo

echo "== 2.1) Create Confirm Form =="
RESP=$(curl -sS -X POST "$BASE_URL/api/forms" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d '{
    "tenantId": "default",
    "title": "Gold Purchase - Confirm Quote",
    "description": "User confirms computed price",
    "schemaJson": "{\"version\": \"1.0\", \"title\": \"Gold Purchase - Confirm Quote\", \"fields\": [{\"id\": \"accept\", \"name\": \"accept\", \"type\": \"Checkbox\", \"label\": \"I confirm the price\", \"isRequired\": true}]}"
  }')
echo "Response: $RESP"
FORM_CONFIRM_ID=$(extract_json_value "$RESP" "id")
export FORM_CONFIRM_ID
echo "FORM_CONFIRM_ID=$FORM_CONFIRM_ID"
echo

###############################################################################
# 3) API Manager: define external services
###############################################################################

echo "== 3) Create WebService: DummyJSON =="
RESP=$(curl -sS -X POST "$BASE_URL/api/admin/apim/services" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d '{
    "name": "DummyJSON",
    "baseUrl": "https://dummyjson.com",
    "description": "Fake user data",
    "type": "REST",
    "ownerTenantId": "default",
    "securityProfileId": null
  }')
echo "Response: $RESP"
SVC_DUMMY_ID=$(extract_json_value "$RESP" "id")
export SVC_DUMMY_ID
echo "SVC_DUMMY_ID=$SVC_DUMMY_ID"
echo

echo "== 3.1) Create Method: GET /users/1 =="
RESP=$(curl -sS -X POST "$BASE_URL/api/admin/apim/services/$SVC_DUMMY_ID/methods" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d '{
    "name": "GetUser1",
    "path": "/users/1",
    "httpMethod": "GET",
    "description": "Get a single dummy user",
    "timeoutSeconds": 30,
    "cacheTtlSeconds": 0,
    "parameters": []
  }')
echo "Response: $RESP"
METHOD_USER1_ID=$(extract_json_value "$RESP" "id")
export METHOD_USER1_ID
echo "METHOD_USER1_ID=$METHOD_USER1_ID"
echo

echo "== 3.2) Create WebService: NBP =="
RESP=$(curl -sS -X POST "$BASE_URL/api/admin/apim/services" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d '{
    "name": "NBP",
    "baseUrl": "https://api.nbp.pl",
    "description": "NBP Web API (gold prices)",
    "type": "REST",
    "ownerTenantId": "default",
    "securityProfileId": null
  }')
echo "Response: $RESP"
SVC_NBP_ID=$(extract_json_value "$RESP" "id")
export SVC_NBP_ID
echo "SVC_NBP_ID=$SVC_NBP_ID"
echo

echo "== 3.3) Create Method: GET /api/cenyzlota?format=json =="
RESP=$(curl -sS -X POST "$BASE_URL/api/admin/apim/services/$SVC_NBP_ID/methods" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d '{
    "name": "GetGoldPrice",
    "path": "/api/cenyzlota",
    "httpMethod": "GET",
    "description": "Current gold price (1g) from NBP",
    "timeoutSeconds": 30,
    "cacheTtlSeconds": 0,
    "parameters": [
      { "name": "format", "dataType": "string", "isRequired": false, "defaultValue": "json", "location": "Query", "description": "force JSON" }
    ]
  }')
echo "Response: $RESP"
METHOD_GOLD_ID=$(extract_json_value "$RESP" "id")
export METHOD_GOLD_ID
echo "METHOD_GOLD_ID=$METHOD_GOLD_ID"
echo

echo "== 3.4) Create WebService: MathJS =="
RESP=$(curl -sS -X POST "$BASE_URL/api/admin/apim/services" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d '{
    "name": "MathJS",
    "baseUrl": "https://api.mathjs.org",
    "description": "Evaluate expressions",
    "type": "REST",
    "ownerTenantId": "default",
    "securityProfileId": null
  }')
echo "Response: $RESP"
SVC_MATH_ID=$(extract_json_value "$RESP" "id")
export SVC_MATH_ID
echo "SVC_MATH_ID=$SVC_MATH_ID"
echo

echo "== 3.5) Create Method: GET /v4/?expr=... =="
RESP=$(curl -sS -X POST "$BASE_URL/api/admin/apim/services/$SVC_MATH_ID/methods" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d '{
    "name": "EvalExpr",
    "path": "/v4/",
    "httpMethod": "GET",
    "description": "Evaluate expression (expr query param)",
    "timeoutSeconds": 30,
    "cacheTtlSeconds": 0,
    "parameters": [
      { "name": "expr", "dataType": "string", "isRequired": true, "defaultValue": null, "location": "Query", "description": "url-encoded math expression" }
    ]
  }')
echo "Response: $RESP"
METHOD_MATH_ID=$(extract_json_value "$RESP" "id")
export METHOD_MATH_ID
echo "METHOD_MATH_ID=$METHOD_MATH_ID"
echo

###############################################################################
# 4) Mapping definitions
###############################################################################

echo "== 4) Create Mapping: BuildExpr =="
RESP=$(curl -sS -X POST "$BASE_URL/api/admin/mappings" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d '{ 
    "code": "MAP_GOLD_EXPR",
    "name": "Build MathJS expr: goldPrice * grams",
    "sourceSchemaRef": "workflow.context",
    "targetSchemaRef": "mathjs.query",
    "rulesJson": "[{\"target\":\"expr\",\"expression\":\"concat($.apis.FetchGoldPrice.response[0].cena,\\u0027*\\u0027,$.grams)\"}]"
  }')
echo "Response: $RESP"
MAP_EXPR_ID=$(extract_json_value "$RESP" "id")
export MAP_EXPR_ID
echo "MAP_EXPR_ID=$MAP_EXPR_ID"
echo

echo "== 4.1) Publish MAP_GOLD_EXPR =="
curl -sS -X POST "$BASE_URL/api/admin/mappings/$MAP_EXPR_ID/publish" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "X-Tenant-Id: $TENANT_ID"
echo -e "\n"

echo "== 4.2) Create Mapping: ExtractTotalPrice =="
RESP=$(curl -sS -X POST "$BASE_URL/api/admin/mappings" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d '{
    "code": "MAP_TOTAL_PRICE",
    "name": "Extract totalPrice from MathJS response",
    "sourceSchemaRef": "mathjs.response",
    "targetSchemaRef": "workflow.context",
    "rulesJson": "[{\"target\":\"totalPrice\",\"expression\":\"$.response\"}]"
  }')
echo "Response: $RESP"
MAP_TOTAL_ID=$(extract_json_value "$RESP" "id")
export MAP_TOTAL_ID
echo "MAP_TOTAL_ID=$MAP_TOTAL_ID"
echo

echo "== 4.3) Publish MAP_TOTAL_PRICE =="
curl -sS -X POST "$BASE_URL/api/admin/mappings/$MAP_TOTAL_ID/publish" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "X-Tenant-Id: $TENANT_ID"
echo -e "\n"

###############################################################################
# 5) Workflow definition + publish
###############################################################################

echo "== 5) Create Workflow Definition =="
RESP=$(curl -sS -X POST "$BASE_URL/api/admin/workflows/definitions" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d "{
    \"code\": \"gold_purchase_demo\",
    \"name\": \"Gold Purchase Demo\",
    \"description\": \"Collect info, fetch gold price, compute total, confirm, approve\",
    \"dsl\": {
      \"version\": \"1.0\",
      \"startAt\": \"CollectOrder\",
      \"steps\": [
        { \"id\": \"CollectOrder\", \"name\": \"Collect Order\", \"type\": \"FormStep\",
          \"config\": { \"formId\": \"$FORM_REQUEST_ID\" },
          \"transitions\": [{ \"to\": \"FetchUser\" }]
        },
        { \"id\": \"FetchUser\", \"name\": \"Fetch Dummy User\", \"type\": \"ServiceTaskStep\",
          \"config\": { \"serviceMethodId\": \"$METHOD_USER1_ID\" },
          \"transitions\": [{ \"to\": \"FetchGoldPrice\" }]
        },
        { \"id\": \"FetchGoldPrice\", \"name\": \"Fetch Gold Price\", \"type\": \"ServiceTaskStep\",
          \"config\": { \"serviceMethodId\": \"$METHOD_GOLD_ID\" },
          \"transitions\": [{ \"to\": \"CalcTotal\" }]
        },
        { \"id\": \"CalcTotal\", \"name\": \"Calculate Total\", \"type\": \"ServiceTaskStep\",
          \"config\": {
            \"serviceMethodId\": \"$METHOD_MATH_ID\",
            \"requestMappingId\": \"$MAP_EXPR_ID\",
            \"responseMappingId\": \"$MAP_TOTAL_ID\"
          },
          \"transitions\": [{ \"to\": \"ConfirmQuote\" }]
        },
        { \"id\": \"ConfirmQuote\", \"name\": \"Confirm Quote\", \"type\": \"FormStep\",
          \"config\": { \"formId\": \"$FORM_CONFIRM_ID\" },
          \"transitions\": [{ \"to\": \"ApproveOrder\" }]
        },
        { \"id\": \"ApproveOrder\", \"name\": \"Approve Order\", \"type\": \"HumanTaskStep\",
          \"config\": { \"assigneeType\": \"Role\", \"assigneeId\": \"Admin\", \"title\": \"Approve gold order\" },
          \"transitions\": [{ \"to\": \"Done\" }]
        },
        { \"id\": \"Done\", \"name\": \"Done\", \"type\": \"EndStep\", \"config\": {} }
      ]
    },
    \"ownerTenantId\": \"default\"
  }")
echo "Response: $RESP"
WF_DEF_ID=$(extract_json_value "$RESP" "id")
export WF_DEF_ID
echo "WF_DEF_ID=$WF_DEF_ID"
echo

echo "== 5.1) Publish Workflow Definition =="
curl -sS -X POST "$BASE_URL/api/admin/workflows/definitions/$WF_DEF_ID/publish" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "X-Tenant-Id: $TENANT_ID"
echo -e "\n"

###############################################################################
# 6) Product + Version + bind to workflow + publish
###############################################################################

echo "== 6) Create Product =="
RESP=$(curl -sS -X POST "$BASE_URL/api/admin/products" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d '{
    "key": "gold_buy",
    "name": "خرید طلا (Demo)",
    "description": "Demo product for end-to-end workflow test"
  }')
echo "Response: $RESP"
PRODUCT_ID=$(extract_json_value "$RESP" "id")
export PRODUCT_ID
echo "PRODUCT_ID=$PRODUCT_ID"
echo

echo "== 6.1) Create Product Version =="
RESP=$(curl -sS -X POST "$BASE_URL/api/admin/products/$PRODUCT_ID/versions" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d '{
    "versionNumber": 1,
    "status": "Draft",
    "metadataJson": "{\"demo\":true}"
  }')
echo "Response: $RESP"
VERSION_ID=$(extract_json_value "$RESP" "id")
export VERSION_ID
echo "VERSION_ID=$VERSION_ID"
echo

echo "== 6.2) Bind Product Version to Workflow Definition =="
curl -sS -X PUT "$BASE_URL/api/admin/products/$PRODUCT_ID/versions/$VERSION_ID" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d "{
    \"productId\": \"$PRODUCT_ID\",
    \"versionNumber\": 1,
    \"status\": \"Draft\",
    \"metadataJson\": \"{\\\"demo\\\":true}\",

    \"workflowDefinitionId\": \"$WF_DEF_ID\",
    \"workflowDefinitionCode\": \"gold_purchase_demo\",
    \"workflowVersionNumber\": \"1\",

    \"workflowId\": \"$WF_DEF_ID\",
    \"initialFormId\": \"$FORM_REQUEST_ID\",
    \"formId\": \"$FORM_REQUEST_ID\"
  }"
echo -e "\n"

echo "== 6.3) Publish Product Version =="
curl -sS -X POST "$BASE_URL/api/admin/products/$PRODUCT_ID/versions/$VERSION_ID/publish" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "X-Tenant-Id: $TENANT_ID"
echo -e "\n"

###############################################################################
# 7) END-USER flow
###############################################################################

echo "== 7) Register END-USER =="
RESP=$(curl -sS -X POST "$BASE_URL/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "nationalCode": "2222222222",
    "mobileNumber": "09120000002",
    "birthDate": "13740215",
    "firstName": "Ali",
    "lastName": "Ahmadi",
    "referralCode": null
  }')
echo "Response: $RESP"

# Check if we got a token directly or need OTP
USER_TOKEN=$(extract_json_value "$RESP" "token")

if [ -z "$USER_TOKEN" ]; then
    echo "Token not found in register response. Sending OTP for Login..."
    curl -sS -X POST "$BASE_URL/api/auth/send-otp" \
      -H "Content-Type: application/json" \
      -d "{ \"mobileNumber\": \"09120000002\", \"purpose\": \"Login\" }"

    echo "Attempting Login with OTP..."
    RESP=$(curl -sS -X POST "$BASE_URL/api/auth/login-with-otp" \
      -H "Content-Type: application/json" \
      -d "{
        \"mobileNumber\": \"09120000002\",
        \"otpCode\": \"$FIXED_OTP\"
      }")
    echo "Login Response: $RESP"
    USER_TOKEN=$(extract_json_value "$RESP" "token")
fi

USER_ID=$(extract_json_value "$RESP" "userId")
if [ -z "$USER_ID" ]; then
     USER_ID=$(extract_json_value "$RESP" "id")
fi

if [ -z "$USER_TOKEN" ] || [ -z "$USER_ID" ]; then
    echo "Error: Failed to register/login end user."
    exit 1
fi

export USER_TOKEN
export USER_ID
echo "USER_TOKEN=$USER_TOKEN"
echo "USER_ID=$USER_ID"
echo

echo "== 7.1) Start Product =="
RESP=$(curl -sS -X POST "$BASE_URL/api/products/gold_buy/start" \
  -H "Authorization: Bearer $USER_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d '{
    "context": {
      "grams": 5,
      "firstName": "Ali",
      "lastName": "Ahmadi",
      "nationalCode": "2222222222",
      "mobileNumber": "09120000002"
    }
  }')
echo "Response: $RESP"
# Note: response has "workflowId" field for the run ID
WORKFLOW_RUN_ID=$(extract_json_value "$RESP" "workflowId")
export WORKFLOW_RUN_ID
echo "WORKFLOW_RUN_ID=$WORKFLOW_RUN_ID"
echo

echo "== 7.2) Inspect workflow state/context =="
curl -sS "$BASE_URL/api/workflows/$WORKFLOW_RUN_ID" \
  -H "Authorization: Bearer $USER_TOKEN" \
  -H "X-Tenant-Id: $TENANT_ID"
echo -e "\n"

echo "== 7.4) Submit Request Form =="
curl -sS -X POST "$BASE_URL/api/submissions" \
  -H "Authorization: Bearer $USER_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d "{
    \"formId\": \"$FORM_REQUEST_ID\",
    \"userId\": \"$USER_ID\",
    \"submissionData\": {
      \"firstName\": \"Ali\",
      \"lastName\": \"Ahmadi\",
      \"nationalCode\": \"2222222222\",
      \"mobileNumber\": \"09120000002\",
      \"grams\": 5
    },
    \"validateBeforeSubmit\": false,
    \"metadata\": {
      \"workflowRunId\": \"$WORKFLOW_RUN_ID\",
      \"stepKey\": \"CollectOrder\"
    }
  }"
echo -e "\n"

echo "== 7.5) Inspect workflow again =="
curl -sS "$BASE_URL/api/workflows/$WORKFLOW_RUN_ID" \
  -H "Authorization: Bearer $USER_TOKEN" \
  -H "X-Tenant-Id: $TENANT_ID"
echo -e "\n"

echo "== 7.7) Submit Confirm Form =="
curl -sS -X POST "$BASE_URL/api/submissions" \
  -H "Authorization: Bearer $USER_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d "{
    \"formId\": \"$FORM_CONFIRM_ID\",
    \"userId\": \"$USER_ID\",
    \"submissionData\": { \"accept\": true },
    \"validateBeforeSubmit\": false,
    \"metadata\": {
      \"workflowRunId\": \"$WORKFLOW_RUN_ID\",
      \"stepKey\": \"ConfirmQuote\"
    }
  }"
echo -e "\n"

###############################################################################
# 8) OPERATOR completes HumanTask
###############################################################################

echo "== 8) List tasks for role Admin =="
RESP=$(curl -sS "$BASE_URL/api/tasks?assigneeType=Role&assigneeId=Admin" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "X-Tenant-Id: $TENANT_ID")
echo "Response: $RESP"

# Extract first task ID
TASK_ID=$(echo "$RESP" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
export TASK_ID
echo "TASK_ID=$TASK_ID"
echo

if [ -z "$TASK_ID" ]; then
    echo "Error: No task found for Admin."
    exit 1
fi

echo "== 8.1) Complete task (Approved) =="
curl -sS -X POST "$BASE_URL/api/tasks/$TASK_ID/complete" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d '{
    "outcome": "Approved",
    "payload": { "approved": true }
  }'
echo -e "\n"

echo "== 8.2) Final workflow status =="
curl -sS "$BASE_URL/api/workflows/$WORKFLOW_RUN_ID" \
  -H "Authorization: Bearer $USER_TOKEN" \
  -H "X-Tenant-Id: $TENANT_ID"
echo -e "\n"

echo "DONE."
