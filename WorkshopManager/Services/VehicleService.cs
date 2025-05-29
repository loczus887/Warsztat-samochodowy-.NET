using Microsoft.EntityFrameworkCore;
using WorkshopManager.Data;
using WorkshopManager.Models;
using WorkshopManager.Services.Interfaces;

namespace WorkshopManager.Services;

public class VehicleService : IVehicleService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public VehicleService(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<List<Vehicle>> GetAllVehiclesAsync()
    {
        return await _context.Vehicles
            .Include(v => v.Customer)
            .OrderBy(v => v.Make)
            .ThenBy(v => v.Model)
            .ToListAsync();
    }

    public async Task<Vehicle?> GetVehicleByIdAsync(int id)
    {
        return await _context.Vehicles
            .Include(v => v.Customer)
            .Include(v => v.ServiceOrders)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<Vehicle> CreateVehicleAsync(Vehicle vehicle)
    {
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();
        return vehicle;
    }

    public async Task UpdateVehicleAsync(Vehicle vehicle)
    {
        _context.Entry(vehicle).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteVehicleAsync(int id)
    {
        var vehicle = await _context.Vehicles.FindAsync(id);
        if (vehicle != null)
        {
            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<string> SaveVehicleImageAsync(IFormFile imageFile)
    {
        // Sprawdzenie, czy katalog uploads istnieje
        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        // Generowanie unikalnej nazwy pliku
        var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        // Zapisanie pliku
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await imageFile.CopyToAsync(fileStream);
        }

        return "/uploads/" + uniqueFileName;
    }
}