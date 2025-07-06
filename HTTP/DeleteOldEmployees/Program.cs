using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;

namespace DeleteOldEmployees;

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
            Console.WriteLine("Deleting old employees via HTTP API...");
            
            await DeleteOldEmployeesAsync();
            
            Console.WriteLine("Delete old employees operation completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task DeleteOldEmployeesAsync()
    {
        var allEmployees = new List<GetEmployeeDTO>();
        int page = 1;
        int limit = 50;
        
        while (true)
        {
            var response = await httpClient.GetAsync($"{baseUrl}/Employees?Page={page}&Limit={limit}");
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
        
        var currentDate = DateTime.Now;
        var oldEmployees = allEmployees
            .Where(e => (currentDate - e.BirthDate).Days / 365.25 > 70)
            .OrderBy(e => e.BirthDate)
            .ToList();
        
        Console.WriteLine("Employees older than 70 years:");
        Console.WriteLine("Name\t\t\t\tDepartment\t\tAge\tBirth Date");
        Console.WriteLine(new string('-', 80));
        
        foreach (var employee in oldEmployees)
        {
            string name = $"{employee.FirstName} {employee.MiddleName} {employee.LastName}";
            string department = employee.Department.Name;
            int age = (int)((currentDate - employee.BirthDate).Days / 365.25);
            DateTime birthDate = employee.BirthDate;
            
            Console.WriteLine($"{name.PadRight(30)}\t{department.PadRight(15)}\t{age}\t{birthDate:yyyy-MM-dd}");
        }
        
        if (oldEmployees.Count > 0)
        {
            Console.WriteLine($"\nFound {oldEmployees.Count} employees older than 70 years");
            Console.WriteLine("Deleting these employees...");
            
            int deletedCount = 0;
            
            foreach (var employee in oldEmployees)
            {
                try
                {
                    await DeleteEmployeeAsync(employee.Id);
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete employee {employee.FirstName} {employee.LastName}: {ex.Message}");
                }
            }
            
            Console.WriteLine($"Successfully deleted {deletedCount} employees older than 70 years");
        }
        else
        {
            Console.WriteLine("No employees older than 70 years found");
        }
    }

    private static async Task DeleteEmployeeAsync(Guid employeeId)
    {
        var response = await httpClient.DeleteAsync($"{baseUrl}/Employees/{employeeId}");
        response.EnsureSuccessStatusCode();
    }
} 