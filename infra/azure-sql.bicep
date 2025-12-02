// azure-sql.bicep - Azure SQL Database for Expense Management System
// Uses Azure AD-Only Authentication (required by MCAPS governance policy)

@description('Location for the SQL Server')
param location string

@description('Unique suffix for resource names')
param uniqueSuffix string

@description('Object ID of the Entra ID admin')
param adminObjectId string

@description('User Principal Name of the Entra ID admin')
param adminLogin string

@description('Principal ID of the managed identity for role assignment')
param managedIdentityPrincipalId string

var sqlServerName = 'sql-expensemgmt-${uniqueSuffix}'
var databaseName = 'Northwind'

// SQL Server with Entra ID-only authentication
resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: sqlServerName
  location: location
  properties: {
    minimalTlsVersion: '1.2'
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

// Database - Basic tier for development
resource database 'Microsoft.Sql/servers/databases@2021-11-01' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
    capacity: 5
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648 // 2GB
  }
}

// Firewall rule to allow Azure services
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  parent: sqlServer
  name: 'AllowAllAzureIPs'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

output sqlServerName string = sqlServer.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = database.name
