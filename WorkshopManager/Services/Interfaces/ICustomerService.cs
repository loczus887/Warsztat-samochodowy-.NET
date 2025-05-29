using WorkshopManager.Models;

namespace WorkshopManager.Services.Interfaces;

public interface ICustomerService
{
    Task<List<Customer>> GetAllCustomersAsync();
    Task<Customer?> GetCustomerByIdAsync(int id);
    Task<List<Customer>> SearchCustomersAsync(string searchTerm);
    Task<Customer> CreateCustomerAsync(Customer customer);
    Task UpdateCustomerAsync(Customer customer);
    Task DeleteCustomerAsync(int id);
    Task<List<Vehicle>> GetCustomerVehiclesAsync(int customerId);
}