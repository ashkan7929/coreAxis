#!/bin/bash

# Base URL
URL="http://localhost:5077"

# 1. Create Request Form
echo "Creating Request Form..."
REQ_FORM_RES=$(curl -s -X POST "$URL/api/forms" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: default" \
  -d '{
    "tenantId": "default",
    "name": "GoldPurchaseRequest_v4",
    "title": "Gold Purchase - Request",
    "description": "Collect user info and requested grams",
    "schemaJson": "{\"version\": \"1.0\", \"title\": \"Gold Purchase - Request\", \"fields\": [{\"id\": \"grams\", \"name\": \"grams\", \"type\": \"Number\", \"label\": \"Gold Amount (grams)\", \"isRequired\": true}]}"
  }')
echo "Request Form Response: $REQ_FORM_RES"
REQ_FORM_ID=$(echo $REQ_FORM_RES | grep -o '"id":"[^"]*' | cut -d'"' -f4)
echo "Request Form ID: $REQ_FORM_ID"

# 2. Create Confirm Form
echo "\nCreating Confirm Form..."
CONF_FORM_RES=$(curl -s -X POST "$URL/api/forms" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: default" \
  -d '{
    "tenantId": "default",
    "name": "GoldPurchaseConfirm_v4",
    "title": "Gold Purchase - Confirm Quote",
    "description": "User confirms computed price",
    "schemaJson": "{\"version\": \"1.0\", \"title\": \"Gold Purchase - Confirm Quote\", \"fields\": [{\"id\": \"accept\", \"name\": \"accept\", \"type\": \"Checkbox\", \"label\": \"I confirm the price\", \"isRequired\": true}]}"
  }')
echo "Confirm Form Response: $CONF_FORM_RES"
CONF_FORM_ID=$(echo $CONF_FORM_RES | grep -o '"id":"[^"]*' | cut -d'"' -f4)
echo "Confirm Form ID: $CONF_FORM_ID"

# 3. Create Workflow Definition
echo "\nCreating Workflow Definition..."
WORKFLOW_DEF_RES=$(curl -s -X POST "$URL/api/admin/workflows" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "gold_purchase_v4",
    "name": "Gold Purchase Workflow v4",
    "description": "Gold purchase workflow without auth"
  }')
echo "Workflow Def Response: $WORKFLOW_DEF_RES"
WORKFLOW_ID=$(echo $WORKFLOW_DEF_RES | grep -o '"id":"[^"]*' | cut -d'"' -f4)
echo "Workflow ID: $WORKFLOW_ID"

if [ -z "$WORKFLOW_ID" ]; then
  echo "Failed to create workflow definition"
  exit 1
fi

# 4. Create Workflow Version (DSL)
echo "\nCreating Workflow Version..."
# Construct DSL JSON with actual Form IDs
DSL_JSON=$(cat <<EOF
{
  "startAt": "RequestForm",
  "steps": [
    {
      "id": "RequestForm",
      "type": "FormStep",
      "name": "Request Gold",
      "config": {
        "formId": "$REQ_FORM_ID",
        "assignee": "user"
      },
      "transitions": [
        { "to": "ComputePrice" }
      ]
    },
    {
      "id": "ComputePrice",
      "type": "CalculationStep",
      "name": "Compute Price",
      "config": {
        "expression": "grams * 10000",
        "outputVariable": "price"
      },
      "transitions": [
        { "to": "ConfirmForm" }
      ]
    },
    {
      "id": "ConfirmForm",
      "type": "FormStep",
      "name": "Confirm Quote",
      "config": {
        "formId": "$CONF_FORM_ID",
        "assignee": "user"
      },
      "transitions": [
        { "to": "ProcessPayment" }
      ]
    },
    {
      "id": "ProcessPayment",
      "type": "CalculationStep",
      "name": "Process Payment",
      "config": {
        "expression": "1",
        "outputVariable": "paymentSuccess"
      }
    }
  ]
}
EOF
)

VERSION_RES=$(curl -s -X POST "$URL/api/admin/workflows/$WORKFLOW_ID/versions" \
  -H "Content-Type: application/json" \
  -d "{
    \"versionNumber\": 1,
    \"dslJson\": $DSL_JSON,
    \"changelog\": \"Initial version\"
  }")
echo "Version Response: $VERSION_RES"

# 5. Publish Version
echo "\nPublishing Version..."
curl -s -X POST "$URL/api/admin/workflows/$WORKFLOW_ID/versions/1/publish" \
  -H "Content-Type: application/json" \
  -d '{}'

# 6. Start Workflow
echo "\nStarting Workflow..."
START_RES=$(curl -s -X POST "$URL/api/workflows/start" \
  -H "Content-Type: application/json" \
  -d '{
    "definitionCode": "gold_purchase_v4",
    "version": 1,
    "context": {},
    "correlationId": "'"$(uuidgen)"'"
  }')
echo "Start Workflow Response: $START_RES"
RUN_ID=$(echo $START_RES | grep -o '"workflowId":"[^"]*' | cut -d'"' -f4)
echo "Run ID: $RUN_ID"

# Save IDs
echo "REQ_FORM_ID=$REQ_FORM_ID" > workflow_ids_v4.env
echo "CONF_FORM_ID=$CONF_FORM_ID" >> workflow_ids_v4.env
echo "WORKFLOW_ID=$WORKFLOW_ID" >> workflow_ids_v4.env
echo "RUN_ID=$RUN_ID" >> workflow_ids_v4.env
