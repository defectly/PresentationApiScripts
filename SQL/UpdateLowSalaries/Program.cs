using Microsoft.Data.SqlClient;

namespace UpdateLowSalaries;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            var connectionString = GetConnectionString();
            Console.WriteLine("Connection string configured successfully from environment variables!");
            
            await UpdateLowSalariesAsync(connectionString);
            
            Console.WriteLine("Update low salaries operation completed successfully!");
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

    private static async Task UpdateLowSalariesAsync(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        string selectQuery = @"
            SELECT 
                e.Id,
                e.FirstName,
                e.MiddleName,
                e.LastName,
                e.Salary,
                d.Name as DepartmentName
            FROM Employees e
            INNER JOIN Departments d ON e.DepartmentId = d.Id
            WHERE e.Salary < 15000
            ORDER BY e.Salary
        ";
        
        using var selectCommand = new SqlCommand(selectQuery, connection);
        using var reader = await selectCommand.ExecuteReaderAsync();
        
        Console.WriteLine("Employees with salary < 15000:");
        Console.WriteLine("Name\t\t\t\tDepartment\t\tCurrent Salary");
        Console.WriteLine(new string('-', 80));
        
        int count = 0;
        while (await reader.ReadAsync())
        {
            string name = $"{reader["FirstName"]} {reader["MiddleName"]} {reader["LastName"]}";
            string department = reader["DepartmentName"].ToString() ?? string.Empty;
            decimal salary = (decimal)reader["Salary"];
            
            Console.WriteLine($"{name.PadRight(30)}\t{department.PadRight(15)}\t${salary:N0}");
            count++;
        }
        
        if (count > 0)
        {
            Console.WriteLine($"\nFound {count} employees with salary < 15000");
            Console.WriteLine("Updating their salaries to 15000...");
            
            reader.Close();
            
            string updateQuery = @"
                UPDATE Employees 
                SET Salary = 15000, 
                    UpdatedAt = GETDATE()
                WHERE Salary < 15000
            ";
            
            using var updateCommand = new SqlCommand(updateQuery, connection);
            int updatedCount = await updateCommand.ExecuteNonQueryAsync();
            
            Console.WriteLine($"Successfully updated {updatedCount} employees to 15000 salary");
            
            string verifyQuery = @"
                SELECT 
                    e.FirstName,
                    e.MiddleName,
                    e.LastName,
                    e.Salary,
                    d.Name as DepartmentName
                FROM Employees e
                INNER JOIN Departments d ON e.DepartmentId = d.Id
                WHERE e.Salary = 15000
                ORDER BY e.LastName
            ";
            
            using var verifyCommand = new SqlCommand(verifyQuery, connection);
            using var verifyReader = await verifyCommand.ExecuteReaderAsync();
            
            Console.WriteLine("\nEmployees with updated salary (15000):");
            while (await verifyReader.ReadAsync())
            {
                string name = $"{verifyReader["FirstName"]} {verifyReader["MiddleName"]} {verifyReader["LastName"]}";
                string department = verifyReader["DepartmentName"].ToString() ?? string.Empty;
                decimal salary = (decimal)verifyReader["Salary"];
                
                Console.WriteLine($"{name.PadRight(30)}\t{department.PadRight(15)}\t{salary:N0}");
            }
        }
        else
        {
            Console.WriteLine("No employees with salary < 15000 found");
        }
    }
}
