using Microsoft.Data.SqlClient;
using ExpenseManagement.Models;
using System.Data;

namespace ExpenseManagement.Services;

public interface IDatabaseService
{
    Task<SqlConnection> GetConnectionAsync();
    bool IsConnected { get; }
    ErrorInfo? LastError { get; }
}

public class DatabaseService : IDatabaseService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseService> _logger;
    private bool _isConnected;
    private ErrorInfo? _lastError;

    public bool IsConnected => _isConnected;
    public ErrorInfo? LastError => _lastError;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SqlConnection> GetConnectionAsync()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");
            }

            var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            _isConnected = true;
            _lastError = null;
            return connection;
        }
        catch (SqlException ex)
        {
            _isConnected = false;
            _lastError = ErrorInfo.Create(
                "Database connection failed",
                GetDetailedSqlErrorMessage(ex));
            _logger.LogError(ex, "Failed to connect to database");
            throw;
        }
        catch (Exception ex)
        {
            _isConnected = false;
            _lastError = ErrorInfo.Create(
                "Unexpected error connecting to database",
                ex.Message);
            _logger.LogError(ex, "Unexpected error connecting to database");
            throw;
        }
    }

    private string GetDetailedSqlErrorMessage(SqlException ex)
    {
        if (ex.Message.Contains("Login failed"))
        {
            return "Login failed. If using Managed Identity, ensure the managed identity has been granted access to the database. " +
                   "Run the script.sql to create the user and assign roles: " +
                   "CREATE USER [managed-identity-name] FROM EXTERNAL PROVIDER; " +
                   "ALTER ROLE db_datareader ADD MEMBER [managed-identity-name]; " +
                   "ALTER ROLE db_datawriter ADD MEMBER [managed-identity-name];";
        }
        
        if (ex.Message.Contains("Cannot open server"))
        {
            return "Cannot connect to SQL Server. Verify the server name is correct and that your IP address is allowed through the firewall.";
        }

        if (ex.Message.Contains("Managed Identity"))
        {
            return "Managed Identity authentication failed. Ensure AZURE_CLIENT_ID is set in the App Service configuration " +
                   "and the managed identity has 'Cognitive Services OpenAI User' role on the Azure OpenAI resource.";
        }

        return ex.Message;
    }
}
