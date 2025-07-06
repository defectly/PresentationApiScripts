using Microsoft.Data.SqlClient;

namespace DeleteOldEmployees;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            var connectionString = GetConnectionString();
            Console.WriteLine("Connection string configured successfully from environment variables!");
            
            await DeleteOldEmployeesAsync(connectionString);
            
            Console.WriteLine("Delete old employees operation completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static string GetConnectionString()
    {
        var config = LoadEnvironmentVariables("config.env");
        
        string dbServer = config.GetValueOrDefault("DB_SERVER", Environment.GetEnvironmentVariable("DB_SERVER") ?? "localhost");
        string dbDatabase = config.GetValueOrDefault("DB_DATABASE", Environment.GetEnvironmentVariable("DB_DATABASE") ?? "CompanyPresentationApi");
        string dbUserId = config.GetValueOrDefault("DB_USER_ID", Environment.GetEnvironmentVariable("DB_USER_ID") ?? "SA");
        string dbPassword = config.GetValueOrDefault("DB_PASSWORD", Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "");
        string dbTrustServerCertificate = config.GetValueOrDefault("DB_TRUST_SERVER_CERTIFICATE", Environment.GetEnvironmentVariable("DB_TRUST_SERVER_CERTIFICATE") ?? "true");
        
        return $"Server={dbServer};Database={dbDatabase};User Id={dbUserId};Password={dbPassword};TrustServerCertificate={dbTrustServerCertificate};";
    }

    private static Dictionary<string, string> LoadEnvironmentVariables(string filePath)
    {
        var envVars = new Dictionary<string, string>();
        
        if (File.Exists(filePath))
        {
            foreach (var line in File.ReadAllLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;
                    
                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    envVars[parts[0].Trim()] = parts[1].Trim();
                }
            }
        }
        else
        {
            Console.WriteLine($"Warning: Config file '{filePath}' not found. Using environment variables.");
        }
        
        return envVars;
    }

    private static async Task DeleteOldEmployeesAsync(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        string selectQuery = @"
            SELECT 
                e.Id,
                e.FirstName,
                e.MiddleName,
                e.LastName,
                e.BirthDate,
                d.Name as DepartmentName,
                DATEDIFF(YEAR, e.BirthDate, GETDATE()) as Age
            FROM Employees e
            INNER JOIN Departments d ON e.DepartmentId = d.Id
            WHERE DATEDIFF(YEAR, e.BirthDate, GETDATE()) > 70
            ORDER BY e.BirthDate
        ";
        
        using var selectCommand = new SqlCommand(selectQuery, connection);
        using var reader = await selectCommand.ExecuteReaderAsync();
        
        Console.WriteLine("Employees older than 70 years:");
        Console.WriteLine("Name\t\t\t\tDepartment\t\tAge\tBirth Date");
        Console.WriteLine(new string('-', 80));
        
        int count = 0;
        while (await reader.ReadAsync())
        {
            string name = $"{reader["FirstName"]} {reader["MiddleName"]} {reader["LastName"]}";
            string department = reader["DepartmentName"].ToString() ?? string.Empty;
            int age = (int)reader["Age"];
            DateTime birthDate = (DateTime)reader["BirthDate"];
            
            Console.WriteLine($"{name.PadRight(30)}\t{department.PadRight(15)}\t{age}\t{birthDate:yyyy-MM-dd}");
            count++;
        }
        
        if (count > 0)
        {
            Console.WriteLine($"\nFound {count} employees older than 70 years");
            Console.WriteLine("Deleting these employees...");
            
            reader.Close();
            
            string deleteQuery = "DELETE FROM Employees WHERE DATEDIFF(YEAR, BirthDate, GETDATE()) > 70";
            using var deleteCommand = new SqlCommand(deleteQuery, connection);
            int deletedCount = await deleteCommand.ExecuteNonQueryAsync();
            
            Console.WriteLine($"Successfully deleted {deletedCount} employees older than 70 years");
        }
        else
        {
            Console.WriteLine("No employees older than 70 years found");
        }
    }
}
