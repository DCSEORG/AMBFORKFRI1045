using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<IndexModel> _logger;

    public IEnumerable<Expense>? RecentExpenses { get; set; }
    public IEnumerable<ExpenseSummary>? CategorySummary { get; set; }

    public IndexModel(IExpenseService expenseService, ILogger<IndexModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            RecentExpenses = await _expenseService.GetExpensesAsync();
            CategorySummary = await _expenseService.GetExpenseSummaryByCategoryAsync();
            
            if (_expenseService is ExpenseService service && service.LastError != null)
            {
                ViewData["Error"] = service.LastError;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard data");
            ViewData["Error"] = $"Error loading data: {ex.Message}";
        }
    }
}
