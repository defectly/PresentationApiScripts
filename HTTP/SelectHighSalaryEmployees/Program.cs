using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SelectHighSalaryEmployees;

public class GetEmployeeDTO
{
    public Guid Id { get; set; }
    public GetEmployeeDepartmentDTO Department { get; set; } = new();
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public DateTime HireDate { get; set; }
    public decimal Salary { get; set; }
}

public class GetEmployeeDepartmentDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class PaginationResult
{
    public List<GetEmployeeDTO> Data { get; set; } = new();
    public int CurrentPage { get; set; }
    public int PerPage { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}

public class Program
{
    private static readonly HttpClient httpClient = new();
    private static readonly string baseUrl = "http://93.127.223.38:3000";

    public static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("Retrieving high salary employees via HTTP API...");
            
            await SelectHighSalaryEmployeesAsync();
            
            Console.WriteLine("Select high salary employees operation completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task SelectHighSalaryEmployeesAsync()
    {
        var allEmployees = new List<GetEmployeeDTO>();
        int page = 1;
        int limit = 50;
        
        while (true)
        {
            var response = await httpClient.GetAsync($"{baseUrl}/Employees?Page={page}&Limit={limit}&MinSalary=10000");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var paginationResult = JsonSerializer.Deserialize<PaginationResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (paginationResult == null || paginationResult.Data.Count == 0)
                break;
                
            allEmployees.AddRange(paginationResult.Data);
            
            if (page >= paginationResult.TotalPages)
                break;
                
            page++;
        }
        
        var sortedEmployees = allEmployees.OrderByDescending(e => e.Salary).ToList();

        Console.WriteLine(
            $"{"Name".PadRight(32)}" +
            $"{"Department".PadRight(18)}" +
            $"{"Salary".PadLeft(12)}" +
            $"{"Birth Date".PadLeft(15)}" +
            $"{"Hire Date".PadLeft(15)}"
        );
        Console.WriteLine(new string('-', 92));

        foreach (var employee in sortedEmployees)
        {
            string name = $"{employee.FirstName} {employee.MiddleName} {employee.LastName}";
            string department = employee.Department.Name;
            decimal salary = employee.Salary;
            DateTime birthDate = employee.BirthDate;
            DateTime hireDate = employee.HireDate;

            Console.WriteLine(
                $"{name.PadRight(32)}" +
                $"{department.PadRight(18)}" +
                $"{salary.ToString().PadLeft(12)}" +
                $"{birthDate.ToString("yyyy-MM-dd").PadLeft(15)}" +
                $"{hireDate.ToString("yyyy-MM-dd").PadLeft(15)}"
            );
        }
        
        Console.WriteLine($"\nTotal: {sortedEmployees.Count} employees with salary > $10,000");
    }
} 