using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using OpenAI.Chat;
using System.Text.Json;
using ExpenseManagement.Models;

namespace ExpenseManagement.Services;

public interface IChatService
{
    Task<string> GetChatResponseAsync(string userMessage, List<ChatMessage>? conversationHistory = null);
    bool IsConfigured { get; }
}

public class ChatService : IChatService
{
    private readonly IConfiguration _configuration;
    private readonly IExpenseService _expenseService;
    private readonly ILogger<ChatService> _logger;
    private readonly AzureOpenAIClient? _openAIClient;
    private readonly string? _deploymentName;

    public bool IsConfigured => _openAIClient != null && !string.IsNullOrEmpty(_deploymentName);

    public ChatService(IConfiguration configuration, IExpenseService expenseService, ILogger<ChatService> logger)
    {
        _configuration = configuration;
        _expenseService = expenseService;
        _logger = logger;

        var endpoint = configuration["OpenAI:Endpoint"];
        _deploymentName = configuration["OpenAI:DeploymentName"];

        if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(_deploymentName))
        {
            try
            {
                var managedIdentityClientId = configuration["ManagedIdentityClientId"];
                TokenCredential credential;

                if (!string.IsNullOrEmpty(managedIdentityClientId))
                {
                    _logger.LogInformation("Using ManagedIdentityCredential with client ID: {ClientId}", managedIdentityClientId);
                    credential = new ManagedIdentityCredential(managedIdentityClientId);
                }
                else
                {
                    _logger.LogInformation("Using DefaultAzureCredential");
                    credential = new DefaultAzureCredential();
                }

                _openAIClient = new AzureOpenAIClient(new Uri(endpoint), credential);
                _logger.LogInformation("Azure OpenAI client initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure OpenAI client");
            }
        }
        else
        {
            _logger.LogWarning("Azure OpenAI not configured. Chat UI will use dummy responses.");
        }
    }

    public async Task<string> GetChatResponseAsync(string userMessage, List<ChatMessage>? conversationHistory = null)
    {
        if (!IsConfigured)
        {
            return GetDummyResponse(userMessage);
        }

        try
        {
            var chatClient = _openAIClient!.GetChatClient(_deploymentName);

            var tools = GetFunctionTools();
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(GetSystemPrompt())
            };

            if (conversationHistory != null)
            {
                messages.AddRange(conversationHistory);
            }

            messages.Add(new UserChatMessage(userMessage));

            var options = new ChatCompletionOptions();
            foreach (var tool in tools)
            {
                options.Tools.Add(tool);
            }

            var response = await chatClient.CompleteChatAsync(messages, options);

            while (response.Value.FinishReason == ChatFinishReason.ToolCalls)
            {
                var toolCalls = response.Value.ToolCalls;
                messages.Add(new AssistantChatMessage(toolCalls));

                foreach (var toolCall in toolCalls)
                {
                    var functionResult = await ExecuteFunctionAsync(toolCall.FunctionName, toolCall.FunctionArguments.ToString());
                    messages.Add(new ToolChatMessage(toolCall.Id, functionResult));
                }

                response = await chatClient.CompleteChatAsync(messages, options);
            }

            return response.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat response");
            return $"I apologize, but I encountered an error processing your request: {ex.Message}";
        }
    }

    private static string GetSystemPrompt()
    {
        return @"You are a helpful assistant for the Expense Management System. You can help users with:
- Viewing their expenses
- Creating new expenses
- Submitting expenses for approval
- Checking expense status
- Getting summaries of expenses by category or status
- Approving or rejecting expenses (for managers)

When users ask about expenses, use the available functions to retrieve real data from the database.
Format lists nicely with bullet points or numbered lists.
Always be helpful and provide clear, concise answers.
When showing amounts, display them in GBP (£) format.";
    }

    private List<ChatTool> GetFunctionTools()
    {
        return new List<ChatTool>
        {
            ChatTool.CreateFunctionTool(
                "get_expenses",
                "Retrieves a list of expenses with optional filters",
                BinaryData.FromString(@"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""statusFilter"": {
                            ""type"": ""string"",
                            ""description"": ""Filter by status: Draft, Submitted, Approved, or Rejected""
                        },
                        ""categoryFilter"": {
                            ""type"": ""string"",
                            ""description"": ""Filter by category: Travel, Meals, Supplies, Accommodation, Other""
                        }
                    }
                }")
            ),
            ChatTool.CreateFunctionTool(
                "get_pending_expenses",
                "Retrieves expenses pending approval",
                BinaryData.FromString(@"{
                    ""type"": ""object"",
                    ""properties"": {}
                }")
            ),
            ChatTool.CreateFunctionTool(
                "get_expense_summary_by_category",
                "Gets a summary of expenses grouped by category",
                BinaryData.FromString(@"{
                    ""type"": ""object"",
                    ""properties"": {}
                }")
            ),
            ChatTool.CreateFunctionTool(
                "get_expense_summary_by_status",
                "Gets a summary of expenses grouped by status",
                BinaryData.FromString(@"{
                    ""type"": ""object"",
                    ""properties"": {}
                }")
            ),
            ChatTool.CreateFunctionTool(
                "get_categories",
                "Gets all available expense categories",
                BinaryData.FromString(@"{
                    ""type"": ""object"",
                    ""properties"": {}
                }")
            ),
            ChatTool.CreateFunctionTool(
                "create_expense",
                "Creates a new expense",
                BinaryData.FromString(@"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""userId"": {
                            ""type"": ""integer"",
                            ""description"": ""The user ID creating the expense""
                        },
                        ""categoryId"": {
                            ""type"": ""integer"",
                            ""description"": ""The category ID (1=Travel, 2=Meals, 3=Supplies, 4=Accommodation, 5=Other)""
                        },
                        ""amount"": {
                            ""type"": ""number"",
                            ""description"": ""The expense amount in GBP""
                        },
                        ""expenseDate"": {
                            ""type"": ""string"",
                            ""description"": ""The date of the expense in YYYY-MM-DD format""
                        },
                        ""description"": {
                            ""type"": ""string"",
                            ""description"": ""Description of the expense""
                        }
                    },
                    ""required"": [""userId"", ""categoryId"", ""amount"", ""expenseDate""]
                }")
            ),
            ChatTool.CreateFunctionTool(
                "submit_expense",
                "Submits an expense for approval",
                BinaryData.FromString(@"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""expenseId"": {
                            ""type"": ""integer"",
                            ""description"": ""The ID of the expense to submit""
                        }
                    },
                    ""required"": [""expenseId""]
                }")
            ),
            ChatTool.CreateFunctionTool(
                "approve_expense",
                "Approves a pending expense",
                BinaryData.FromString(@"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""expenseId"": {
                            ""type"": ""integer"",
                            ""description"": ""The ID of the expense to approve""
                        },
                        ""reviewerId"": {
                            ""type"": ""integer"",
                            ""description"": ""The ID of the manager approving the expense""
                        }
                    },
                    ""required"": [""expenseId"", ""reviewerId""]
                }")
            ),
            ChatTool.CreateFunctionTool(
                "reject_expense",
                "Rejects a pending expense",
                BinaryData.FromString(@"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""expenseId"": {
                            ""type"": ""integer"",
                            ""description"": ""The ID of the expense to reject""
                        },
                        ""reviewerId"": {
                            ""type"": ""integer"",
                            ""description"": ""The ID of the manager rejecting the expense""
                        }
                    },
                    ""required"": [""expenseId"", ""reviewerId""]
                }")
            )
        };
    }

    private async Task<string> ExecuteFunctionAsync(string functionName, string arguments)
    {
        try
        {
            var args = JsonDocument.Parse(arguments);

            switch (functionName)
            {
                case "get_expenses":
                    var statusFilter = args.RootElement.TryGetProperty("statusFilter", out var sf) ? sf.GetString() : null;
                    var categoryFilter = args.RootElement.TryGetProperty("categoryFilter", out var cf) ? cf.GetString() : null;
                    var expenses = await _expenseService.GetExpensesAsync(statusFilter, categoryFilter);
                    return JsonSerializer.Serialize(expenses.Select(e => new
                    {
                        e.ExpenseId,
                        e.UserName,
                        e.CategoryName,
                        e.StatusName,
                        Amount = $"£{e.AmountGBP:F2}",
                        Date = e.ExpenseDate.ToString("dd/MM/yyyy"),
                        e.Description
                    }));

                case "get_pending_expenses":
                    var pending = await _expenseService.GetPendingExpensesAsync();
                    return JsonSerializer.Serialize(pending.Select(e => new
                    {
                        e.ExpenseId,
                        e.UserName,
                        e.CategoryName,
                        Amount = $"£{e.AmountGBP:F2}",
                        Date = e.ExpenseDate.ToString("dd/MM/yyyy"),
                        e.Description
                    }));

                case "get_expense_summary_by_category":
                    var categorySummary = await _expenseService.GetExpenseSummaryByCategoryAsync();
                    return JsonSerializer.Serialize(categorySummary);

                case "get_expense_summary_by_status":
                    var statusSummary = await _expenseService.GetExpenseSummaryByStatusAsync();
                    return JsonSerializer.Serialize(statusSummary);

                case "get_categories":
                    var categories = await _expenseService.GetCategoriesAsync();
                    return JsonSerializer.Serialize(categories);

                case "create_expense":
                    var createRequest = new CreateExpenseRequest
                    {
                        UserId = args.RootElement.GetProperty("userId").GetInt32(),
                        CategoryId = args.RootElement.GetProperty("categoryId").GetInt32(),
                        Amount = args.RootElement.GetProperty("amount").GetDecimal(),
                        ExpenseDate = DateTime.Parse(args.RootElement.GetProperty("expenseDate").GetString()!),
                        Description = args.RootElement.TryGetProperty("description", out var desc) ? desc.GetString() : null
                    };
                    var newId = await _expenseService.CreateExpenseAsync(createRequest);
                    return JsonSerializer.Serialize(new { success = true, expenseId = newId, message = "Expense created successfully" });

                case "submit_expense":
                    var submitId = args.RootElement.GetProperty("expenseId").GetInt32();
                    var submitted = await _expenseService.SubmitExpenseAsync(submitId);
                    return JsonSerializer.Serialize(new { success = submitted, message = submitted ? "Expense submitted for approval" : "Failed to submit expense" });

                case "approve_expense":
                    var approveId = args.RootElement.GetProperty("expenseId").GetInt32();
                    var approverId = args.RootElement.GetProperty("reviewerId").GetInt32();
                    var approved = await _expenseService.ApproveExpenseAsync(approveId, approverId);
                    return JsonSerializer.Serialize(new { success = approved, message = approved ? "Expense approved" : "Failed to approve expense" });

                case "reject_expense":
                    var rejectId = args.RootElement.GetProperty("expenseId").GetInt32();
                    var rejecterId = args.RootElement.GetProperty("reviewerId").GetInt32();
                    var rejected = await _expenseService.RejectExpenseAsync(rejectId, rejecterId);
                    return JsonSerializer.Serialize(new { success = rejected, message = rejected ? "Expense rejected" : "Failed to reject expense" });

                default:
                    return JsonSerializer.Serialize(new { error = $"Unknown function: {functionName}" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName}", functionName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private static string GetDummyResponse(string userMessage)
    {
        var lowerMessage = userMessage.ToLower();

        if (lowerMessage.Contains("expense") && (lowerMessage.Contains("list") || lowerMessage.Contains("show") || lowerMessage.Contains("view")))
        {
            return @"**Demo Mode - GenAI services not deployed**

Here are some sample expenses:

1. **Travel** - £120.00 (Submitted)
   - Date: 15/01/2024
   - Taxi from airport to client site

2. **Meals** - £45.50 (Approved)
   - Date: 10/01/2024
   - Client lunch meeting

3. **Supplies** - £25.99 (Draft)
   - Date: 20/01/2024
   - Office stationery

To enable AI-powered chat, deploy using `deploy-with-chat.sh` instead of `deploy.sh`.";
        }

        if (lowerMessage.Contains("pending") || lowerMessage.Contains("approve"))
        {
            return @"**Demo Mode - GenAI services not deployed**

Pending expenses for approval:

1. **Travel** - £120.00
   - Submitted by: Alice Example
   - Date: 20/01/2024

To enable AI-powered chat, deploy using `deploy-with-chat.sh` instead of `deploy.sh`.";
        }

        if (lowerMessage.Contains("help"))
        {
            return @"**Demo Mode - GenAI services not deployed**

I can help you with:
- Viewing expenses
- Creating new expenses
- Submitting expenses for approval
- Approving or rejecting expenses (managers)
- Getting expense summaries

To enable full AI capabilities, deploy using `deploy-with-chat.sh` instead of `deploy.sh`.";
        }

        return @"**Demo Mode - GenAI services not deployed**

I'm currently running in demo mode without Azure OpenAI services. 

To enable the full AI-powered experience:
1. Run `deploy-with-chat.sh` instead of `deploy.sh`
2. This will deploy Azure OpenAI and AI Search resources
3. The chat interface will then use real AI to help with expense management

Try asking me to:
- Show my expenses
- What expenses are pending approval?
- Help";
    }
}
