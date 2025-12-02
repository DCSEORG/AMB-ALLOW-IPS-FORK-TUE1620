using Microsoft.Data.SqlClient;
using ExpenseManagement.Models;
using System.Data;

namespace ExpenseManagement.Services;

public interface IExpenseService
{
    Task<List<Expense>> GetExpensesAsync(ExpenseFilter? filter = null);
    Task<List<Expense>> GetPendingExpensesAsync();
    Task<Expense?> GetExpenseByIdAsync(int expenseId);
    Task<List<ExpenseCategory>> GetCategoriesAsync();
    Task<List<ExpenseStatus>> GetStatusesAsync();
    Task<List<User>> GetUsersAsync();
    Task<int> CreateExpenseAsync(CreateExpenseRequest request);
    Task<bool> SubmitExpenseAsync(int expenseId);
    Task<bool> ApproveExpenseAsync(ApproveExpenseRequest request);
    Task<bool> RejectExpenseAsync(ApproveExpenseRequest request);
    ErrorInfo? LastError { get; }
    bool UseDummyData { get; }
}

public class ExpenseService : IExpenseService
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<ExpenseService> _logger;
    private ErrorInfo? _lastError;
    private bool _useDummyData;

    public ErrorInfo? LastError => _lastError;
    public bool UseDummyData => _useDummyData;

    public ExpenseService(IDatabaseService databaseService, ILogger<ExpenseService> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task<List<Expense>> GetExpensesAsync(ExpenseFilter? filter = null)
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            using var command = new SqlCommand("sp_GetExpenses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            if (filter != null)
            {
                command.Parameters.AddWithValue("@CategoryName", filter.Category ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@StatusName", filter.Status ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SearchText", filter.SearchText ?? (object)DBNull.Value);
            }

            var expenses = new List<Expense>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }

            _useDummyData = false;
            _lastError = null;
            return expenses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expenses, returning dummy data");
            _useDummyData = true;
            _lastError = new ErrorInfo
            {
                Message = "Failed to retrieve expenses from database",
                FileName = "ExpenseService.cs",
                LineNumber = 55,
                DetailedMessage = ex.Message
            };
            return GetDummyExpenses();
        }
    }

    public async Task<List<Expense>> GetPendingExpensesAsync()
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            using var command = new SqlCommand("sp_GetPendingExpenses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            var expenses = new List<Expense>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }

            _useDummyData = false;
            return expenses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending expenses, returning dummy data");
            _useDummyData = true;
            _lastError = new ErrorInfo
            {
                Message = "Failed to retrieve pending expenses",
                FileName = "ExpenseService.cs",
                LineNumber = 95,
                DetailedMessage = ex.Message
            };
            return GetDummyExpenses().Where(e => e.StatusName == "Submitted").ToList();
        }
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            using var command = new SqlCommand("sp_GetExpenseById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapExpense(reader);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense by ID");
            _lastError = new ErrorInfo
            {
                Message = "Failed to retrieve expense",
                FileName = "ExpenseService.cs",
                LineNumber = 125,
                DetailedMessage = ex.Message
            };
            return GetDummyExpenses().FirstOrDefault(e => e.ExpenseId == expenseId);
        }
    }

    public async Task<List<ExpenseCategory>> GetCategoriesAsync()
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            using var command = new SqlCommand("sp_GetCategories", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            var categories = new List<ExpenseCategory>();
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
            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories, returning dummy data");
            return GetDummyCategories();
        }
    }

    public async Task<List<ExpenseStatus>> GetStatusesAsync()
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            using var command = new SqlCommand("sp_GetStatuses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            var statuses = new List<ExpenseStatus>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                statuses.Add(new ExpenseStatus
                {
                    StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
                    StatusName = reader.GetString(reader.GetOrdinal("StatusName"))
                });
            }
            return statuses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statuses, returning dummy data");
            return GetDummyStatuses();
        }
    }

    public async Task<List<User>> GetUsersAsync()
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            using var command = new SqlCommand("sp_GetUsers", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            var users = new List<User>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    UserName = reader.GetString(reader.GetOrdinal("UserName")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
                    RoleName = reader.IsDBNull(reader.GetOrdinal("RoleName")) ? null : reader.GetString(reader.GetOrdinal("RoleName")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                });
            }
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users, returning dummy data");
            return GetDummyUsers();
        }
    }

    public async Task<int> CreateExpenseAsync(CreateExpenseRequest request)
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            using var command = new SqlCommand("sp_CreateExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@UserId", request.UserId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", (int)(request.Amount * 100));
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", request.Description ?? (object)DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            _lastError = new ErrorInfo
            {
                Message = "Failed to create expense",
                FileName = "ExpenseService.cs",
                LineNumber = 250,
                DetailedMessage = ex.Message
            };
            throw;
        }
    }

    public async Task<bool> SubmitExpenseAsync(int expenseId)
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            using var command = new SqlCommand("sp_SubmitExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting expense");
            _lastError = new ErrorInfo
            {
                Message = "Failed to submit expense",
                FileName = "ExpenseService.cs",
                LineNumber = 275,
                DetailedMessage = ex.Message
            };
            return false;
        }
    }

    public async Task<bool> ApproveExpenseAsync(ApproveExpenseRequest request)
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            using var command = new SqlCommand("sp_ApproveExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@ExpenseId", request.ExpenseId);
            command.Parameters.AddWithValue("@ReviewerId", request.ReviewerId);
            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense");
            _lastError = new ErrorInfo
            {
                Message = "Failed to approve expense",
                FileName = "ExpenseService.cs",
                LineNumber = 300,
                DetailedMessage = ex.Message
            };
            return false;
        }
    }

    public async Task<bool> RejectExpenseAsync(ApproveExpenseRequest request)
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            using var command = new SqlCommand("sp_RejectExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@ExpenseId", request.ExpenseId);
            command.Parameters.AddWithValue("@ReviewerId", request.ReviewerId);
            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting expense");
            _lastError = new ErrorInfo
            {
                Message = "Failed to reject expense",
                FileName = "ExpenseService.cs",
                LineNumber = 325,
                DetailedMessage = ex.Message
            };
            return false;
        }
    }

    private Expense MapExpense(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32(reader.GetOrdinal("ExpenseId")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            UserName = reader.IsDBNull(reader.GetOrdinal("UserName")) ? null : reader.GetString(reader.GetOrdinal("UserName")),
            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
            CategoryName = reader.IsDBNull(reader.GetOrdinal("CategoryName")) ? null : reader.GetString(reader.GetOrdinal("CategoryName")),
            StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
            StatusName = reader.IsDBNull(reader.GetOrdinal("StatusName")) ? null : reader.GetString(reader.GetOrdinal("StatusName")),
            AmountMinor = reader.GetInt32(reader.GetOrdinal("AmountMinor")),
            Currency = reader.GetString(reader.GetOrdinal("Currency")),
            ExpenseDate = reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    // Dummy data for when database is unavailable
    private List<Expense> GetDummyExpenses()
    {
        return new List<Expense>
        {
            new Expense
            {
                ExpenseId = 1,
                UserId = 1,
                UserName = "Alice Example",
                CategoryId = 1,
                CategoryName = "Travel",
                StatusId = 2,
                StatusName = "Submitted",
                AmountMinor = 12000,
                Currency = "GBP",
                ExpenseDate = new DateTime(2024, 1, 15),
                Description = "Taxi to client site",
                CreatedAt = DateTime.UtcNow
            },
            new Expense
            {
                ExpenseId = 2,
                UserId = 1,
                UserName = "Alice Example",
                CategoryId = 2,
                CategoryName = "Meals",
                StatusId = 2,
                StatusName = "Submitted",
                AmountMinor = 6900,
                Currency = "GBP",
                ExpenseDate = new DateTime(2023, 10, 1),
                Description = "Client lunch",
                CreatedAt = DateTime.UtcNow
            },
            new Expense
            {
                ExpenseId = 3,
                UserId = 1,
                UserName = "Alice Example",
                CategoryId = 3,
                CategoryName = "Supplies",
                StatusId = 3,
                StatusName = "Approved",
                AmountMinor = 9950,
                Currency = "GBP",
                ExpenseDate = new DateTime(2023, 12, 4),
                Description = "Office supplies",
                CreatedAt = DateTime.UtcNow
            },
            new Expense
            {
                ExpenseId = 4,
                UserId = 1,
                UserName = "Alice Example",
                CategoryId = 1,
                CategoryName = "Travel",
                StatusId = 3,
                StatusName = "Approved",
                AmountMinor = 1920,
                Currency = "GBP",
                ExpenseDate = new DateTime(2023, 1, 18),
                Description = "Transport to meeting",
                CreatedAt = DateTime.UtcNow
            }
        };
    }

    private List<ExpenseCategory> GetDummyCategories()
    {
        return new List<ExpenseCategory>
        {
            new ExpenseCategory { CategoryId = 1, CategoryName = "Travel", IsActive = true },
            new ExpenseCategory { CategoryId = 2, CategoryName = "Meals", IsActive = true },
            new ExpenseCategory { CategoryId = 3, CategoryName = "Supplies", IsActive = true },
            new ExpenseCategory { CategoryId = 4, CategoryName = "Accommodation", IsActive = true },
            new ExpenseCategory { CategoryId = 5, CategoryName = "Other", IsActive = true }
        };
    }

    private List<ExpenseStatus> GetDummyStatuses()
    {
        return new List<ExpenseStatus>
        {
            new ExpenseStatus { StatusId = 1, StatusName = "Draft" },
            new ExpenseStatus { StatusId = 2, StatusName = "Submitted" },
            new ExpenseStatus { StatusId = 3, StatusName = "Approved" },
            new ExpenseStatus { StatusId = 4, StatusName = "Rejected" }
        };
    }

    private List<User> GetDummyUsers()
    {
        return new List<User>
        {
            new User { UserId = 1, UserName = "Alice Example", Email = "alice@example.co.uk", RoleId = 1, RoleName = "Employee", IsActive = true },
            new User { UserId = 2, UserName = "Bob Manager", Email = "bob.manager@example.co.uk", RoleId = 2, RoleName = "Manager", IsActive = true }
        };
    }
}
