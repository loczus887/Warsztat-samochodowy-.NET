using Microsoft.EntityFrameworkCore;
using WorkshopManager.Data;
using WorkshopManager.Models;
using WorkshopManager.Services.Interfaces;

namespace WorkshopManager.Services;

public class PartService : IPartService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PartService> _logger;

    public PartService(ApplicationDbContext context, ILogger<PartService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Part>> GetAllPartsAsync()
    {
        try
        {
            return await _context.Parts
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all parts");
            throw;
        }
    }

    public async Task<Part?> GetPartByIdAsync(int id)
    {
        try
        {
            return await _context.Parts
                .Include(p => p.UsedParts)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting part with ID: {PartId}", id);
            throw;
        }
    }

    public async Task<List<Part>> SearchPartsByNameAsync(string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllPartsAsync();

            return await _context.Parts
                .Where(p => p.Name.Contains(searchTerm) ||
                       (p.Category != null && p.Category.Contains(searchTerm)))
                .OrderBy(p => p.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching parts with term: {SearchTerm}", searchTerm);
            throw;
        }
    }

    public async Task<Part> CreatePartAsync(Part part)
    {
        try
        {
            _context.Parts.Add(part);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created part with ID: {PartId}", part.Id);
            return part;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating part: {PartName}", part.Name);
            throw;
        }
    }

    public async Task UpdatePartAsync(Part part)
    {
        try
        {
            _context.Entry(part).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated part with ID: {PartId}", part.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating part with ID: {PartId}", part.Id);
            throw;
        }
    }

    public async Task DeletePartAsync(int id)
    {
        try
        {
            var part = await _context.Parts.FindAsync(id);
            if (part != null)
            {
                _context.Parts.Remove(part);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted part with ID: {PartId}", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting part with ID: {PartId}", id);
            throw;
        }
    }
}