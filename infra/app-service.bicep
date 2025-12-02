// app-service.bicep - App Service for Expense Management System

@description('Location for the App Service')
param location string

@description('Unique suffix for resource names')
param uniqueSuffix string

@description('Managed Identity resource ID')
param managedIdentityId string

@description('Managed Identity Client ID')
param managedIdentityClientId string

var appServicePlanName = 'asp-expensemgmt-${uniqueSuffix}'
var appServiceName = 'app-expensemgmt-${uniqueSuffix}'

// App Service Plan - S1 SKU to avoid cold start
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'S1'
    tier: 'Standard'
    capacity: 1
  }
  properties: {
    reserved: false // Windows
  }
}

// App Service
resource appService 'Microsoft.Web/sites@2022-09-01' = {
  name: appServiceName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      alwaysOn: true
      http20Enabled: true
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'AZURE_CLIENT_ID'
          value: managedIdentityClientId
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
      ]
    }
  }
}

output appServiceName string = appService.name
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output managedIdentityPrincipalId string = managedIdentityId
