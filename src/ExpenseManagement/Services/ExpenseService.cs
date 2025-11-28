using Microsoft.Data.SqlClient;
using ExpenseManagement.Models;
using System.Data;

namespace ExpenseManagement.Services;

public interface IExpenseService
{
    Task<IEnumerable<Expense>> GetExpensesAsync(string? statusFilter = null, string? categoryFilter = null, int? userId = null);
    Task<Expense?> GetExpenseByIdAsync(int expenseId);
    Task<IEnumerable<Expense>> GetPendingExpensesAsync(string? categoryFilter = null);
    Task<int> CreateExpenseAsync(CreateExpenseRequest request);
    Task<bool> UpdateExpenseAsync(int expenseId, UpdateExpenseRequest request);
    Task<bool> DeleteExpenseAsync(int expenseId);
    Task<bool> SubmitExpenseAsync(int expenseId);
    Task<bool> ApproveExpenseAsync(int expenseId, int reviewerId);
    Task<bool> RejectExpenseAsync(int expenseId, int reviewerId);
    Task<IEnumerable<ExpenseCategory>> GetCategoriesAsync();
    Task<IEnumerable<ExpenseStatus>> GetStatusesAsync();
    Task<IEnumerable<User>> GetUsersAsync();
    Task<IEnumerable<User>> GetManagersAsync();
    Task<IEnumerable<ExpenseSummary>> GetExpenseSummaryByCategoryAsync();
    Task<IEnumerable<ExpenseSummary>> GetExpenseSummaryByStatusAsync();
}

public class ExpenseService : IExpenseService
{
    private readonly string _connectionString;
    private readonly ILogger<ExpenseService> _logger;
    private string? _lastError;

    public string? LastError => _lastError;

    public ExpenseService(IConfiguration configuration, ILogger<ExpenseService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    private async Task<SqlConnection> GetConnectionAsync()
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    public async Task<IEnumerable<Expense>> GetExpensesAsync(string? statusFilter = null, string? categoryFilter = null, int? userId = null)
    {
        var expenses = new List<Expense>();
        _lastError = null;

        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("GetExpenses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@StatusFilter", (object?)statusFilter ?? DBNull.Value);
            command.Parameters.AddWithValue("@CategoryFilter", (object?)categoryFilter ?? DBNull.Value);
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex);
            _logger.LogError(ex, "Error getting expenses");
            return GetDummyExpenses();
        }

        return expenses;
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        _lastError = null;

        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("GetExpenseById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapExpense(reader);
            }
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex);
            _logger.LogError(ex, "Error getting expense by ID {ExpenseId}", expenseId);
        }

        return null;
    }

    public async Task<IEnumerable<Expense>> GetPendingExpensesAsync(string? categoryFilter = null)
    {
        var expenses = new List<Expense>();
        _lastError = null;

        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("GetPendingExpenses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@CategoryFilter", (object?)categoryFilter ?? DBNull.Value);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex);
            _logger.LogError(ex, "Error getting pending expenses");
            return GetDummyExpenses().Where(e => e.StatusName == "Submitted");
        }

        return expenses;
    }

    public async Task<int> CreateExpenseAsync(CreateExpenseRequest request)
    {
        _lastError = null;

        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("CreateExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@UserId", request.UserId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", (int)(request.Amount * 100));
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReceiptFile", (object?)request.ReceiptFile ?? DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex);
            _logger.LogError(ex, "Error creating expense");
            throw;
        }
    }

    public async Task<bool> UpdateExpenseAsync(int expenseId, UpdateExpenseRequest request)
    {
        _lastError = null;

        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("UpdateExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", (int)(request.Amount * 100));
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReceiptFile", (object?)request.ReceiptFile ?? DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex);
            _logger.LogError(ex, "Error updating expense {ExpenseId}", expenseId);
            throw;
        }
    }

    public async Task<bool> DeleteExpenseAsync(int expenseId)
    {
        _lastError = null;

        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("DeleteExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex);
            _logger.LogError(ex, "Error deleting expense {ExpenseId}", expenseId);
            throw;
        }
    }

    public async Task<bool> SubmitExpenseAsync(int expenseId)
    {
        _lastError = null;

        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("SubmitExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex);
            _logger.LogError(ex, "Error submitting expense {ExpenseId}", expenseId);
            throw;
        }
    }

    public async Task<bool> ApproveExpenseAsync(int expenseId, int reviewerId)
    {
        _lastError = null;

        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("ApproveExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewerId", reviewerId);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex);
            _logger.LogError(ex, "Error approving expense {ExpenseId}", expenseId);
            throw;
        }
    }

    public async Task<bool> RejectExpenseAsync(int expenseId, int reviewerId)
    {
        _lastError = null;

        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("RejectExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewerId", reviewerId);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex);
            _logger.LogError(ex, "Error rejecting expense {ExpenseId}", expenseId);
            throw;
        }
    }

    public async Task<IEnumerable<ExpenseCategory>> GetCategoriesAsync()
    {
        var categories = new List<ExpenseCategory>();
        _lastError = null;

        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("GetCategories", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new ExpenseCategory
                {
                    CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                });
            }
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex);
            _logger.LogError(ex, "Error getting categories");
            return GetDummyCategories();
        }

        return categories;
    }

    public async Task<IEnumerable<ExpenseStatus>> GetStatusesAsync()
    {
        var statuses = new List<ExpenseStatus>();
        _lastError = null;

        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("GetStatuses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                statuses.Add(new ExpenseStatus
                {
                    StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
                    StatusName = reader.GetString(reader.GetOrdinal("StatusName"))
                });
            }
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex);
            _logger.LogError(ex, "Error getting statuses");
            return GetDummyStatuses();
        }

        return statuses;
    }

    public async Task<IEnumerable<User>> GetUsersAsync()
    {
        var users = new List<User>();
        _lastError = null;

        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("GetUsers", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(MapUser(reader));
            }
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex);
            _logger.LogError(ex, "Error getting users");
            return GetDummyUsers();
        }

        return users;
    }

    public async Task<IEnumerable<User>> GetManagersAsync()
    {
        var managers = new List<User>();
        _lastError = null;

        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("GetManagers", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                managers.Add(new User
                {
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    UserName = reader.GetString(reader.GetOrdinal("UserName")),
                    Email = reader.GetString(reader.GetOrdinal("Email"))
                });
            }
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex);
            _logger.LogError(ex, "Error getting managers");
            return GetDummyUsers().Where(u => u.RoleName == "Manager");
        }

        return managers;
    }

    public async Task<IEnumerable<ExpenseSummary>> GetExpenseSummaryByCategoryAsync()
    {
        var summaries = new List<ExpenseSummary>();
        _lastError = null;

        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("GetExpenseSummaryByCategory", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                summaries.Add(new ExpenseSummary
                {
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    TotalExpenses = reader.GetInt32(reader.GetOrdinal("TotalExpenses")),
                    TotalAmountMinor = reader.GetInt32(reader.GetOrdinal("TotalAmountMinor")),
                    TotalAmountGBP = reader.GetDecimal(reader.GetOrdinal("TotalAmountGBP"))
                });
            }
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex);
            _logger.LogError(ex, "Error getting expense summary by category");
        }

        return summaries;
    }

    public async Task<IEnumerable<ExpenseSummary>> GetExpenseSummaryByStatusAsync()
    {
        var summaries = new List<ExpenseSummary>();
        _lastError = null;

        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("GetExpenseSummaryByStatus", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                summaries.Add(new ExpenseSummary
                {
                    StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
                    TotalExpenses = reader.GetInt32(reader.GetOrdinal("TotalExpenses")),
                    TotalAmountMinor = reader.GetInt32(reader.GetOrdinal("TotalAmountMinor")),
                    TotalAmountGBP = reader.GetDecimal(reader.GetOrdinal("TotalAmountGBP"))
                });
            }
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex);
            _logger.LogError(ex, "Error getting expense summary by status");
        }

        return summaries;
    }

    private static Expense MapExpense(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32(reader.GetOrdinal("ExpenseId")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
            CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
            StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
            StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
            AmountMinor = reader.GetInt32(reader.GetOrdinal("AmountMinor")),
            AmountGBP = reader.GetDecimal(reader.GetOrdinal("AmountGBP")),
            Currency = reader.GetString(reader.GetOrdinal("Currency")),
            ExpenseDate = reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            ReceiptFile = reader.IsDBNull(reader.GetOrdinal("ReceiptFile")) ? null : reader.GetString(reader.GetOrdinal("ReceiptFile")),
            SubmittedAt = reader.IsDBNull(reader.GetOrdinal("SubmittedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    private static User MapUser(SqlDataReader reader)
    {
        return new User
        {
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
            RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
            ManagerId = reader.IsDBNull(reader.GetOrdinal("ManagerId")) ? null : reader.GetInt32(reader.GetOrdinal("ManagerId")),
            ManagerName = reader.IsDBNull(reader.GetOrdinal("ManagerName")) ? null : reader.GetString(reader.GetOrdinal("ManagerName")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    private string FormatError(Exception ex)
    {
        var errorMessage = $"Error in {ex.TargetSite?.DeclaringType?.Name ?? "Unknown"}.{ex.TargetSite?.Name ?? "Unknown"}: {ex.Message}";
        
        if (ex is SqlException sqlEx)
        {
            if (sqlEx.Message.Contains("Login failed") || sqlEx.Message.Contains("Cannot open database"))
            {
                errorMessage += "\n\nManaged Identity Issue: Ensure the managed identity has been granted access to the database. " +
                    "Run the script.sql file to create the user and assign roles: " +
                    "CREATE USER [identity-name] FROM EXTERNAL PROVIDER; " +
                    "ALTER ROLE db_datareader ADD MEMBER [identity-name]; " +
                    "ALTER ROLE db_datawriter ADD MEMBER [identity-name];";
            }
        }

        return errorMessage;
    }

    private static IEnumerable<Expense> GetDummyExpenses()
    {
        return new List<Expense>
        {
            new() { ExpenseId = 1, UserName = "Demo User", CategoryName = "Travel", StatusName = "Submitted", AmountGBP = 120.00m, ExpenseDate = DateTime.Now.AddDays(-5), Description = "Demo expense - Database not connected" },
            new() { ExpenseId = 2, UserName = "Demo User", CategoryName = "Meals", StatusName = "Approved", AmountGBP = 45.50m, ExpenseDate = DateTime.Now.AddDays(-10), Description = "Demo expense - Database not connected" },
            new() { ExpenseId = 3, UserName = "Demo User", CategoryName = "Supplies", StatusName = "Draft", AmountGBP = 25.99m, ExpenseDate = DateTime.Now.AddDays(-2), Description = "Demo expense - Database not connected" }
        };
    }

    private static IEnumerable<ExpenseCategory> GetDummyCategories()
    {
        return new List<ExpenseCategory>
        {
            new() { CategoryId = 1, CategoryName = "Travel", IsActive = true },
            new() { CategoryId = 2, CategoryName = "Meals", IsActive = true },
            new() { CategoryId = 3, CategoryName = "Supplies", IsActive = true },
            new() { CategoryId = 4, CategoryName = "Accommodation", IsActive = true },
            new() { CategoryId = 5, CategoryName = "Other", IsActive = true }
        };
    }

    private static IEnumerable<ExpenseStatus> GetDummyStatuses()
    {
        return new List<ExpenseStatus>
        {
            new() { StatusId = 1, StatusName = "Draft" },
            new() { StatusId = 2, StatusName = "Submitted" },
            new() { StatusId = 3, StatusName = "Approved" },
            new() { StatusId = 4, StatusName = "Rejected" }
        };
    }

    private static IEnumerable<User> GetDummyUsers()
    {
        return new List<User>
        {
            new() { UserId = 1, UserName = "Demo Employee", Email = "demo@example.com", RoleName = "Employee", IsActive = true },
            new() { UserId = 2, UserName = "Demo Manager", Email = "manager@example.com", RoleName = "Manager", IsActive = true }
        };
    }
}
