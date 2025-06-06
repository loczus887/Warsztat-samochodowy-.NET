using Microsoft.EntityFrameworkCore;
using WorkshopManager.Data;
using WorkshopManager.Models;
using WorkshopManager.Services.Interfaces;

namespace WorkshopManager.Services;

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _context;

    public CustomerService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Customer>> GetAllCustomersAsync()
    {
        return await _context.Customers
            .Include(c => c.Vehicles)  // DODANE - załaduje pojazdy!
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToListAsync();
    }

    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        return await _context.Customers
            .Include(c => c.Vehicles)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Customer>> SearchCustomersAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllCustomersAsync();

        return await _context.Customers
            .Include(c => c.Vehicles)  // DODANE - załaduje pojazdy też w wyszukiwaniu!
            .Where(c => c.FirstName.Contains(searchTerm) ||
                   c.LastName.Contains(searchTerm) ||
                   c.PhoneNumber.Contains(searchTerm) ||
                   c.Email.Contains(searchTerm))
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToListAsync();
    }

    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task UpdateCustomerAsync(Customer customer)
    {
        _context.Entry(customer).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCustomerAsync(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer != null)
        {
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Vehicle>> GetCustomerVehiclesAsync(int customerId)
    {
        return await _context.Vehicles
            .Where(v => v.CustomerId == customerId)
            .OrderByDescending(v => v.Year)
            .ToListAsync();
    }
}