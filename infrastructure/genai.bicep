// Azure OpenAI and AI Search resources for GenAI features

@description('Location for non-OpenAI resources')
param location string

@description('Unique suffix for resource naming')
param uniqueSuffix string

@description('Managed Identity Principal ID for role assignments')
param managedIdentityPrincipalId string

// Azure OpenAI deployed to Sweden Central for model availability
var openAILocation = 'swedencentral'

// Cognitive Services OpenAI User role
var cognitiveServicesOpenAIUserRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')

// Search Index Data Contributor role
var searchIndexDataContributorRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7')

// Search Service Contributor role
var searchServiceContributorRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7ca78c08-252a-4471-8644-bb5ff32d4ba0')

// Azure OpenAI account
resource openAI 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: 'aoai-expensemgmt-${uniqueSuffix}'
  location: openAILocation
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: 'aoai-expensemgmt-${uniqueSuffix}'
    publicNetworkAccess: 'Enabled'
  }
}

// GPT-4o model deployment
resource gpt4oDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAI
  name: 'gpt-4o'
  sku: {
    name: 'Standard'
    capacity: 8
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-05-13'
    }
  }
}

// Azure AI Search (Cognitive Search)
resource searchService 'Microsoft.Search/searchServices@2023-11-01' = {
  name: 'search-expensemgmt-${uniqueSuffix}'
  location: location
  sku: {
    name: 'basic'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
    publicNetworkAccess: 'enabled'
  }
}

// Role assignment: Cognitive Services OpenAI User for Managed Identity
resource openAIRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAI.id, managedIdentityPrincipalId, cognitiveServicesOpenAIUserRole)
  scope: openAI
  properties: {
    principalId: managedIdentityPrincipalId
    roleDefinitionId: cognitiveServicesOpenAIUserRole
    principalType: 'ServicePrincipal'
  }
}

// Role assignment: Search Index Data Contributor for Managed Identity
resource searchIndexRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(searchService.id, managedIdentityPrincipalId, searchIndexDataContributorRole)
  scope: searchService
  properties: {
    principalId: managedIdentityPrincipalId
    roleDefinitionId: searchIndexDataContributorRole
    principalType: 'ServicePrincipal'
  }
}

// Role assignment: Search Service Contributor for Managed Identity
resource searchServiceRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(searchService.id, managedIdentityPrincipalId, searchServiceContributorRole)
  scope: searchService
  properties: {
    principalId: managedIdentityPrincipalId
    roleDefinitionId: searchServiceContributorRole
    principalType: 'ServicePrincipal'
  }
}

// Outputs
output openAIEndpoint string = openAI.properties.endpoint
output openAIModelName string = gpt4oDeployment.name
output openAIName string = openAI.name
output searchEndpoint string = 'https://${searchService.name}.search.windows.net'
output searchName string = searchService.name
