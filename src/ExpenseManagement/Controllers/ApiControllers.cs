using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(IExpenseService expenseService, ILogger<ExpensesController> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    /// <summary>
    /// Get all expenses with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Expense>>> GetExpenses(
        [FromQuery] string? category = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null)
    {
        var filter = new ExpenseFilter
        {
            Category = category,
            Status = status,
            SearchText = search
        };
        
        var expenses = await _expenseService.GetExpensesAsync(filter);
        return Ok(expenses);
    }

    /// <summary>
    /// Get expenses pending approval
    /// </summary>
    [HttpGet("pending")]
    public async Task<ActionResult<List<Expense>>> GetPendingExpenses()
    {
        var expenses = await _expenseService.GetPendingExpensesAsync();
        return Ok(expenses);
    }

    /// <summary>
    /// Get a specific expense by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Expense>> GetExpense(int id)
    {
        var expense = await _expenseService.GetExpenseByIdAsync(id);
        if (expense == null)
        {
            return NotFound();
        }
        return Ok(expense);
    }

    /// <summary>
    /// Create a new expense
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<int>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        try
        {
            var expenseId = await _expenseService.CreateExpenseAsync(request);
            return CreatedAtAction(nameof(GetExpense), new { id = expenseId }, expenseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Submit an expense for approval
    /// </summary>
    [HttpPost("{id}/submit")]
    public async Task<ActionResult> SubmitExpense(int id)
    {
        var result = await _expenseService.SubmitExpenseAsync(id);
        if (!result)
        {
            return BadRequest(new { error = "Failed to submit expense" });
        }
        return Ok(new { message = "Expense submitted successfully" });
    }

    /// <summary>
    /// Approve an expense
    /// </summary>
    [HttpPost("{id}/approve")]
    public async Task<ActionResult> ApproveExpense(int id, [FromBody] ApproveExpenseRequest request)
    {
        request.ExpenseId = id;
        request.Approve = true;
        
        var result = await _expenseService.ApproveExpenseAsync(request);
        if (!result)
        {
            return BadRequest(new { error = "Failed to approve expense" });
        }
        return Ok(new { message = "Expense approved successfully" });
    }

    /// <summary>
    /// Reject an expense
    /// </summary>
    [HttpPost("{id}/reject")]
    public async Task<ActionResult> RejectExpense(int id, [FromBody] ApproveExpenseRequest request)
    {
        request.ExpenseId = id;
        request.Approve = false;
        
        var result = await _expenseService.RejectExpenseAsync(request);
        if (!result)
        {
            return BadRequest(new { error = "Failed to reject expense" });
        }
        return Ok(new { message = "Expense rejected successfully" });
    }
}

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public CategoriesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>
    /// Get all expense categories
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ExpenseCategory>>> GetCategories()
    {
        var categories = await _expenseService.GetCategoriesAsync();
        return Ok(categories);
    }
}

[ApiController]
[Route("api/[controller]")]
public class StatusesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public StatusesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>
    /// Get all expense statuses
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ExpenseStatus>>> GetStatuses()
    {
        var statuses = await _expenseService.GetStatusesAsync();
        return Ok(statuses);
    }
}

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public UsersController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<User>>> GetUsers()
    {
        var users = await _expenseService.GetUsersAsync();
        return Ok(users);
    }
}

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Send a chat message to the AI assistant
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] ChatRequest request)
    {
        var response = await _chatService.SendMessageAsync(request);
        return Ok(response);
    }

    /// <summary>
    /// Check if chat service is configured
    /// </summary>
    [HttpGet("status")]
    public ActionResult<object> GetStatus()
    {
        return Ok(new { configured = _chatService.IsConfigured });
    }
}
