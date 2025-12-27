#!/usr/bin/env bash
set -euo pipefail

# CoreAxis demo: Gold purchase (no code changes) — CURL-only flow
# Base URL from ApiGateway Program.cs
BASE_URL="${BASE_URL:-http://localhost:5077}"
TENANT_ID="${TENANT_ID:-default}"

echo "BASE_URL=$BASE_URL"
echo "TENANT_ID=$TENANT_ID"
echo

###############################################################################
# 0) Helper: You can export tokens/ids from previous responses manually.
###############################################################################

###############################################################################
# 1) ADMIN/OPERATOR bootstrap (register + assign Admin role)
###############################################################################

echo "== 1) Register ADMIN user (returns token + userId) =="
curl -sS -X POST "$BASE_URL/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "nationalCode": "1111111111",
    "mobileNumber": "09120000001",
    "birthDate": "1990-01-01",
    "firstName": "Admin",
    "lastName": "User",
    "referralCode": null
  }'
echo -e "\n"

echo "Now set ADMIN_TOKEN and ADMIN_USER_ID from the response above."
echo "export ADMIN_TOKEN='...'"
echo "export ADMIN_USER_ID='...'"
echo

: "${ADMIN_TOKEN:?Set ADMIN_TOKEN}"
: "${ADMIN_USER_ID:?Set ADMIN_USER_ID}"

echo "== 1.1) List roles to find Admin roleId =="
curl -sS "$BASE_URL/api/roles" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "X-Tenant-Id: $TENANT_ID"
echo -e "\n"

echo "Now set ADMIN_ROLE_ID to the role with name == \"Admin\"."
echo "export ADMIN_ROLE_ID='...'"
echo
: "${ADMIN_ROLE_ID:?Set ADMIN_ROLE_ID}"

echo "== 1.2) Assign Admin role to ADMIN user =="
curl -sS -X POST "$BASE_URL/api/users/$ADMIN_USER_ID/roles" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d "{\"roleId\":\"$ADMIN_ROLE_ID\"}"
echo -e "\n"

###############################################################################
# 2) Create Dynamic Forms (Request + Confirm)
###############################################################################

echo "== 2) Create Request Form (collect user info + grams) =="
curl -sS -X POST "$BASE_URL/api/forms" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d '{
    "tenantId": "default",
    "title": "Gold Purchase - Request",
    "description": "Collect user info and requested grams",
    "schemaJson": "{\"version\": \"1.0\", \"title\": \"Gold Purchase - Request\", \"fields\": [{\"id\": \"firstName\", \"name\": \"firstName\", \"type\": \"Text\", \"label\": \"First Name\", \"isRequired\": true}, {\"id\": \"lastName\", \"name\": \"lastName\", \"type\": \"Text\", \"label\": \"Last Name\", \"isRequired\": true}, {\"id\": \"nationalCode\", \"name\": \"nationalCode\", \"type\": \"Text\", \"label\": \"National Code\", \"isRequired\": true}, {\"id\": \"mobileNumber\", \"name\": \"mobileNumber\", \"type\": \"Text\", \"label\": \"Mobile Number\", \"isRequired\": true}, {\"id\": \"grams\", \"name\": \"grams\", \"type\": \"Number\", \"label\": \"Grams of gold\", \"isRequired\": true}]}"
  }'
echo -e "\n"

echo "Set FORM_REQUEST_ID from the response (field: id)."
echo "export FORM_REQUEST_ID='...'"
echo
: "${FORM_REQUEST_ID:?Set FORM_REQUEST_ID}"

echo "== 2.1) Create Confirm Form (user confirms quote) =="
curl -sS -X POST "$BASE_URL/api/forms" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d '{
    "tenantId": "default",
    "title": "Gold Purchase - Confirm Quote",
    "description": "User confirms computed price",
    "schemaJson": "{\"version\": \"1.0\", \"title\": \"Gold Purchase - Confirm Quote\", \"fields\": [{\"id\": \"accept\", \"name\": \"accept\", \"type\": \"Checkbox\", \"label\": \"I confirm the price\", \"isRequired\": true}]}"
  }'
echo -e "\n"

echo "Set FORM_CONFIRM_ID from the response (field: id)."
echo "export FORM_CONFIRM_ID='...'"
echo
: "${FORM_CONFIRM_ID:?Set FORM_CONFIRM_ID}"

###############################################################################
# 3) API Manager: define external services (DummyJSON, NBP gold price, MathJS)
###############################################################################

echo "== 3) Create WebService: DummyJSON (https://dummyjson.com) =="
curl -sS -X POST "$BASE_URL/api/admin/apim/services" \
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
  }'
echo -e "\n"

echo "Set SVC_DUMMY_ID from the response (field: id)."
echo "export SVC_DUMMY_ID='...'"
echo
: "${SVC_DUMMY_ID:?Set SVC_DUMMY_ID}"

echo "== 3.1) Create Method: GET /users/1 =="
curl -sS -X POST "$BASE_URL/api/admin/apim/services/$SVC_DUMMY_ID/methods" \
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
  }'
echo -e "\n"

echo "Set METHOD_USER1_ID from response (field: id)."
echo "export METHOD_USER1_ID='...'"
echo
: "${METHOD_USER1_ID:?Set METHOD_USER1_ID}"

echo "== 3.2) Create WebService: NBP (https://api.nbp.pl) =="
curl -sS -X POST "$BASE_URL/api/admin/apim/services" \
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
  }'
echo -e "\n"

echo "Set SVC_NBP_ID from response (field: id)."
echo "export SVC_NBP_ID='...'"
echo
: "${SVC_NBP_ID:?Set SVC_NBP_ID}"

echo "== 3.3) Create Method: GET /api/cenyzlota?format=json =="
curl -sS -X POST "$BASE_URL/api/admin/apim/services/$SVC_NBP_ID/methods" \
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
  }'
echo -e "\n"

echo "Set METHOD_GOLD_ID from response (field: id)."
echo "export METHOD_GOLD_ID='...'"
echo
: "${METHOD_GOLD_ID:?Set METHOD_GOLD_ID}"

echo "== 3.4) Create WebService: MathJS (https://api.mathjs.org) =="
curl -sS -X POST "$BASE_URL/api/admin/apim/services" \
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
  }'
echo -e "\n"

echo "Set SVC_MATH_ID from response (field: id)."
echo "export SVC_MATH_ID='...'"
echo
: "${SVC_MATH_ID:?Set SVC_MATH_ID}"

echo "== 3.5) Create Method: GET /v4/?expr=... =="
curl -sS -X POST "$BASE_URL/api/admin/apim/services/$SVC_MATH_ID/methods" \
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
  }'
echo -e "\n"

echo "Set METHOD_MATH_ID from response (field: id)."
echo "export METHOD_MATH_ID='...'"
echo
: "${METHOD_MATH_ID:?Set METHOD_MATH_ID}"

###############################################################################
# 4) Mapping definitions (build expr = goldPrice * grams, then extract totalPrice)
###############################################################################

echo "== 4) Create Mapping: BuildExpr (request mapping for MathJS) =="
curl -sS -X POST "$BASE_URL/api/admin/mappings" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d '{ 
    "code": "MAP_GOLD_EXPR",
    "name": "Build MathJS expr: goldPrice * grams",
    "sourceSchemaRef": "workflow.context",
    "targetSchemaRef": "mathjs.query",
    "rulesJson": "[{\"target\":\"expr\",\"expression\":\"concat($.apis.FetchGoldPrice.response[0].cena,\\u0027*\\u0027,$.grams)\"}]"
  }'
echo -e "\n"

echo "Set MAP_EXPR_ID from response (field: id)."
echo "export MAP_EXPR_ID='...'"
echo
: "${MAP_EXPR_ID:?Set MAP_EXPR_ID}"

echo "== 4.1) Publish MAP_GOLD_EXPR =="
curl -sS -X POST "$BASE_URL/api/admin/mappings/$MAP_EXPR_ID/publish" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "X-Tenant-Id: $TENANT_ID"
echo -e "\n"

echo "== 4.2) Create Mapping: ExtractTotalPrice (response mapping for MathJS) =="
curl -sS -X POST "$BASE_URL/api/admin/mappings" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d '{
    "code": "MAP_TOTAL_PRICE",
    "name": "Extract totalPrice from MathJS response",
    "sourceSchemaRef": "mathjs.response",
    "targetSchemaRef": "workflow.context",
    "rulesJson": "[{\"target\":\"totalPrice\",\"expression\":\"$.response\"}]"
  }'
echo -e "\n"

echo "Set MAP_TOTAL_ID from response (field: id)."
echo "export MAP_TOTAL_ID='...'"
echo
: "${MAP_TOTAL_ID:?Set MAP_TOTAL_ID}"

echo "== 4.3) Publish MAP_TOTAL_PRICE =="
curl -sS -X POST "$BASE_URL/api/admin/mappings/$MAP_TOTAL_ID/publish" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "X-Tenant-Id: $TENANT_ID"
echo -e "\n"

###############################################################################
# 5) Workflow definition + publish
###############################################################################

echo "== 5) Create Workflow Definition =="
curl -sS -X POST "$BASE_URL/api/admin/workflows/definitions" \
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
  }"
echo -e "\n"

echo "Set WF_DEF_ID from response (field: id)."
echo "export WF_DEF_ID='...'"
echo
: "${WF_DEF_ID:?Set WF_DEF_ID}"

echo "== 5.1) Publish Workflow Definition =="
curl -sS -X POST "$BASE_URL/api/admin/workflows/definitions/$WF_DEF_ID/publish" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "X-Tenant-Id: $TENANT_ID"
echo -e "\n"

###############################################################################
# 6) Product + Version + bind to workflow + publish
###############################################################################

echo "== 6) Create Product (key=gold_buy) =="
curl -sS -X POST "$BASE_URL/api/admin/products" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d '{
    "key": "gold_buy",
    "name": "خرید طلا (Demo)",
    "description": "Demo product for end-to-end workflow test"
  }'
echo -e "\n"

echo "Set PRODUCT_ID from response (field: id)."
echo "export PRODUCT_ID='...'"
echo
: "${PRODUCT_ID:?Set PRODUCT_ID}"

echo "== 6.1) Create Product Version (v1 draft) =="
curl -sS -X POST "$BASE_URL/api/admin/products/$PRODUCT_ID/versions" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -d '{
    "versionNumber": 1,
    "status": "Draft",
    "metadataJson": "{\"demo\":true}"
  }'
echo -e "\n"

echo "Set VERSION_ID from response (field: id)."
echo "export VERSION_ID='...'"
echo
: "${VERSION_ID:?Set VERSION_ID}"

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
# 7) END-USER flow (register -> start -> submit forms)
###############################################################################

echo "== 7) Register END-USER (returns token + userId) =="
curl -sS -X POST "$BASE_URL/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "nationalCode": "2222222222",
    "mobileNumber": "09120000002",
    "birthDate": "1995-05-05",
    "firstName": "Ali",
    "lastName": "Ahmadi",
    "referralCode": null
  }'
echo -e "\n"

echo "Now set USER_TOKEN and USER_ID from the response above."
echo "export USER_TOKEN='...'"
echo "export USER_ID='...'"
echo
: "${USER_TOKEN:?Set USER_TOKEN}"
: "${USER_ID:?Set USER_ID}"

echo "== 7.1) Start Product (IMPORTANT: set grams in start context for this demo) =="
curl -sS -X POST "$BASE_URL/api/products/gold_buy/start" \
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
  }'
echo -e "\n"

echo "Set WORKFLOW_RUN_ID from response (field: workflowId)."
echo "export WORKFLOW_RUN_ID='...'"
echo
: "${WORKFLOW_RUN_ID:?Set WORKFLOW_RUN_ID}"

echo "== 7.2) Inspect workflow state/context =="
curl -sS "$BASE_URL/api/workflows/$WORKFLOW_RUN_ID" \
  -H "Authorization: Bearer $USER_TOKEN" \
  -H "X-Tenant-Id: $TENANT_ID"
echo -e "\n"

echo "== 7.3) Fetch Request Form schema =="
curl -sS "$BASE_URL/api/forms/$FORM_REQUEST_ID/schema" \
  -H "Authorization: Bearer $USER_TOKEN" \
  -H "X-Tenant-Id: $TENANT_ID"
echo -e "\n"

echo "== 7.4) Submit Request Form (resume from CollectOrder) =="
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

echo "== 7.5) Inspect workflow again (should pause at ConfirmQuote; contextJson should include totalPrice) =="
curl -sS "$BASE_URL/api/workflows/$WORKFLOW_RUN_ID" \
  -H "Authorization: Bearer $USER_TOKEN" \
  -H "X-Tenant-Id: $TENANT_ID"
echo -e "\n"

echo "== 7.6) Fetch Confirm Form schema =="
curl -sS "$BASE_URL/api/forms/$FORM_CONFIRM_ID/schema" \
  -H "Authorization: Bearer $USER_TOKEN" \
  -H "X-Tenant-Id: $TENANT_ID"
echo -e "\n"

echo "== 7.7) Submit Confirm Form (resume from ConfirmQuote) =="
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
curl -sS "$BASE_URL/api/tasks?assigneeType=Role&assigneeId=Admin" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "X-Tenant-Id: $TENANT_ID"
echo -e "\n"

echo "Pick the taskId from above and set TASK_ID."
echo "export TASK_ID='...'"
echo
: "${TASK_ID:?Set TASK_ID}"

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

echo "== 8.2) Final workflow status (should be Completed) =="
curl -sS "$BASE_URL/api/workflows/$WORKFLOW_RUN_ID" \
  -H "Authorization: Bearer $USER_TOKEN" \
  -H "X-Tenant-Id: $TENANT_ID"
echo -e "\n"

echo "DONE."
