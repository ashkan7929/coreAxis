#!/bin/bash

# 1. Register User
echo "Registering user..."
curl -s -X POST "http://localhost:5077/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "nationalCode": "0000000001",
    "mobileNumber": "09120000001",
    "birthDate": "13700101",
    "firstName": "Admin",
    "lastName": "User"
  }'

echo "\nSending OTP..."
# 2. Send OTP
curl -s -X POST "http://localhost:5077/api/auth/send-otp" \
  -H "Content-Type: application/json" \
  -d '{
    "mobileNumber": "09120000001",
    "purpose": "Login"
  }'

echo "\nLogging in..."
# 3. Login with OTP (Mock OTP is 123456)
LOGIN_RES=$(curl -s -X POST "http://localhost:5077/api/auth/login-with-otp" \
  -H "Content-Type: application/json" \
  -d '{
    "mobileNumber": "09120000001",
    "otpCode": "123456"
  }')

echo "Login Response: $LOGIN_RES"
TOKEN=$(echo $LOGIN_RES | grep -o '"token":"[^"]*' | cut -d'"' -f4)
echo "Token: $TOKEN"

if [ -z "$TOKEN" ]; then
  echo "Failed to get token"
  exit 1
fi

# 4. Create Request Form
echo "\nCreating Request Form..."
REQ_FORM_RES=$(curl -s -X POST "http://localhost:5077/api/forms" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: default" \
  -d '{
    "tenantId": "default",
    "name": "GoldPurchaseRequest_v3",
    "title": "Gold Purchase - Request",
    "description": "Collect user info and requested grams",
    "schemaJson": "{\"version\": \"1.0\", \"title\": \"Gold Purchase - Request\", \"fields\": [{\"id\": \"grams\", \"name\": \"grams\", \"type\": \"Number\", \"label\": \"Gold Amount (grams)\", \"isRequired\": true}]}"
  }')
echo "Request Form Response: $REQ_FORM_RES"
REQ_FORM_ID=$(echo $REQ_FORM_RES | grep -o '"id":"[^"]*' | cut -d'"' -f4)
echo "Request Form ID: $REQ_FORM_ID"

# 5. Create Confirm Form
echo "\nCreating Confirm Form..."
CONF_FORM_RES=$(curl -s -X POST "http://localhost:5077/api/forms" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: default" \
  -d '{
    "tenantId": "default",
    "name": "GoldPurchaseConfirm_v3",
    "title": "Gold Purchase - Confirm Quote",
    "description": "User confirms computed price",
    "schemaJson": "{\"version\": \"1.0\", \"title\": \"Gold Purchase - Confirm Quote\", \"fields\": [{\"id\": \"accept\", \"name\": \"accept\", \"type\": \"Checkbox\", \"label\": \"I confirm the price\", \"isRequired\": true}]}"
  }')
echo "Confirm Form Response: $CONF_FORM_RES"
CONF_FORM_ID=$(echo $CONF_FORM_RES | grep -o '"id":"[^"]*' | cut -d'"' -f4)
echo "Confirm Form ID: $CONF_FORM_ID"

# Save IDs to file
echo "REQ_FORM_ID=$REQ_FORM_ID" > workflow_ids.env
echo "CONF_FORM_ID=$CONF_FORM_ID" >> workflow_ids.env
echo "TOKEN=$TOKEN" >> workflow_ids.env
