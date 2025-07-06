using Microsoft.Data.SqlClient;
using Bogus;

namespace DatabaseSeeding;

public class Department
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Employee
{
    public Guid Id { get; set; }
    public Guid DepartmentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public DateTime HireDate { get; set; }
    public decimal Salary { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            var connectionString = GetConnectionString();
            Console.WriteLine("Connection string configured successfully from environment variables!");
            
            var departments = GenerateDepartments();
            var employees = GenerateEmployees(departments);
            
            await SeedDatabaseAsync(connectionString, departments, employees);
            
            Console.WriteLine("Database seeding completed successfully!");
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

    private static List<Department> GenerateDepartments()
    {
        var currentTime = DateTime.UtcNow;
        var departments = new List<Department>
        {
            new Department { Id = Guid.NewGuid(), Name = "HR", CreatedAt = currentTime, UpdatedAt = currentTime },
            new Department { Id = Guid.NewGuid(), Name = "IT", CreatedAt = currentTime, UpdatedAt = currentTime },
            new Department { Id = Guid.NewGuid(), Name = "Finance", CreatedAt = currentTime, UpdatedAt = currentTime },
            new Department { Id = Guid.NewGuid(), Name = "Marketing", CreatedAt = currentTime, UpdatedAt = currentTime },
            new Department { Id = Guid.NewGuid(), Name = "Sales", CreatedAt = currentTime, UpdatedAt = currentTime },
            new Department { Id = Guid.NewGuid(), Name = "Ops", CreatedAt = currentTime, UpdatedAt = currentTime },
            new Department { Id = Guid.NewGuid(), Name = "R&D", CreatedAt = currentTime, UpdatedAt = currentTime },
            new Department { Id = Guid.NewGuid(), Name = "CS", CreatedAt = currentTime, UpdatedAt = currentTime },
            new Department { Id = Guid.NewGuid(), Name = "Legal", CreatedAt = currentTime, UpdatedAt = currentTime },
            new Department { Id = Guid.NewGuid(), Name = "QA", CreatedAt = currentTime, UpdatedAt = currentTime }
        };

        Console.WriteLine($"Generated {departments.Count} departments:");
        foreach (var dept in departments)
        {
            Console.WriteLine($"- {dept.Name} (ID: {dept.Id})");
        }

        return departments;
    }

    private static List<Employee> GenerateEmployees(List<Department> departments)
    {
        var currentTime = DateTime.UtcNow;
        var faker = new Faker<Employee>()
            .RuleFor(e => e.Id, f => Guid.NewGuid())
            .RuleFor(e => e.DepartmentId, f => f.PickRandom(departments).Id)
            .RuleFor(e => e.FirstName, f => f.Person.FirstName)
            .RuleFor(e => e.MiddleName, f => f.Random.Bool(0.7f) ? f.Person.FirstName : null)
            .RuleFor(e => e.LastName, f => f.Person.LastName)
            .RuleFor(e => e.BirthDate, f => f.Date.Between(DateTime.Now.AddYears(-65), DateTime.Now.AddYears(-18)))
            .RuleFor(e => e.HireDate, f => f.Date.Between(DateTime.Now.AddYears(-20), DateTime.Now.AddMonths(-1)))
            .RuleFor(e => e.Salary, f => f.Random.Decimal(30000, 150000))
            .RuleFor(e => e.CreatedAt, f => currentTime)
            .RuleFor(e => e.UpdatedAt, f => currentTime);

        var employees = faker.Generate(100);

        Console.WriteLine($"Generated {employees.Count} employees");
        Console.WriteLine("Sample employees:");
        for (int i = 0; i < Math.Min(5, employees.Count); i++)
        {
            var emp = employees[i];
            var dept = departments.Find(d => d.Id == emp.DepartmentId);
            Console.WriteLine($"- {emp.FirstName} {emp.LastName} - {dept?.Name} - ${emp.Salary:N2}");
        }

        return employees;
    }

    private static async Task SeedDatabaseAsync(string connectionString, List<Department> departments, List<Employee> employees)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        // Clear existing data
        using var clearCommand = new SqlCommand("DELETE FROM Employees; DELETE FROM Departments;", connection);
        await clearCommand.ExecuteNonQueryAsync();
        
        // Seed departments
        await SeedDepartmentsAsync(connection, departments);
        
        // Seed employees
        await SeedEmployeesAsync(connection, employees);
    }

    private static async Task SeedDepartmentsAsync(SqlConnection connection, List<Department> departments)
    {
        string insertDepartmentQuery = "INSERT INTO Departments (Id, Name, CreatedAt, UpdatedAt) VALUES (@Id, @Name, @CreatedAt, @UpdatedAt)";
        
        foreach (var department in departments)
        {
            using var command = new SqlCommand(insertDepartmentQuery, connection);
            command.Parameters.AddWithValue("@Id", department.Id);
            command.Parameters.AddWithValue("@Name", department.Name);
            command.Parameters.AddWithValue("@CreatedAt", department.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", department.UpdatedAt);
            await command.ExecuteNonQueryAsync();
        }
        
        Console.WriteLine($"Successfully seeded {departments.Count} departments!");
    }

    private static async Task SeedEmployeesAsync(SqlConnection connection, List<Employee> employees)
    {
        string insertEmployeeQuery = @"
            INSERT INTO Employees (Id, DepartmentId, FirstName, MiddleName, LastName, BirthDate, HireDate, Salary, CreatedAt, UpdatedAt) 
            VALUES (@Id, @DepartmentId, @FirstName, @MiddleName, @LastName, @BirthDate, @HireDate, @Salary, @CreatedAt, @UpdatedAt)
        ";
        
        int insertedCount = 0;
        
        foreach (var employee in employees)
        {
            using var command = new SqlCommand(insertEmployeeQuery, connection);
            command.Parameters.AddWithValue("@Id", employee.Id);
            command.Parameters.AddWithValue("@DepartmentId", employee.DepartmentId);
            command.Parameters.AddWithValue("@FirstName", employee.FirstName);
            command.Parameters.AddWithValue("@MiddleName", employee.MiddleName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@LastName", employee.LastName);
            command.Parameters.AddWithValue("@BirthDate", employee.BirthDate);
            command.Parameters.AddWithValue("@HireDate", employee.HireDate);
            command.Parameters.AddWithValue("@Salary", employee.Salary);
            command.Parameters.AddWithValue("@CreatedAt", employee.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", employee.UpdatedAt);
            
            await command.ExecuteNonQueryAsync();
            insertedCount++;
            
            if (insertedCount % 10 == 0)
            {
                Console.WriteLine($"Inserted {insertedCount} employees...");
            }
        }
        
        Console.WriteLine($"Successfully seeded {insertedCount} employees!");
    }
}
