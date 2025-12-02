namespace ExpenseManagement.Models;

public class Expense
{
    public int ExpenseId { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int StatusId { get; set; }
    public string? StatusName { get; set; }
    public int AmountMinor { get; set; } // Amount in pence
    public string Currency { get; set; } = "GBP";
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }
    public string? ReceiptFile { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int? ReviewedBy { get; set; }
    public string? ReviewerName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Computed property for display
    public decimal AmountGBP => AmountMinor / 100m;
    public string FormattedAmount => $"£{AmountGBP:N2}";
}

public class ExpenseCategory
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class ExpenseStatus
{
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
}

public class User
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string? RoleName { get; set; }
    public int? ManagerId { get; set; }
    public bool IsActive { get; set; }
}

public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateExpenseRequest
{
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public int CategoryId { get; set; }
    public string? Description { get; set; }
    public int UserId { get; set; } = 1; // Default to first user
}

public class ApproveExpenseRequest
{
    public int ExpenseId { get; set; }
    public int ReviewerId { get; set; }
    public bool Approve { get; set; }
}

public class ExpenseFilter
{
    public string? Category { get; set; }
    public string? Status { get; set; }
    public string? SearchText { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class ErrorInfo
{
    public string Message { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public int? LineNumber { get; set; }
    public string? DetailedMessage { get; set; }
    
    public static ErrorInfo Create(
        string message,
        string? detailedMessage = null,
        [System.Runtime.CompilerServices.CallerFilePath] string? filePath = null,
        [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
    {
        return new ErrorInfo
        {
            Message = message,
            DetailedMessage = detailedMessage,
            FileName = System.IO.Path.GetFileName(filePath),
            LineNumber = lineNumber
        };
    }
}

public class ChatMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<ChatMessage>? History { get; set; }
}

public class ChatResponse
{
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
}
