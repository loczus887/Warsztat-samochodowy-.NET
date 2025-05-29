using WorkshopManager.Models;

namespace WorkshopManager.Services.Interfaces;

public interface IPartService
{
    Task<List<Part>> GetAllPartsAsync();
    Task<Part?> GetPartByIdAsync(int id);
    Task<List<Part>> SearchPartsByNameAsync(string searchTerm);
    Task<Part> CreatePartAsync(Part part);
    Task UpdatePartAsync(Part part);
    Task DeletePartAsync(int id);
}