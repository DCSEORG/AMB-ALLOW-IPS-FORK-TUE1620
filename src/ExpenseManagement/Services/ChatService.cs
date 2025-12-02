using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using ExpenseManagement.Models;
using System.Text.Json;

namespace ExpenseManagement.Services;

public interface IChatService
{
    Task<ChatResponse> SendMessageAsync(ChatRequest request);
    bool IsConfigured { get; }
}

public class ChatService : IChatService
{
    private readonly IConfiguration _configuration;
    private readonly IExpenseService _expenseService;
    private readonly ILogger<ChatService> _logger;
    private readonly bool _isConfigured;

    public bool IsConfigured => _isConfigured;

    public ChatService(IConfiguration configuration, IExpenseService expenseService, ILogger<ChatService> logger)
    {
        _configuration = configuration;
        _expenseService = expenseService;
        _logger = logger;

        var endpoint = _configuration["OpenAI:Endpoint"];
        _isConfigured = !string.IsNullOrEmpty(endpoint);
    }

    public async Task<ChatResponse> SendMessageAsync(ChatRequest request)
    {
        if (!_isConfigured)
        {
            return new ChatResponse
            {
                Success = true,
                Message = "GenAI services are not configured. To enable AI chat functionality, deploy using deploy-with-chat.sh to provision Azure OpenAI resources. " +
                          "You can still use all other features of the Expense Management System."
            };
        }

        try
        {
            var endpoint = _configuration["OpenAI:Endpoint"]!;
            var deploymentName = _configuration["OpenAI:DeploymentName"] ?? "gpt-4o";
            var managedIdentityClientId = _configuration["ManagedIdentityClientId"];

            Azure.Core.TokenCredential credential;
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

            var client = new OpenAIClient(new Uri(endpoint), credential);

            // Define available functions for the AI to call
            var functions = GetFunctionDefinitions();
            
            var chatOptions = new ChatCompletionsOptions
            {
                DeploymentName = deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(GetSystemPrompt())
                }
            };

            // Add history if provided
            if (request.History != null)
            {
                foreach (var msg in request.History)
                {
                    if (msg.Role == "user")
                        chatOptions.Messages.Add(new ChatRequestUserMessage(msg.Content));
                    else if (msg.Role == "assistant")
                        chatOptions.Messages.Add(new ChatRequestAssistantMessage(msg.Content));
                }
            }

            chatOptions.Messages.Add(new ChatRequestUserMessage(request.Message));

            // Add function definitions
            foreach (var func in functions)
            {
                chatOptions.Tools.Add(func);
            }

            var response = await client.GetChatCompletionsAsync(chatOptions);
            var choice = response.Value.Choices[0];

            // Handle function calls
            while (choice.FinishReason == CompletionsFinishReason.ToolCalls)
            {
                var toolCalls = choice.Message.ToolCalls;
                chatOptions.Messages.Add(new ChatRequestAssistantMessage(choice.Message));

                foreach (var toolCall in toolCalls)
                {
                    if (toolCall is ChatCompletionsFunctionToolCall functionCall)
                    {
                        var functionResult = await ExecuteFunctionAsync(functionCall.Name, functionCall.Arguments);
                        chatOptions.Messages.Add(new ChatRequestToolMessage(functionResult, functionCall.Id));
                    }
                }

                response = await client.GetChatCompletionsAsync(chatOptions);
                choice = response.Value.Choices[0];
            }

            return new ChatResponse
            {
                Success = true,
                Message = choice.Message.Content ?? "I processed your request but have no response to provide."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat service");
            return new ChatResponse
            {
                Success = false,
                Message = "An error occurred while processing your request.",
                Error = ex.Message
            };
        }
    }

    private string GetSystemPrompt()
    {
        return @"You are an AI assistant for the Expense Management System. You can help users with:

1. **Viewing Expenses**: List all expenses, filter by category or status, or search for specific expenses.
2. **Creating Expenses**: Help users submit new expense claims with amount, date, category, and description.
3. **Approving/Rejecting Expenses**: Managers can approve or reject submitted expenses.
4. **Understanding the System**: Explain how expense management works, statuses, categories, etc.

Available functions:
- get_expenses: Retrieve list of expenses, optionally filtered by category or status
- get_pending_expenses: Get expenses awaiting approval
- get_categories: Get available expense categories
- create_expense: Submit a new expense
- approve_expense: Approve a pending expense (managers only)
- reject_expense: Reject a pending expense (managers only)

When displaying lists of expenses, format them nicely with:
- Clear headers
- Amounts in GBP format (£X.XX)
- Dates in readable format
- Status clearly indicated

Be helpful, concise, and guide users through the expense management process.";
    }

    private List<ChatCompletionsFunctionToolDefinition> GetFunctionDefinitions()
    {
        return new List<ChatCompletionsFunctionToolDefinition>
        {
            new ChatCompletionsFunctionToolDefinition
            {
                Name = "get_expenses",
                Description = "Retrieves a list of expenses. Can be filtered by category or status.",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new
                    {
                        category = new { type = "string", description = "Filter by category name (e.g., Travel, Meals, Supplies)" },
                        status = new { type = "string", description = "Filter by status (Draft, Submitted, Approved, Rejected)" }
                    }
                })
            },
            new ChatCompletionsFunctionToolDefinition
            {
                Name = "get_pending_expenses",
                Description = "Retrieves all expenses that are pending approval (status = Submitted).",
                Parameters = BinaryData.FromObjectAsJson(new { type = "object", properties = new { } })
            },
            new ChatCompletionsFunctionToolDefinition
            {
                Name = "get_categories",
                Description = "Retrieves all available expense categories.",
                Parameters = BinaryData.FromObjectAsJson(new { type = "object", properties = new { } })
            },
            new ChatCompletionsFunctionToolDefinition
            {
                Name = "create_expense",
                Description = "Creates a new expense claim.",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new
                    {
                        amount = new { type = "number", description = "Amount in GBP (e.g., 25.50)" },
                        category_id = new { type = "integer", description = "Category ID (1=Travel, 2=Meals, 3=Supplies, 4=Accommodation, 5=Other)" },
                        expense_date = new { type = "string", description = "Date of expense in YYYY-MM-DD format" },
                        description = new { type = "string", description = "Description of the expense" }
                    },
                    required = new[] { "amount", "category_id", "expense_date" }
                })
            },
            new ChatCompletionsFunctionToolDefinition
            {
                Name = "approve_expense",
                Description = "Approves a pending expense. Only managers can approve expenses.",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new
                    {
                        expense_id = new { type = "integer", description = "ID of the expense to approve" }
                    },
                    required = new[] { "expense_id" }
                })
            },
            new ChatCompletionsFunctionToolDefinition
            {
                Name = "reject_expense",
                Description = "Rejects a pending expense. Only managers can reject expenses.",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new
                    {
                        expense_id = new { type = "integer", description = "ID of the expense to reject" }
                    },
                    required = new[] { "expense_id" }
                })
            }
        };
    }

    private async Task<string> ExecuteFunctionAsync(string functionName, string arguments)
    {
        try
        {
            var args = JsonSerializer.Deserialize<JsonElement>(arguments);

            switch (functionName)
            {
                case "get_expenses":
                    var filter = new ExpenseFilter();
                    if (args.TryGetProperty("category", out var cat))
                        filter.Category = cat.GetString();
                    if (args.TryGetProperty("status", out var stat))
                        filter.Status = stat.GetString();
                    var expenses = await _expenseService.GetExpensesAsync(filter);
                    return JsonSerializer.Serialize(expenses.Select(e => new
                    {
                        e.ExpenseId,
                        e.UserName,
                        e.CategoryName,
                        Amount = e.FormattedAmount,
                        Date = e.ExpenseDate.ToString("dd/MM/yyyy"),
                        e.StatusName,
                        e.Description
                    }));

                case "get_pending_expenses":
                    var pending = await _expenseService.GetPendingExpensesAsync();
                    return JsonSerializer.Serialize(pending.Select(e => new
                    {
                        e.ExpenseId,
                        e.UserName,
                        e.CategoryName,
                        Amount = e.FormattedAmount,
                        Date = e.ExpenseDate.ToString("dd/MM/yyyy"),
                        e.Description
                    }));

                case "get_categories":
                    var categories = await _expenseService.GetCategoriesAsync();
                    return JsonSerializer.Serialize(categories);

                case "create_expense":
                    var createRequest = new CreateExpenseRequest
                    {
                        Amount = args.GetProperty("amount").GetDecimal(),
                        CategoryId = args.GetProperty("category_id").GetInt32(),
                        ExpenseDate = DateTime.Parse(args.GetProperty("expense_date").GetString()!),
                        Description = args.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                        UserId = 1 // Default user
                    };
                    var newId = await _expenseService.CreateExpenseAsync(createRequest);
                    return JsonSerializer.Serialize(new { success = true, expense_id = newId, message = $"Expense created with ID {newId}" });

                case "approve_expense":
                    var approveId = args.GetProperty("expense_id").GetInt32();
                    var approved = await _expenseService.ApproveExpenseAsync(new ApproveExpenseRequest { ExpenseId = approveId, ReviewerId = 2 });
                    return JsonSerializer.Serialize(new { success = approved, message = approved ? "Expense approved" : "Failed to approve expense" });

                case "reject_expense":
                    var rejectId = args.GetProperty("expense_id").GetInt32();
                    var rejected = await _expenseService.RejectExpenseAsync(new ApproveExpenseRequest { ExpenseId = rejectId, ReviewerId = 2 });
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
}
