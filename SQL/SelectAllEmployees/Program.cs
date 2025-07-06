using Microsoft.Data.SqlClient;

namespace SelectAllEmployees;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            var connectionString = GetConnectionString();
            Console.WriteLine("Connection string configured successfully from environment variables!");
            
            await SelectAllEmployeesAsync(connectionString);
            
            Console.WriteLine("Select all employees operation completed successfully!");
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

    private static async Task SelectAllEmployeesAsync(string connectionString)
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
            ORDER BY e.LastName, e.FirstName
        ";
        
        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        Console.WriteLine(
            $"{"Name".PadRight(32)}" +
            $"{"Department".PadRight(18)}" +
            $"{"Salary".PadLeft(12)}" +
            $"{"Birth Date".PadLeft(15)}" +
            $"{"Hire Date".PadLeft(15)}"
        );
        Console.WriteLine(new string('-', 92));


        int count = 0;
        while (await reader.ReadAsync())
        {
            string name = $"{reader["FirstName"]} {reader["MiddleName"]} {reader["LastName"]}";
            string department = reader["DepartmentName"].ToString() ?? string.Empty;
            decimal salary = (decimal)reader["Salary"];
            DateTime birthDate = (DateTime)reader["BirthDate"];
            DateTime hireDate = (DateTime)reader["HireDate"];

            Console.WriteLine(
                $"{name.PadRight(32)}" +
                $"{department.PadRight(18)}" +
                $"{salary.ToString("C0").PadLeft(12)}" +
                $"{birthDate.ToString("yyyy-MM-dd").PadLeft(15)}" +
                $"{hireDate.ToString("yyyy-MM-dd").PadLeft(15)}"
            );

            count++;
        }
        
        Console.WriteLine($"\nTotal: {count} employees");
    }
}
