using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<IndexModel> _logger;

    public List<Expense> Expenses { get; set; } = new();
    public List<ExpenseCategory> Categories { get; set; } = new();
    public List<ExpenseStatus> Statuses { get; set; } = new();
    public ErrorInfo? ErrorInfo { get; set; }
    public bool UseDummyData { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? CategoryFilter { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? SearchText { get; set; }

    public IndexModel(IExpenseService expenseService, ILogger<IndexModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        var filter = new ExpenseFilter
        {
            Category = CategoryFilter,
            Status = StatusFilter,
            SearchText = SearchText
        };

        Expenses = await _expenseService.GetExpensesAsync(filter);
        Categories = await _expenseService.GetCategoriesAsync();
        Statuses = await _expenseService.GetStatusesAsync();
        
        ErrorInfo = _expenseService.LastError;
        UseDummyData = _expenseService.UseDummyData;
    }

    public async Task<IActionResult> OnPostSubmitAsync(int expenseId)
    {
        var result = await _expenseService.SubmitExpenseAsync(expenseId);
        if (!result)
        {
            ErrorInfo = _expenseService.LastError;
        }
        return RedirectToPage();
    }
}
