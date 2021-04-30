param storage_account_name string
param connections_azurequeues_name string
param connections_azuretables_name string
param connections_powerbi_name string
param logic_app_name string
param powerbi_workspace_name string
param powerbi_dataset_name string

param location string = resourceGroup().location

resource storage_account 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storage_account_name
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: true
    allowCrossTenantReplication: false
    allowSharedKeyAccess: true
    defaultToOAuthAuthentication: false
    publicNetworkAccess: 'Enabled'
    minimumTlsVersion: 'TLS1_2'
  }
}

resource storage_account_queue_service 'Microsoft.Storage/storageAccounts/queueServices@2023-01-01' = {
  parent: storage_account
  name: 'default'
}

resource storage_account_queue_service_queue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-01-01' = {
  parent: storage_account_queue_service
  name: 'inkbird'
}

resource storage_account_table_service 'Microsoft.Storage/storageAccounts/tableServices@2023-01-01' = {
  parent: storage_account
  name: 'default'
}

resource storage_account_table_service_table 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-01-01' = {
  parent: storage_account_table_service
  name: 'inkbird'
}

resource connections_azurequeues 'Microsoft.Web/connections@2016-06-01' = {
  name: connections_azurequeues_name
  location: location
  properties: {
    displayName: connections_azurequeues_name
    api: {
      name: connections_azurequeues_name
      displayName: 'Azure Queues'
      description: 'Azure Queues'
      id: subscriptionResourceId('Microsoft.Web/locations/managedApis', location, 'azurequeues')
      type: 'Microsoft.Web/locations/managedApis'
    }
    parameterValueSet: {
      name: 'managedIdentityAuth'
      values: {}
  }
}
}

resource connections_azuretables 'Microsoft.Web/connections@2016-06-01' = {
  name: connections_azuretables_name
  location: location
  properties: {
    displayName: connections_azuretables_name
    api: {
      name: 'azuretables'
      displayName: 'Azure Table Storage'
      description: 'Azure Table Storage'
      id: subscriptionResourceId('Microsoft.Web/locations/managedApis', location, 'azuretables')
      type: 'Microsoft.Web/locations/managedApis'
    }
    parameterValueSet: {
        name: 'managedIdentityAuth'
        values: {}
    }
  }
}

resource connections_powerbi 'Microsoft.Web/connections@2016-06-01' = {
  name: connections_powerbi_name
  location: location
  properties: {
    displayName: connections_powerbi_name
    api: {
      name: 'powerbi'
      displayName: 'Power BI'
      description: 'Power BI'
      id: subscriptionResourceId('Microsoft.Web/locations/managedApis', location, 'powerbi')
      type: 'Microsoft.Web/locations/managedApis'
    }
  }
}

resource logic_app 'Microsoft.Logic/workflows@2019-05-01' = {
  name: logic_app_name
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    state: 'Enabled'
    definition: {
      '$schema': 'https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#'
      contentVersion: '1.0.0.0'
      parameters: {
        '$connections': {
          defaultValue: {}
          type: 'Object'
        }
      }
      triggers: {
        When_there_are_messages_in_a_queue: {
          recurrence: {
            frequency: 'Minute'
            interval: 1
          }
          evaluatedRecurrence: {
            frequency: 'Minute'
            interval: 1
          }
          splitOn: '@triggerBody()?[\'QueueMessagesList\']?[\'QueueMessage\']'
          type: 'ApiConnection'
          inputs: {
            host: {
              connection: {
                name: '@parameters(\'$connections\')[\'azurequeues\'][\'connectionId\']'
              }
            }
            method: 'get'
            path: '/v2/storageAccounts/@{encodeURIComponent(encodeURIComponent(\'${storage_account.name}\'))}/queues/@{encodeURIComponent(\'${storage_account_queue_service_queue.name}\')}/message_trigger'
          }
        }
      }
      actions: {
        Add_rows_to_a_dataset: {
          runAfter: {
            Convert_time_zone: [
              'Succeeded'
            ]
          }
          type: 'ApiConnection'
          inputs: {
            body: {
              EventTime: '@body(\'Convert_time_zone\')'
              EventTimeUtc: '@variables(\'Insertion Time\')'
              Humidity: '@{body(\'Parse_message_text\')?[\'Humidity\']}'
              Location: '@body(\'Parse_message_text\')?[\'Location\']'
              Temperature: '@{body(\'Parse_message_text\')?[\'Temperature\']}'
            }
            host: {
              connection: {
                name: '@parameters(\'$connections\')[\'powerbi\'][\'connectionId\']'
              }
            }
            method: 'post'
            path: '/v1.0/myorg/groups/@{encodeURIComponent(\'${powerbi_workspace_name}\')}/datasets/@{encodeURIComponent(\'${powerbi_dataset_name}\')}/tables/@{encodeURIComponent(\'RealTimeData\')}/rows'
            queries: {
              pbi_source: 'powerAutomate'
            }
          }
        }
        Convert_time_zone: {
          runAfter: {
            Parse_message_text: [
              'Succeeded'
            ]
          }
          type: 'Expression'
          kind: 'ConvertTimeZone'
          inputs: {
            baseTime: '@variables(\'Insertion Time\')'
            destinationTimeZone: 'Tokyo Standard Time'
            formatString: 's'
            sourceTimeZone: 'UTC'
          }
        }
        Delete_message: {
          runAfter: {
            Add_rows_to_a_dataset: [
              'Succeeded'
            ]
            Insert_entity: [
              'Succeeded'
            ]
          }
          type: 'ApiConnection'
          inputs: {
            host: {
              connection: {
                name: '@parameters(\'$connections\')[\'azurequeues\'][\'connectionId\']'
              }
            }
            method: 'delete'
            path: '/v2/storageAccounts/@{encodeURIComponent(encodeURIComponent(\'${storage_account.name}\'))}/queues/@{encodeURIComponent(\'${storage_account_queue_service_queue.name}\')}/messages/@{encodeURIComponent(triggerBody()?[\'MessageId\'])}'
            queries: {
              popreceipt: '@triggerBody()?[\'PopReceipt\']'
            }
          }
        }
        Initialize_variable: {
          runAfter: {}
          type: 'InitializeVariable'
          inputs: {
            variables: [
              {
                name: 'Insertion Time'
                type: 'string'
                value: '@{formatDateTime(triggerBody()?[\'InsertionTime\'], \'s\')}Z'
              }
            ]
          }
        }
        Insert_entity: {
          runAfter: {
            Convert_time_zone: [
              'Succeeded'
            ]
          }
          type: 'ApiConnection'
          inputs: {
            body: {
              EventTime: '@{variables(\'Insertion Time\')}'
              Humidity: '@body(\'Parse_message_text\')?[\'Humidity\']'
              Location: '@{body(\'Parse_message_text\')?[\'Location\']}'
              PartitionKey: '@{body(\'Parse_message_text\')?[\'Id\']}'
              RowKey: '@{triggerBody()?[\'MessageId\']}'
              Temperature: '@body(\'Parse_message_text\')?[\'Temperature\']'
            }
            host: {
              connection: {
                name: '@parameters(\'$connections\')[\'azuretables\'][\'connectionId\']'
              }
            }
            method: 'post'
            path: '/v2/storageAccounts/@{encodeURIComponent(encodeURIComponent(\'${storage_account.name}\'))}/tables/@{encodeURIComponent(\'${storage_account_table_service_table.name}\')}/entities'
          }
        }
        Parse_message_text: {
          runAfter: {
            Initialize_variable: [
              'Succeeded'
            ]
          }
          type: 'ParseJson'
          inputs: {
            content: '@triggerBody()?[\'MessageText\']'
            schema: {
              properties: {
                Humidity: {
                  type: 'number'
                }
                Id: {
                  type: 'string'
                }
                Location: {
                  type: 'string'
                }
                Temperature: {
                  type: 'number'
                }
              }
              type: 'object'
            }
          }
        }
      }
      outputs: {}
    }
    parameters: {
      '$connections': {
        value: {
          azurequeues: {
            connectionId: connections_azurequeues.id
            connectionName: connections_azurequeues.name
            connectionProperties: {
              authentication: {
                type: 'ManagedServiceIdentity'
              }
            }
            id: subscriptionResourceId('Microsoft.Web/locations/managedApis', location, 'azurequeues')
          }
          azuretables: {
            connectionId: connections_azuretables.id
            connectionName: connections_azuretables.name
            connectionProperties: {
              authentication: {
                type: 'ManagedServiceIdentity'
              }
            }
            id: subscriptionResourceId('Microsoft.Web/locations/managedApis', location, 'azuretables')
          }
          powerbi: {
            connectionId: connections_powerbi.id
            connectionName: connections_powerbi.name
            id: subscriptionResourceId('Microsoft.Web/locations/managedApis', location, 'powerbi')
          }
        }
      }
    }
  }
}
