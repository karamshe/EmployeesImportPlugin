using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using PhoneApp.Domain;
using PhoneApp.Domain.Attributes;
using PhoneApp.Domain.DTO;
using PhoneApp.Domain.Interfaces;
using Newtonsoft.Json;

namespace EmployeesImportPlugin
{
    [Author(Name = "Polina Karamsyheva")]
    public class Plugin : IPluggable
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string ApiUrl = "https://dummyjson.com/users";

        public IEnumerable<DataTransferObject> Run(IEnumerable<DataTransferObject> args)
        {
            var employees = args != null ? args.Cast<EmployeesDTO>().ToList() : new List<EmployeesDTO>();

            try
            {
                Console.WriteLine("Starting loading users from API...");

                var apiUsers = FetchUsersFromApiAsync().Result;

                foreach (var user in apiUsers)
                {
                    string fullName = $"{user.FirstName} {user.LastName}";
                    AddEmployee(employees, fullName, user.Phone);
                }

            }
            catch (AggregateException ex)
            {
                Console.WriteLine($"Error loading data: {ex.InnerException?.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }

            return employees;
        }

        private async Task<List<ApiUser>> FetchUsersFromApiAsync()
        {
            var response = await _httpClient.GetAsync(ApiUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(json);

            return apiResponse?.Users ?? throw new Exception("User data not found in API response");
        }

        private void AddEmployee(List<EmployeesDTO> employees, string name, string phone)
        {
            if (!employees.Any(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                var newEmployee = new EmployeesDTO { Name = name };
                if (!string.IsNullOrWhiteSpace(phone))
                {
                    newEmployee.AddPhone(phone);
                }
                employees.Add(newEmployee);
                Console.WriteLine($"Add: {name} | Phone: {phone}");
            }
        }
    }

    public class ApiResponse
    {
        public List<ApiUser> Users { get; set; }
    }

    public class ApiUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
    }
}