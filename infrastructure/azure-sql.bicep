// Azure SQL Database with Entra ID (Azure AD) Only Authentication

@description('Location for all resources')
param location string

@description('Unique suffix for resource naming')
param uniqueSuffix string

@description('Azure AD Object ID of the SQL Server administrator')
param adminObjectId string

@description('Azure AD User Principal Name of the SQL Server administrator')
param adminLogin string

@description('Managed Identity Principal ID for database access')
param managedIdentityPrincipalId string

// SQL Server
resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: 'sql-expensemgmt-${uniqueSuffix}'
  location: location
  properties: {
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: 'User'
      login: adminLogin
      sid: adminObjectId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
  }
}

// SQL Database - Northwind (Basic tier for development)
resource sqlDatabase 'Microsoft.Sql/servers/databases@2021-11-01' = {
  parent: sqlServer
  name: 'Northwind'
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648
  }
}

// Firewall rule to allow Azure services
resource firewallRule 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Outputs
output sqlServerName string = sqlServer.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = sqlDatabase.name
output managedIdentityPrincipalId string = managedIdentityPrincipalId
