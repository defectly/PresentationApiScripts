using Microsoft.Data.SqlClient;

namespace SelectHighSalaryEmployees;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            var connectionString = GetConnectionString();
            Console.WriteLine("Connection string configured successfully from environment variables!");
            
            await SelectHighSalaryEmployeesAsync(connectionString);
            
            Console.WriteLine("Select high salary employees operation completed successfully!");
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

    private static async Task SelectHighSalaryEmployeesAsync(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        string query = @"
            SELECT 
                e.Id,
                e.FirstName,
                e.MiddleName,
                e.LastName,
                e.BirthDate,
                e.HireDate,
                e.Salary,
                d.Name as DepartmentName
            FROM Employees e
            INNER JOIN Departments d ON e.DepartmentId = d.Id
            WHERE e.Salary > 100000
            ORDER BY e.Salary DESC
        ";
        
        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        Console.WriteLine("Employees with Salary > $100,000:");
        Console.WriteLine("Name\t\t\t\tDepartment\t\tSalary\t\tBirth Date\tHire Date");
        Console.WriteLine(new string('-', 100));
        
        int count = 0;
        while (await reader.ReadAsync())
        {
            string name = $"{reader["FirstName"]} {reader["MiddleName"]} {reader["LastName"]}";
            string department = reader["DepartmentName"].ToString() ?? string.Empty;
            decimal salary = (decimal)reader["Salary"];
            DateTime birthDate = (DateTime)reader["BirthDate"];
            DateTime hireDate = (DateTime)reader["HireDate"];
            
            Console.WriteLine($"{name.PadRight(30)}\t{department.PadRight(15)}\t${salary:N0}\t\t{birthDate:yyyy-MM-dd}\t{hireDate:yyyy-MM-dd}");
            count++;
        }
        
        Console.WriteLine($"\nTotal: {count} employees with salary > $100,000");
    }
}
