using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class ApproveModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<ApproveModel> _logger;

    public List<Expense> PendingExpenses { get; set; } = new();
    public ErrorInfo? ErrorInfo { get; set; }
    public string? SuccessMessage { get; set; }
    public bool UseDummyData { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchText { get; set; }

    public ApproveModel(IExpenseService expenseService, ILogger<ApproveModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        PendingExpenses = await _expenseService.GetPendingExpensesAsync();
        
        if (!string.IsNullOrEmpty(SearchText))
        {
            PendingExpenses = PendingExpenses
                .Where(e => 
                    (e.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (e.CategoryName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (e.UserName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        ErrorInfo = _expenseService.LastError;
        UseDummyData = _expenseService.UseDummyData;
    }

    public async Task<IActionResult> OnPostApproveAsync(int expenseId)
    {
        var request = new ApproveExpenseRequest
        {
            ExpenseId = expenseId,
            ReviewerId = 2, // Default manager
            Approve = true
        };

        var result = await _expenseService.ApproveExpenseAsync(request);
        if (result)
        {
            SuccessMessage = $"Expense #{expenseId} approved successfully!";
        }
        else
        {
            ErrorInfo = _expenseService.LastError ?? new ErrorInfo
            {
                Message = "Failed to approve expense"
            };
        }

        await OnGetAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRejectAsync(int expenseId)
    {
        var request = new ApproveExpenseRequest
        {
            ExpenseId = expenseId,
            ReviewerId = 2, // Default manager
            Approve = false
        };

        var result = await _expenseService.RejectExpenseAsync(request);
        if (result)
        {
            SuccessMessage = $"Expense #{expenseId} rejected.";
        }
        else
        {
            ErrorInfo = _expenseService.LastError ?? new ErrorInfo
            {
                Message = "Failed to reject expense"
            };
        }

        await OnGetAsync();
        return Page();
    }
}
