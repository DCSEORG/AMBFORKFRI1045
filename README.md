![Header image](https://github.com/DougChisholm/App-Mod-Booster/blob/main/repo-header-booster.png)

# Expense Management System

A modern cloud-native ASP.NET 8 application that demonstrates how to modernize legacy applications using Azure services and AI capabilities.

## 🚀 Quick Start

### Prerequisites
- Azure CLI installed and logged in (`az login`)
- Python 3.x installed (for database setup)
- .NET 8 SDK (for local development)

### Deploy to Azure

**Without AI Chat (Basic deployment):**
```bash
chmod +x deploy.sh
./deploy.sh
```

**With AI Chat (Full GenAI experience):**
```bash
chmod +x deploy-with-chat.sh
./deploy-with-chat.sh
```

After deployment, access the application at: `https://<app-name>.azurewebsites.net/Index`

## 📋 Features

### Core Functionality
- **View Expenses** - List and filter expenses by status and category
- **Add Expense** - Submit new expense claims with amount, date, category, and description
- **Approve Expenses** - Managers can review, approve, or reject pending expenses
- **Dashboard** - Overview of recent expenses and summaries

### AI Chat Interface (with GenAI deployment)
- Natural language queries about expenses
- Function calling for database operations
- Create, submit, approve expenses through conversation
- Expense summaries and reports

### API
- RESTful API with Swagger documentation
- Endpoints for expenses, categories, statuses, users
- Chat API for AI interactions

## 🏗️ Architecture

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed architecture diagram.

| Service | Purpose | SKU |
|---------|---------|-----|
| App Service | Web application hosting | Standard S1 |
| Azure SQL | Database | Basic |
| Managed Identity | Secure authentication | - |
| Azure OpenAI* | AI chat capabilities | S0 (GPT-4o) |
| AI Search* | RAG pattern support | Basic |

*Only deployed with `deploy-with-chat.sh`

## 🔐 Security

- **Managed Identity** - No credentials in code
- **Entra ID Only** - SQL Server uses Azure AD authentication exclusively
- **HTTPS Only** - All traffic encrypted
- **Stored Procedures** - All database access through stored procedures

## 📁 Project Structure

```
├── infrastructure/          # Bicep IaC templates
│   ├── main.bicep          # Main template
│   ├── app-service.bicep   # App Service
│   ├── azure-sql.bicep     # Azure SQL Database
│   ├── managed-identity.bicep # Managed Identity
│   └── genai.bicep         # Azure OpenAI & AI Search
├── src/ExpenseManagement/   # ASP.NET 8 application
│   ├── Api/                # REST API controllers
│   ├── Models/             # Data models
│   ├── Pages/              # Razor Pages
│   ├── Services/           # Business logic
│   └── wwwroot/            # Static files
├── Database-Schema/         # SQL schema
├── deploy.sh               # Deployment script (no AI)
├── deploy-with-chat.sh     # Deployment script (with AI)
├── stored-procedures.sql   # Database stored procedures
└── app.zip                 # Deployment package
```

## 💻 Local Development

1. Clone the repository
2. Update `src/ExpenseManagement/appsettings.json` with your connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=tcp:YOUR_SERVER.database.windows.net,1433;Database=Northwind;Authentication=Active Directory Default;..."
     }
   }
   ```
3. Run `az login` to authenticate
4. Navigate to `src/ExpenseManagement` and run:
   ```bash
   dotnet run
   ```
5. Open `https://localhost:5001/Index`

## 📖 API Documentation

Access Swagger UI at: `https://<app-url>/swagger`

### Available Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/expenses | List all expenses |
| GET | /api/expenses/{id} | Get expense by ID |
| GET | /api/expenses/pending | Get pending expenses |
| POST | /api/expenses | Create expense |
| PUT | /api/expenses/{id} | Update expense |
| DELETE | /api/expenses/{id} | Delete expense |
| POST | /api/expenses/{id}/submit | Submit for approval |
| POST | /api/expenses/{id}/approve | Approve expense |
| POST | /api/expenses/{id}/reject | Reject expense |
| GET | /api/categories | List categories |
| GET | /api/statuses | List statuses |
| GET | /api/users | List users |
| POST | /api/chat | AI chat |

## 🤖 Modernization with GitHub Copilot

This project was generated using GitHub Copilot's coding agent. To modernize your own legacy application:

1. Fork this repository
2. Replace the screenshots in `Legacy-Screenshots/` with your app's screenshots
3. Replace the database schema in `Database-Schema/`
4. Open the coding agent and say "modernise my app"
5. Review and merge the generated pull request
6. Deploy using the provided scripts

## 📚 Resources

- [Azure Best Practices](https://learn.microsoft.com/en-us/azure/architecture/best-practices/index-best-practices)
- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [Azure OpenAI Service](https://learn.microsoft.com/en-us/azure/ai-services/openai/)

## 📄 License

See [LICENSE](LICENSE) for details.
