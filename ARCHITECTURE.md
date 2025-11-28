# Azure Architecture Diagram

This document describes the Azure services architecture for the Expense Management System.

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                           Azure Resource Group                                        │
│                         (rg-expensemgmt-demo)                                        │
│                                                                                       │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐ │
│  │                        User-Assigned Managed Identity                            │ │
│  │                       (mid-appmodassist-xxxxxxx)                                 │ │
│  │                                                                                   │ │
│  │    Used for secure authentication between Azure services                         │ │
│  │    - No credentials stored in code                                               │ │
│  │    - Automatic token management                                                   │ │
│  └─────────────────────────────────────────────────────────────────────────────────┘ │
│                                          │                                            │
│                    ┌─────────────────────┼─────────────────────┐                     │
│                    │                     │                     │                     │
│                    ▼                     ▼                     ▼                     │
│  ┌─────────────────────┐  ┌─────────────────────┐  ┌─────────────────────────────┐  │
│  │                     │  │                     │  │                             │  │
│  │   App Service       │  │   Azure SQL         │  │   Azure OpenAI              │  │
│  │   (Standard S1)     │  │   Database          │  │   (Sweden Central)          │  │
│  │                     │  │   (Basic Tier)      │  │                             │  │
│  │   - ASP.NET 8.0     │  │                     │  │   - GPT-4o Model            │  │
│  │   - Razor Pages     │  │   - Northwind DB    │  │   - Capacity: 8             │  │
│  │   - REST APIs       │  │   - Entra ID Auth   │  │   - Managed Identity        │  │
│  │   - Swagger UI      │  │   - No SQL Auth     │  │                             │  │
│  │                     │  │                     │  │                             │  │
│  │   UK South          │  │   UK South          │  │   Sweden Central            │  │
│  │                     │  │                     │  │   (for model availability)  │  │
│  └─────────────────────┘  └─────────────────────┘  └─────────────────────────────┘  │
│            │                        ▲                           ▲                    │
│            │                        │                           │                    │
│            │ ┌──────────────────────┘                           │                    │
│            │ │                                                  │                    │
│            ▼ ▼                                                  │                    │
│  ┌─────────────────────────────────────────────────────────────┐│                    │
│  │                                                             ││                    │
│  │                    Data Flow                                ││                    │
│  │                                                             ││                    │
│  │  1. User → App Service: HTTP requests (HTTPS only)          ││                    │
│  │  2. App Service → SQL: Stored procedures via Managed ID     ││                    │
│  │  3. App Service → OpenAI: Chat completions via Managed ID   ││                    │
│  │  4. OpenAI → App Service: Function calling for data ops     ││                    │
│  │                                                             ││                    │
│  └─────────────────────────────────────────────────────────────┘│                    │
│                                                                  │                    │
│  ┌─────────────────────────────────────────────────────────────┐│                    │
│  │                                                             ││                    │
│  │                  AI Search (Optional)                       │◄───────────────────┤
│  │                  (Basic Tier)                               │                     │
│  │                                                             │                     │
│  │   - Index management                                        │                     │
│  │   - RAG pattern support                                     │                     │
│  │   - Managed Identity access                                 │                     │
│  │                                                             │                     │
│  └─────────────────────────────────────────────────────────────┘                     │
│                                                                                       │
└─────────────────────────────────────────────────────────────────────────────────────┘

                                    │
                                    │ HTTPS
                                    │
                                    ▼
                    ┌─────────────────────────────┐
                    │                             │
                    │         Users               │
                    │                             │
                    │   - Browser access          │
                    │   - API consumers           │
                    │                             │
                    └─────────────────────────────┘
```

## Security Features

1. **Managed Identity Authentication**
   - No passwords or API keys in code
   - Automatic credential rotation
   - Fine-grained role assignments

2. **Entra ID Only for SQL**
   - Azure AD authentication only
   - SQL authentication disabled
   - Compliance with MCAPS governance

3. **HTTPS Only**
   - All traffic encrypted
   - TLS 1.2 minimum

4. **Role-Based Access**
   - Cognitive Services OpenAI User for OpenAI access
   - db_datareader, db_datawriter for SQL access
   - Search Index Data Contributor for AI Search

## Deployment Options

### Without GenAI (deploy.sh)
- App Service
- Azure SQL Database
- Managed Identity

### With GenAI (deploy-with-chat.sh)
- All of the above, plus:
- Azure OpenAI (GPT-4o)
- Azure AI Search
- Function calling for natural language database queries
