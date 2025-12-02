# Expense Management System - Azure Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Azure Resource Group                                 │
│                         (rg-expensemgmt-demo)                               │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    User Assigned Managed Identity                     │   │
│  │                    (mid-appmodassist-xxxxx)                          │   │
│  │                                                                       │   │
│  │  • Connects App Service to SQL Database                              │   │
│  │  • Connects App Service to Azure OpenAI                              │   │
│  │  • Connects App Service to AI Search                                 │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                    │                                         │
│              ┌─────────────────────┼─────────────────────┐                  │
│              │                     │                     │                  │
│              ▼                     ▼                     ▼                  │
│  ┌───────────────────┐  ┌───────────────────┐  ┌───────────────────┐       │
│  │   Azure App       │  │   Azure SQL       │  │   Azure OpenAI    │       │
│  │   Service         │  │   Database        │  │   (swedencentral) │       │
│  │   (UK South)      │  │   (UK South)      │  │                   │       │
│  │                   │  │                   │  │   GPT-4o Model    │       │
│  │   ASP.NET 8.0     │  │   Northwind DB    │  │                   │       │
│  │   Razor Pages     │  │   Basic Tier      │  │   S0 SKU          │       │
│  │   + REST APIs     │  │                   │  │                   │       │
│  │   S1 SKU          │  │   Entra ID Auth   │  │                   │       │
│  │                   │  │   Only            │  │                   │       │
│  └─────────┬─────────┘  └───────────────────┘  └───────────────────┘       │
│            │                                                                 │
│            │            ┌───────────────────┐                               │
│            │            │   Azure AI        │                               │
│            └───────────►│   Search          │                               │
│                         │   (UK South)      │                               │
│                         │                   │                               │
│                         │   Basic SKU       │                               │
│                         └───────────────────┘                               │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘

                              Internet Users
                                    │
                                    ▼
                         ┌───────────────────┐
                         │    Web Browser    │
                         │                   │
                         │  /Index           │ ─── Expense List & Management
                         │  /AddExpense      │ ─── Create New Expenses
                         │  /Approve         │ ─── Manager Approval
                         │  /Chat            │ ─── AI Assistant
                         │  /swagger         │ ─── API Documentation
                         │                   │
                         └───────────────────┘
```

## Data Flow

1. **User Request** → App Service receives HTTP request
2. **Database Operations** → App Service uses Managed Identity to connect to SQL Database
3. **AI Chat** → App Service uses Managed Identity to call Azure OpenAI
4. **Function Calling** → AI can invoke expense operations via function calling

## Security

- **Entra ID Only Authentication** for SQL Database (no SQL passwords)
- **Managed Identity** for all Azure service connections
- **HTTPS Only** enabled on App Service
- **TLS 1.2** minimum for all connections

## Deployment Options

| Script | Resources Deployed |
|--------|-------------------|
| `deploy.sh` | App Service, SQL Database, Managed Identity |
| `deploy-with-chat.sh` | All above + Azure OpenAI + AI Search |
