// managed-identity.bicep - User Assigned Managed Identity for Expense Management System

@description('Location for the managed identity')
param location string

@description('Unique suffix for resource names')
param uniqueSuffix string

// Create a unique name using day-hour-minute pattern
var identityName = 'mid-appmodassist-${uniqueSuffix}'

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
}

output managedIdentityId string = managedIdentity.id
output managedIdentityName string = managedIdentity.name
output managedIdentityClientId string = managedIdentity.properties.clientId
output managedIdentityPrincipalId string = managedIdentity.properties.principalId
