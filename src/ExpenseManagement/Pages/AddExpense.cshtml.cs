using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;
using System.ComponentModel.DataAnnotations;

namespace ExpenseManagement.Pages;

public class AddExpenseModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<AddExpenseModel> _logger;

    public IEnumerable<ExpenseCategory>? Categories { get; set; }
    public IEnumerable<User>? Users { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ExpenseDate { get; set; } = DateTime.Today;

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public int UserId { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }
    }

    public AddExpenseModel(IExpenseService expenseService, ILogger<AddExpenseModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        await LoadFormDataAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadFormDataAsync();
            return Page();
        }

        try
        {
            var request = new CreateExpenseRequest
            {
                UserId = Input.UserId,
                CategoryId = Input.CategoryId,
                Amount = Input.Amount,
                ExpenseDate = Input.ExpenseDate,
                Description = Input.Description
            };

            var expenseId = await _expenseService.CreateExpenseAsync(request);
            TempData["Success"] = $"Expense created successfully (ID: {expenseId})";
            return RedirectToPage("/Expenses");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            ViewData["Error"] = $"Error creating expense: {ex.Message}";
            await LoadFormDataAsync();
            return Page();
        }
    }

    private async Task LoadFormDataAsync()
    {
        try
        {
            Categories = await _expenseService.GetCategoriesAsync();
            Users = await _expenseService.GetUsersAsync();

            if (_expenseService is ExpenseService service && service.LastError != null)
            {
                ViewData["Error"] = service.LastError;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading form data");
            ViewData["Error"] = $"Error loading form data: {ex.Message}";
        }
    }
}
