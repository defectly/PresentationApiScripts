using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using Bogus;
using System.Text.Json;

namespace DatabaseSeeding;

public class CreateDepartmentCommand
{
    public string Name { get; set; } = string.Empty;
}

public class CreateEmployeeCommand
{
    public Guid DepartmentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public DateTime HireDate { get; set; }
    public decimal Salary { get; set; }
}

public class GetDepartmentDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class Program
{
    private static readonly HttpClient httpClient = new();
    private static readonly string baseUrl = "http://93.127.223.38:3000";

    public static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("Starting database seeding via HTTP API...");
            
            await ClearExistingDataAsync();
            
            var departmentIds = await SeedDepartmentsAsync();
            await SeedEmployeesAsync(departmentIds);
            
            Console.WriteLine("Database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task ClearExistingDataAsync()
    {
        Console.WriteLine("Clearing existing data...");
        
        var employees = await GetAllEmployeesAsync();
        foreach (var employee in employees)
        {
            await DeleteEmployeeAsync(employee.Id);
        }
        
        var departments = await GetAllDepartmentsAsync();
        foreach (var department in departments)
        {
            await DeleteDepartmentAsync(department.Id);
        }
        
        Console.WriteLine("Existing data cleared successfully!");
    }

    private static async Task<List<Guid>> SeedDepartmentsAsync()
    {
        Console.WriteLine("Seeding departments...");
        
        var departmentNames = new[]
        {
            "HR", "IT", "Finance", "Marketing", "Sales", 
            "Ops", "RnD", "CS", "Legal", "QA"
        };
        
        var departmentIds = new List<Guid>();
        
        foreach (var name in departmentNames)
        {
            var command = new CreateDepartmentCommand { Name = name };
            var departmentId = await CreateDepartmentAsync(command);
            departmentIds.Add(departmentId);
            Console.WriteLine($"Created department: {name} (ID: {departmentId})");
        }
        
        Console.WriteLine($"Successfully seeded {departmentIds.Count} departments!");
        return departmentIds;
    }

    private static async Task SeedEmployeesAsync(List<Guid> departmentIds)
    {
        Console.WriteLine("Seeding employees...");
        
        var faker = new Faker<CreateEmployeeCommand>()
            .RuleFor(e => e.DepartmentId, f => f.PickRandom(departmentIds))
            .RuleFor(e => e.FirstName, f => f.Person.FirstName)
            .RuleFor(e => e.MiddleName, f => f.Random.Bool(0.7f) ? f.Person.FirstName : null)
            .RuleFor(e => e.LastName, f => f.Person.LastName)
            .RuleFor(e => e.BirthDate, f => f.Date.Between(DateTime.Now.AddYears(-90), DateTime.Now.AddYears(-18)))
            .RuleFor(e => e.HireDate, f => f.Date.Between(DateTime.Now.AddYears(-20), DateTime.Now.AddMonths(-1)))
            .RuleFor(e => e.Salary, f => f.Random.Decimal(5000, 25000));

        var employees = faker.Generate(100);
        int createdCount = 0;

        foreach (var employee in employees)
        {
            try
            {
                var employeeId = await CreateEmployeeAsync(employee);
                createdCount++;
                
                if (createdCount <= 5)
                {
                    Console.WriteLine($"Created employee: {employee.FirstName} {employee.LastName} - ${employee.Salary:N2}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create employee {employee.FirstName} {employee.LastName}: {ex.Message}");
            }
        }
        
        Console.WriteLine($"Successfully seeded {createdCount} employees!");
    }

    private static async Task<Guid> CreateDepartmentAsync(CreateDepartmentCommand command)
    {
        var json = JsonSerializer.Serialize(command);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await httpClient.PostAsync($"{baseUrl}/Departments", content);
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Guid>(responseContent);
    }

    private static async Task<Guid> CreateEmployeeAsync(CreateEmployeeCommand command)
    {
        var json = JsonSerializer.Serialize(command);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await httpClient.PostAsync($"{baseUrl}/Employees", content);
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Guid>(responseContent);
    }

    private static async Task<List<GetDepartmentDTO>> GetAllDepartmentsAsync()
    {
        var response = await httpClient.GetAsync($"{baseUrl}/Departments");
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<GetDepartmentDTO>>(json) ?? new List<GetDepartmentDTO>();
    }

    private static async Task<List<GetEmployeeDTO>> GetAllEmployeesAsync()
    {
        var response = await httpClient.GetAsync($"{baseUrl}/Employees?Limit=1000");
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        var paginationResult = JsonSerializer.Deserialize<PaginationResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return paginationResult?.Data ?? new List<GetEmployeeDTO>();
    }

    private static async Task DeleteDepartmentAsync(Guid departmentId)
    {
        await httpClient.DeleteAsync($"{baseUrl}/Departments/{departmentId}");
    }

    private static async Task DeleteEmployeeAsync(Guid employeeId)
    {
        await httpClient.DeleteAsync($"{baseUrl}/Employees/{employeeId}");
    }

    private class PaginationResult
    {
        public List<GetEmployeeDTO> Data { get; set; } = new();
    }

    private class GetEmployeeDTO
    {
        public Guid Id { get; set; }
    }
} 