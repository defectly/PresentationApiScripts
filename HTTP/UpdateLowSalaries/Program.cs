using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;

namespace UpdateLowSalaries;

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

public class UpdateEmployeeDTO
{
    public Guid? DepartmentId { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public DateTime? BirthDate { get; set; }
    public DateTime? HireDate { get; set; }
    public decimal? Salary { get; set; }
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
            Console.WriteLine("Updating low salary employees via HTTP API...");
            
            await UpdateLowSalariesAsync();
            
            Console.WriteLine("Update low salaries operation completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task UpdateLowSalariesAsync()
    {
        var lowSalaryEmployees = new List<GetEmployeeDTO>();
        int page = 1;
        int limit = 50;
        
        while (true)
        {
            var response = await httpClient.GetAsync($"{baseUrl}/Employees?Page={page}&Limit={limit}&MaxSalary=14999");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var paginationResult = JsonSerializer.Deserialize<PaginationResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (paginationResult == null || paginationResult.Data.Count == 0)
                break;
                
            lowSalaryEmployees.AddRange(paginationResult.Data);
            
            if (page >= paginationResult.TotalPages)
                break;
                
            page++;
        }
        
        var sortedEmployees = lowSalaryEmployees.OrderBy(e => e.Salary).ToList();
        
        Console.WriteLine("Employees with salary < 15,000:");
        Console.WriteLine("Name\t\t\t\tDepartment\t\tCurrent Salary");
        Console.WriteLine(new string('-', 80));
        
        foreach (var employee in sortedEmployees)
        {
            string name = $"{employee.FirstName} {employee.MiddleName} {employee.LastName}";
            string department = employee.Department.Name;
            decimal salary = employee.Salary;
            
            Console.WriteLine($"{name.PadRight(30)}\t{department.PadRight(15)}\t${salary:N0}");
        }
        
        if (sortedEmployees.Count > 0)
        {
            Console.WriteLine($"\nFound {sortedEmployees.Count} employees with salary < 15,000");
            Console.WriteLine("Updating their salaries to 15,000...");
            
            int updatedCount = 0;
            
            foreach (var employee in sortedEmployees)
            {
                try
                {
                    var updateDto = new UpdateEmployeeDTO
                    {
                        Salary = 15000
                    };
                    
                    await UpdateEmployeeAsync(employee.Id, updateDto);
                    updatedCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to update employee {employee.FirstName} {employee.LastName}: {ex.Message}");
                }
            }
            
            Console.WriteLine($"Successfully updated {updatedCount} employees to 15,000 salary");
            
            Console.WriteLine("\nVerifying updated employees...");
            await VerifyUpdatedEmployeesAsync();
        }
        else
        {
            Console.WriteLine("No employees with salary < 15,000 found");
        }
    }

    private static async Task UpdateEmployeeAsync(Guid employeeId, UpdateEmployeeDTO updateDto)
    {
        var json = JsonSerializer.Serialize(updateDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await httpClient.PutAsync($"{baseUrl}/Employees/{employeeId}", content);
        response.EnsureSuccessStatusCode();
    }

    private static async Task VerifyUpdatedEmployeesAsync()
    {
        var updatedEmployees = new List<GetEmployeeDTO>();
        int page = 1;
        int limit = 50;
        
        while (true)
        {
            var response = await httpClient.GetAsync($"{baseUrl}/Employees?Page={page}&Limit={limit}&Salary=15000");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var paginationResult = JsonSerializer.Deserialize<PaginationResult>(json);
            
            if (paginationResult == null || paginationResult.Data.Count == 0)
                break;
                
            updatedEmployees.AddRange(paginationResult.Data);
            
            if (page >= paginationResult.TotalPages)
                break;
                
            page++;
        }
        
        Console.WriteLine("Employees with updated salary (15,000):");
        foreach (var employee in updatedEmployees.OrderBy(e => e.LastName))
        {
            string name = $"{employee.FirstName} {employee.MiddleName} {employee.LastName}";
            string department = employee.Department.Name;
            decimal salary = employee.Salary;
            
            Console.WriteLine($"{name.PadRight(30)}\t{department.PadRight(15)}\t${salary:N0}");
        }
    }
} 