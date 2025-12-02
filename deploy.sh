#!/bin/bash
# deploy.sh - Deploy Expense Management System to Azure (without GenAI resources)
# Run: chmod +x deploy.sh && ./deploy.sh

set -e

echo "=========================================="
echo "Expense Management System - Deployment"
echo "=========================================="

# Configuration - Update these values
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-expensemgmt-demo}"
LOCATION="${LOCATION:-uksouth}"

# Get current user info for SQL admin
echo "Getting current user info..."
ADMIN_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)
ADMIN_LOGIN=$(az ad signed-in-user show --query userPrincipalName -o tsv)

echo "Admin: $ADMIN_LOGIN"
echo "Object ID: $ADMIN_OBJECT_ID"

# Create resource group if it doesn't exist
echo ""
echo "Step 1: Creating resource group..."
az group create --name $RESOURCE_GROUP --location $LOCATION --output none
echo "✓ Resource group created: $RESOURCE_GROUP"

# Deploy infrastructure
echo ""
echo "Step 2: Deploying infrastructure (App Service, SQL, Managed Identity)..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
    --resource-group $RESOURCE_GROUP \
    --template-file infra/main.bicep \
    --parameters adminObjectId=$ADMIN_OBJECT_ID adminLogin=$ADMIN_LOGIN deployGenAI=false \
    --query properties.outputs \
    --output json)

# Extract outputs
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceName.value')
APP_SERVICE_URL=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceUrl.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value')
SQL_SERVER_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerName.value')
SQL_SERVER_FQDN=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerFqdn.value')
DATABASE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.databaseName.value')

echo "✓ Infrastructure deployed"
echo "  App Service: $APP_SERVICE_NAME"
echo "  SQL Server: $SQL_SERVER_FQDN"
echo "  Managed Identity: $MANAGED_IDENTITY_NAME"

# Wait for SQL Server to be ready
echo ""
echo "Step 3: Waiting for SQL Server to be ready (30 seconds)..."
sleep 30

# Add current IP to SQL firewall
echo ""
echo "Step 4: Configuring SQL firewall..."
MY_IP=$(curl -s https://api.ipify.org)

# Allow Azure services access
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "AllowAllAzureIPs" \
    --start-ip-address 0.0.0.0 \
    --end-ip-address 0.0.0.0 \
    --output none 2>/dev/null || true

# Add deployment IP
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "AllowDeploymentIP" \
    --start-ip-address $MY_IP \
    --end-ip-address $MY_IP \
    --output none 2>/dev/null || true

echo "✓ Firewall configured"
echo "Waiting 15 seconds for firewall rules to propagate..."
sleep 15

# Install Python dependencies
echo ""
echo "Step 5: Installing Python dependencies..."
pip3 install --quiet pyodbc azure-identity

# Export variables for Python scripts
export SQL_SERVER_FQDN
export SQL_DATABASE=$DATABASE_NAME

# Import database schema
echo ""
echo "Step 6: Importing database schema..."
python3 run-sql.py

# Update script.sql with managed identity name (cross-platform)
echo ""
echo "Step 7: Configuring managed identity database access..."
sed -i.bak "s/MANAGED-IDENTITY-NAME/$MANAGED_IDENTITY_NAME/g" script.sql && rm -f script.sql.bak

# Run database role assignment
python3 run-sql-dbrole.py

# Create stored procedures
echo ""
echo "Step 8: Creating stored procedures..."
python3 run-sql-stored-procs.py

# Configure App Service settings
echo ""
echo "Step 9: Configuring App Service..."
CONNECTION_STRING="Server=tcp:${SQL_SERVER_FQDN},1433;Database=${DATABASE_NAME};Authentication=Active Directory Managed Identity;User Id=${MANAGED_IDENTITY_CLIENT_ID};"

az webapp config appsettings set \
    --name $APP_SERVICE_NAME \
    --resource-group $RESOURCE_GROUP \
    --settings \
        "ConnectionStrings__DefaultConnection=$CONNECTION_STRING" \
        "ManagedIdentityClientId=$MANAGED_IDENTITY_CLIENT_ID" \
        "AZURE_CLIENT_ID=$MANAGED_IDENTITY_CLIENT_ID" \
    --output none

echo "✓ App Service configured"

# Build and deploy the application
echo ""
echo "Step 10: Building application..."
cd src/ExpenseManagement
dotnet publish -c Release -o ./publish
cd publish

# Create zip file with files at root level
echo "Creating deployment package..."
zip -r ../../../app.zip ./*
cd ../../..

echo "✓ Application built"

# Deploy to Azure
echo ""
echo "Step 11: Deploying application to Azure..."
az webapp deploy \
    --resource-group $RESOURCE_GROUP \
    --name $APP_SERVICE_NAME \
    --src-path ./app.zip \
    --type zip

echo "✓ Application deployed"

# Reset the script.sql file
git checkout script.sql 2>/dev/null || true

echo ""
echo "=========================================="
echo "Deployment Complete!"
echo "=========================================="
echo ""
echo "Application URL: ${APP_SERVICE_URL}/Index"
echo ""
echo "Note: Navigate to /Index to access the Expense Management System"
echo "      The Chat UI will show a message about GenAI not being configured."
echo "      To enable AI chat, run: ./deploy-with-chat.sh"
echo ""
echo "For local development, update appsettings.json with:"
echo "  Server: $SQL_SERVER_FQDN"
echo "  Database: $DATABASE_NAME"
echo "  Use 'Authentication=Active Directory Default' and run 'az login' first"
echo ""
