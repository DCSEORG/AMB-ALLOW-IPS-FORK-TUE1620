using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class AddExpenseModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<AddExpenseModel> _logger;

    public List<ExpenseCategory> Categories { get; set; } = new();
    public ErrorInfo? ErrorInfo { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty]
    public decimal Amount { get; set; }

    [BindProperty]
    public DateTime? ExpenseDate { get; set; }

    [BindProperty]
    public int CategoryId { get; set; }

    [BindProperty]
    public string? Description { get; set; }

    public AddExpenseModel(IExpenseService expenseService, ILogger<AddExpenseModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        Categories = await _expenseService.GetCategoriesAsync();
        ExpenseDate = DateTime.Today;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Categories = await _expenseService.GetCategoriesAsync();

        if (!ExpenseDate.HasValue || Amount <= 0 || CategoryId <= 0)
        {
            ErrorInfo = new ErrorInfo
            {
                Message = "Please fill in all required fields",
                DetailedMessage = "Amount must be greater than 0, and you must select a category and date."
            };
            return Page();
        }

        try
        {
            var request = new CreateExpenseRequest
            {
                Amount = Amount,
                ExpenseDate = ExpenseDate.Value,
                CategoryId = CategoryId,
                Description = Description,
                UserId = 1 // Default user
            };

            var expenseId = await _expenseService.CreateExpenseAsync(request);
            SuccessMessage = $"Expense #{expenseId} created successfully!";
            
            // Reset form
            Amount = 0;
            ExpenseDate = DateTime.Today;
            CategoryId = 0;
            Description = null;
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            ErrorInfo = ErrorInfo.Create(
                "Failed to create expense",
                ex.Message);
            return Page();
        }
    }
}
