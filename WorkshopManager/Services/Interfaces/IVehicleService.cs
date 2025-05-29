using WorkshopManager.Models;

namespace WorkshopManager.Services.Interfaces;

public interface IVehicleService
{
    Task<List<Vehicle>> GetAllVehiclesAsync();
    Task<Vehicle?> GetVehicleByIdAsync(int id);
    Task<Vehicle> CreateVehicleAsync(Vehicle vehicle);
    Task UpdateVehicleAsync(Vehicle vehicle);
    Task DeleteVehicleAsync(int id);
    Task<string> SaveVehicleImageAsync(IFormFile imageFile);
}