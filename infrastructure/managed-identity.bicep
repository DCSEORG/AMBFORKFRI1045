// User-Assigned Managed Identity for secure Azure service connections

@description('Location for all resources')
param location string

@description('Unique suffix for resource naming')
param uniqueSuffix string

// Create User-Assigned Managed Identity
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'mid-appmodassist-${uniqueSuffix}'
  location: location
}

// Outputs
output managedIdentityId string = managedIdentity.id
output managedIdentityName string = managedIdentity.name
output managedIdentityClientId string = managedIdentity.properties.clientId
output managedIdentityPrincipalId string = managedIdentity.properties.principalId
