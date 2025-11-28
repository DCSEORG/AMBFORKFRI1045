#!/bin/bash

# Deploy Expense Management System to Azure
# Usage: ./deploy.sh

set -e

# Configuration - Update these values before running
RESOURCE_GROUP="rg-expensemgmt-demo"
LOCATION="uksouth"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Expense Management System Deployment${NC}"
echo -e "${GREEN}========================================${NC}"

# Check if logged in to Azure
echo -e "\n${YELLOW}Checking Azure CLI login status...${NC}"
if ! az account show &> /dev/null; then
    echo -e "${RED}Not logged in to Azure. Please run 'az login' first.${NC}"
    exit 1
fi

# Get current user's Object ID and UPN for SQL Admin
echo -e "\n${YELLOW}Getting current user information...${NC}"
ADMIN_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)
ADMIN_LOGIN=$(az ad signed-in-user show --query userPrincipalName -o tsv)
echo -e "Admin Object ID: ${ADMIN_OBJECT_ID}"
echo -e "Admin Login: ${ADMIN_LOGIN}"

# Create resource group
echo -e "\n${YELLOW}Creating resource group...${NC}"
az group create --name $RESOURCE_GROUP --location $LOCATION --output none
echo -e "${GREEN}Resource group created: ${RESOURCE_GROUP}${NC}"

# Deploy infrastructure
echo -e "\n${YELLOW}Deploying Azure infrastructure...${NC}"
DEPLOYMENT_OUTPUT=$(az deployment group create \
    --resource-group $RESOURCE_GROUP \
    --template-file infrastructure/main.bicep \
    --parameters adminObjectId=$ADMIN_OBJECT_ID adminLogin=$ADMIN_LOGIN deployGenAI=false \
    --query properties.outputs -o json)

# Extract outputs
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceName.value')
APP_SERVICE_URL=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceUrl.value')
SQL_SERVER_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerName.value')
SQL_SERVER_FQDN=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerFqdn.value')
DATABASE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.databaseName.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value')

echo -e "${GREEN}Infrastructure deployed successfully!${NC}"
echo -e "App Service: ${APP_SERVICE_NAME}"
echo -e "SQL Server: ${SQL_SERVER_NAME}"
echo -e "Database: ${DATABASE_NAME}"
echo -e "Managed Identity: ${MANAGED_IDENTITY_NAME}"

# Configure App Service connection string
echo -e "\n${YELLOW}Configuring App Service settings...${NC}"
CONNECTION_STRING="Server=tcp:${SQL_SERVER_FQDN},1433;Database=${DATABASE_NAME};Authentication=Active Directory Managed Identity;User Id=${MANAGED_IDENTITY_CLIENT_ID};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az webapp config connection-string set \
    --name $APP_SERVICE_NAME \
    --resource-group $RESOURCE_GROUP \
    --connection-string-type SQLAzure \
    --settings "DefaultConnection=$CONNECTION_STRING" \
    --output none

echo -e "${GREEN}Connection string configured!${NC}"

# Wait for SQL Server to be fully ready
echo -e "\n${YELLOW}Waiting 30 seconds for SQL Server to be fully ready...${NC}"
sleep 30

# Add local IP to firewall for schema import
echo -e "\n${YELLOW}Adding local IP to SQL Server firewall...${NC}"
LOCAL_IP=$(curl -s ifconfig.me)
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "LocalDevelopment" \
    --start-ip-address $LOCAL_IP \
    --end-ip-address $LOCAL_IP \
    --output none
echo -e "${GREEN}Firewall rule added for IP: ${LOCAL_IP}${NC}"

# Install required Python packages
echo -e "\n${YELLOW}Installing Python dependencies...${NC}"
pip3 install --quiet pyodbc azure-identity

# Update Python scripts with actual values
echo -e "\n${YELLOW}Configuring Python scripts...${NC}"
sed -i.bak "s/example.database.windows.net/${SQL_SERVER_FQDN}/g" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s/database_name/${DATABASE_NAME}/g" run-sql.py && rm -f run-sql.py.bak

sed -i.bak "s/example.database.windows.net/${SQL_SERVER_FQDN}/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s/database_name/${DATABASE_NAME}/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s/MANAGED-IDENTITY-NAME/${MANAGED_IDENTITY_NAME}/g" script.sql && rm -f script.sql.bak

sed -i.bak "s/example.database.windows.net/${SQL_SERVER_FQDN}/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak
sed -i.bak "s/database_name/${DATABASE_NAME}/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak

# Run the SQL schema import
echo -e "\n${YELLOW}Importing database schema...${NC}"
python3 run-sql.py

# Configure database roles for managed identity
echo -e "\n${YELLOW}Configuring database roles for managed identity...${NC}"
python3 run-sql-dbrole.py

# Deploy stored procedures
echo -e "\n${YELLOW}Deploying stored procedures...${NC}"
python3 run-sql-stored-procs.py

# Deploy the application
echo -e "\n${YELLOW}Deploying application code...${NC}"
az webapp deploy \
    --resource-group $RESOURCE_GROUP \
    --name $APP_SERVICE_NAME \
    --src-path ./app.zip \
    --type zip

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Deployment Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo -e "\nApplication URL: ${APP_SERVICE_URL}/Index"
echo -e "\nNote: Navigate to /Index to view the application"
echo -e "\nTo run locally, update appsettings.json connection string to use:"
echo -e "Authentication=Active Directory Default"
echo -e "Then run 'az login' before starting the application"
