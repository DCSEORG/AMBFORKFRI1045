using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Api;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>
    /// Gets all expenses with optional filters
    /// </summary>
    /// <param name="status">Filter by status (Draft, Submitted, Approved, Rejected)</param>
    /// <param name="category">Filter by category</param>
    /// <param name="userId">Filter by user ID</param>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Expense>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Expense>>> GetExpenses(
        [FromQuery] string? status = null,
        [FromQuery] string? category = null,
        [FromQuery] int? userId = null)
    {
        var expenses = await _expenseService.GetExpensesAsync(status, category, userId);
        return Ok(expenses);
    }

    /// <summary>
    /// Gets a specific expense by ID
    /// </summary>
    /// <param name="id">Expense ID</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Expense), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    /// Gets all pending expenses (submitted but not yet approved/rejected)
    /// </summary>
    /// <param name="category">Optional category filter</param>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IEnumerable<Expense>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Expense>>> GetPendingExpenses([FromQuery] string? category = null)
    {
        var expenses = await _expenseService.GetPendingExpensesAsync(category);
        return Ok(expenses);
    }

    /// <summary>
    /// Creates a new expense
    /// </summary>
    /// <param name="request">Expense creation request</param>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        try
        {
            var expenseId = await _expenseService.CreateExpenseAsync(request);
            return CreatedAtAction(nameof(GetExpense), new { id = expenseId }, new { expenseId });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing expense
    /// </summary>
    /// <param name="id">Expense ID</param>
    /// <param name="request">Update request</param>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateExpense(int id, [FromBody] UpdateExpenseRequest request)
    {
        try
        {
            var success = await _expenseService.UpdateExpenseAsync(id, request);
            if (!success)
            {
                return NotFound();
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes an expense
    /// </summary>
    /// <param name="id">Expense ID</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteExpense(int id)
    {
        try
        {
            var success = await _expenseService.DeleteExpenseAsync(id);
            if (!success)
            {
                return NotFound();
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Submits an expense for approval
    /// </summary>
    /// <param name="id">Expense ID</param>
    [HttpPost("{id}/submit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SubmitExpense(int id)
    {
        try
        {
            var success = await _expenseService.SubmitExpenseAsync(id);
            if (!success)
            {
                return NotFound();
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Approves an expense
    /// </summary>
    /// <param name="id">Expense ID</param>
    /// <param name="request">Approval request with reviewer ID</param>
    [HttpPost("{id}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ApproveExpense(int id, [FromBody] ApproveRejectRequest request)
    {
        try
        {
            var success = await _expenseService.ApproveExpenseAsync(id, request.ReviewerId);
            if (!success)
            {
                return NotFound();
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Rejects an expense
    /// </summary>
    /// <param name="id">Expense ID</param>
    /// <param name="request">Rejection request with reviewer ID</param>
    [HttpPost("{id}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RejectExpense(int id, [FromBody] ApproveRejectRequest request)
    {
        try
        {
            var success = await _expenseService.RejectExpenseAsync(id, request.ReviewerId);
            if (!success)
            {
                return NotFound();
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets expense summary grouped by category
    /// </summary>
    [HttpGet("summary/by-category")]
    [ProducesResponseType(typeof(IEnumerable<ExpenseSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ExpenseSummary>>> GetSummaryByCategory()
    {
        var summary = await _expenseService.GetExpenseSummaryByCategoryAsync();
        return Ok(summary);
    }

    /// <summary>
    /// Gets expense summary grouped by status
    /// </summary>
    [HttpGet("summary/by-status")]
    [ProducesResponseType(typeof(IEnumerable<ExpenseSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ExpenseSummary>>> GetSummaryByStatus()
    {
        var summary = await _expenseService.GetExpenseSummaryByStatusAsync();
        return Ok(summary);
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public CategoriesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>
    /// Gets all expense categories
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ExpenseCategory>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ExpenseCategory>>> GetCategories()
    {
        var categories = await _expenseService.GetCategoriesAsync();
        return Ok(categories);
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StatusesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public StatusesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>
    /// Gets all expense statuses
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ExpenseStatus>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ExpenseStatus>>> GetStatuses()
    {
        var statuses = await _expenseService.GetStatusesAsync();
        return Ok(statuses);
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public UsersController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>
    /// Gets all users
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<User>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        var users = await _expenseService.GetUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// Gets all managers
    /// </summary>
    [HttpGet("managers")]
    [ProducesResponseType(typeof(IEnumerable<User>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<User>>> GetManagers()
    {
        var managers = await _expenseService.GetManagersAsync();
        return Ok(managers);
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    /// <summary>
    /// Sends a message to the AI chat assistant
    /// </summary>
    /// <param name="request">Chat request with message</param>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        var response = await _chatService.GetChatResponseAsync(request.Message);
        return Ok(new ChatResponse { Message = response, IsConfigured = _chatService.IsConfigured });
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
}

public class ChatResponse
{
    public string Message { get; set; } = string.Empty;
    public bool IsConfigured { get; set; }
}
