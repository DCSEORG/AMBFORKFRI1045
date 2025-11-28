// Main Bicep template for Expense Management System
// Deploys App Service, Azure SQL, Managed Identity, and optionally GenAI resources

@description('Location for all resources')
param location string = 'uksouth'

@description('Deploy GenAI resources (Azure OpenAI and AI Search)')
param deployGenAI bool = false

@description('Azure AD Object ID of the SQL Server administrator')
param adminObjectId string

@description('Azure AD User Principal Name of the SQL Server administrator')
param adminLogin string

// Generate unique suffix for resource names
var uniqueSuffix = uniqueString(resourceGroup().id)

// Deploy User-Assigned Managed Identity
module managedIdentity 'managed-identity.bicep' = {
  name: 'managedIdentityDeployment'
  params: {
    location: location
    uniqueSuffix: uniqueSuffix
  }
}

// Deploy App Service
module appService 'app-service.bicep' = {
  name: 'appServiceDeployment'
  params: {
    location: location
    uniqueSuffix: uniqueSuffix
    managedIdentityId: managedIdentity.outputs.managedIdentityId
    managedIdentityClientId: managedIdentity.outputs.managedIdentityClientId
  }
}

// Deploy Azure SQL Database
module azureSql 'azure-sql.bicep' = {
  name: 'azureSqlDeployment'
  params: {
    location: location
    uniqueSuffix: uniqueSuffix
    adminObjectId: adminObjectId
    adminLogin: adminLogin
    managedIdentityPrincipalId: managedIdentity.outputs.managedIdentityPrincipalId
  }
}

// Conditionally deploy GenAI resources
module genai 'genai.bicep' = if (deployGenAI) {
  name: 'genaiDeployment'
  params: {
    location: location
    uniqueSuffix: uniqueSuffix
    managedIdentityPrincipalId: managedIdentity.outputs.managedIdentityPrincipalId
  }
}

// Outputs
output appServiceName string = appService.outputs.appServiceName
output appServiceUrl string = appService.outputs.appServiceUrl
output sqlServerName string = azureSql.outputs.sqlServerName
output sqlServerFqdn string = azureSql.outputs.sqlServerFqdn
output databaseName string = azureSql.outputs.databaseName
output managedIdentityName string = managedIdentity.outputs.managedIdentityName
output managedIdentityClientId string = managedIdentity.outputs.managedIdentityClientId
output managedIdentityPrincipalId string = managedIdentity.outputs.managedIdentityPrincipalId

// GenAI outputs (conditional)
output openAIEndpoint string = deployGenAI ? genai.outputs.openAIEndpoint : ''
output openAIModelName string = deployGenAI ? genai.outputs.openAIModelName : ''
output openAIName string = deployGenAI ? genai.outputs.openAIName : ''
output searchEndpoint string = deployGenAI ? genai.outputs.searchEndpoint : ''
output searchName string = deployGenAI ? genai.outputs.searchName : ''
