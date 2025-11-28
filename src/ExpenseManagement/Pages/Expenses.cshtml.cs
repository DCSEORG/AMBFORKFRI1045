using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class ExpensesModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<ExpensesModel> _logger;

    public IEnumerable<Expense>? Expenses { get; set; }
    public IEnumerable<ExpenseCategory>? Categories { get; set; }
    public IEnumerable<ExpenseStatus>? Statuses { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? CategoryFilter { get; set; }

    public ExpensesModel(IExpenseService expenseService, ILogger<ExpensesModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            Categories = await _expenseService.GetCategoriesAsync();
            Statuses = await _expenseService.GetStatusesAsync();
            Expenses = await _expenseService.GetExpensesAsync(StatusFilter, CategoryFilter);

            if (_expenseService is ExpenseService service && service.LastError != null)
            {
                ViewData["Error"] = service.LastError;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading expenses");
            ViewData["Error"] = $"Error loading expenses: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostSubmitAsync(int expenseId)
    {
        try
        {
            await _expenseService.SubmitExpenseAsync(expenseId);
            TempData["Success"] = "Expense submitted for approval";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting expense {ExpenseId}", expenseId);
            TempData["Error"] = $"Error submitting expense: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int expenseId)
    {
        try
        {
            await _expenseService.DeleteExpenseAsync(expenseId);
            TempData["Success"] = "Expense deleted";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense {ExpenseId}", expenseId);
            TempData["Error"] = $"Error deleting expense: {ex.Message}";
        }

        return RedirectToPage();
    }
}
