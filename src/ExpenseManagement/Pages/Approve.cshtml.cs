using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class ApproveModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<ApproveModel> _logger;

    public IEnumerable<Expense>? PendingExpenses { get; set; }
    public IEnumerable<ExpenseCategory>? Categories { get; set; }
    public IEnumerable<User>? Managers { get; set; }
    public int SelectedReviewerId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? CategoryFilter { get; set; }

    public ApproveModel(IExpenseService expenseService, ILogger<ApproveModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostApproveAsync(int expenseId, int reviewerId)
    {
        try
        {
            if (reviewerId == 0)
            {
                var managers = await _expenseService.GetManagersAsync();
                reviewerId = managers.FirstOrDefault()?.UserId ?? 2;
            }

            await _expenseService.ApproveExpenseAsync(expenseId, reviewerId);
            TempData["Success"] = "Expense approved successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense {ExpenseId}", expenseId);
            TempData["Error"] = $"Error approving expense: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int expenseId, int reviewerId)
    {
        try
        {
            if (reviewerId == 0)
            {
                var managers = await _expenseService.GetManagersAsync();
                reviewerId = managers.FirstOrDefault()?.UserId ?? 2;
            }

            await _expenseService.RejectExpenseAsync(expenseId, reviewerId);
            TempData["Success"] = "Expense rejected";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting expense {ExpenseId}", expenseId);
            TempData["Error"] = $"Error rejecting expense: {ex.Message}";
        }

        return RedirectToPage();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            Categories = await _expenseService.GetCategoriesAsync();
            Managers = await _expenseService.GetManagersAsync();
            PendingExpenses = await _expenseService.GetPendingExpensesAsync(CategoryFilter);
            SelectedReviewerId = Managers?.FirstOrDefault()?.UserId ?? 2;

            if (_expenseService is ExpenseService service && service.LastError != null)
            {
                ViewData["Error"] = service.LastError;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading pending expenses");
            ViewData["Error"] = $"Error loading data: {ex.Message}";
        }
    }
}
