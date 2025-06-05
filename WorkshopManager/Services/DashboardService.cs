using Microsoft.EntityFrameworkCore;
using WorkshopManager.Data;
using WorkshopManager.DTOs;
using WorkshopManager.Models;
using WorkshopManager.Services.Interfaces;

namespace WorkshopManager.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(ApplicationDbContext context, ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardDto> GetAdminDashboardAsync()
    {
        try
        {
            var dashboard = new DashboardDto();

            // Podstawowe statystyki
            dashboard.TotalOrdersCount = await _context.ServiceOrders.CountAsync();
            dashboard.ActiveOrdersCount = await _context.ServiceOrders
                .Where(o => o.Status == OrderStatus.New || o.Status == OrderStatus.InProgress)
                .CountAsync();
            dashboard.CompletedOrdersCount = await _context.ServiceOrders
                .Where(o => o.Status == OrderStatus.Completed)
                .CountAsync();
            dashboard.CustomersCount = await _context.Customers.CountAsync();
            dashboard.VehiclesCount = await _context.Vehicles.CountAsync();

            // Statystyki finansowe
            var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dashboard.TotalRevenueThisMonth = await CalculateRevenueAsync(firstDayOfMonth, DateTime.Now);

            return dashboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin dashboard data");
            throw;
        }
    }

    public async Task<DashboardDto> GetMechanicDashboardAsync(string mechanicId)
    {
        try
        {
            var dashboard = new DashboardDto();

            // Zlecenia przypisane do mechanika
            dashboard.AssignedOrdersCount = await _context.ServiceOrders
                .Where(o => o.MechanicId == mechanicId)
                .CountAsync();

            dashboard.ActiveOrdersCount = await _context.ServiceOrders
                .Where(o => o.MechanicId == mechanicId &&
                       (o.Status == OrderStatus.New || o.Status == OrderStatus.InProgress))
                .CountAsync();

            dashboard.CompletedOrdersCount = await _context.ServiceOrders
                .Where(o => o.MechanicId == mechanicId && o.Status == OrderStatus.Completed)
                .CountAsync();

            // Zlecenia ukończone w tym miesiącu
            var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dashboard.CompletedOrdersThisMonth = await _context.ServiceOrders
                .Where(o => o.MechanicId == mechanicId &&
                       o.Status == OrderStatus.Completed &&
                       o.CompletedAt >= firstDayOfMonth)
                .CountAsync();

            return dashboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mechanic dashboard data for mechanic {MechanicId}", mechanicId);
            throw;
        }
    }

    public async Task<DashboardDto> GetReceptionistDashboardAsync()
    {
        try
        {
            var dashboard = new DashboardDto();

            // Podobne do admin, ale bez niektórych uprawnień
            dashboard.TotalOrdersCount = await _context.ServiceOrders.CountAsync();
            dashboard.ActiveOrdersCount = await _context.ServiceOrders
                .Where(o => o.Status == OrderStatus.New || o.Status == OrderStatus.InProgress)
                .CountAsync();
            dashboard.CompletedOrdersCount = await _context.ServiceOrders
                .Where(o => o.Status == OrderStatus.Completed)
                .CountAsync();
            dashboard.CustomersCount = await _context.Customers.CountAsync();
            dashboard.VehiclesCount = await _context.Vehicles.CountAsync();

            return dashboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting receptionist dashboard data");
            throw;
        }
    }

    private async Task<decimal> CalculateRevenueAsync(DateTime startDate, DateTime endDate)
    {
        var orders = await _context.ServiceOrders
            .Include(o => o.Tasks)
                .ThenInclude(t => t.UsedParts)
                    .ThenInclude(up => up.Part)
            .Where(o => o.Status == OrderStatus.Completed &&
                   o.CompletedAt >= startDate &&
                   o.CompletedAt <= endDate)
            .ToListAsync();

        decimal totalRevenue = 0;

        foreach (var order in orders)
        {
            foreach (var task in order.Tasks)
            {
                totalRevenue += task.LaborCost;

                foreach (var usedPart in task.UsedParts)
                {
                    totalRevenue += usedPart.Quantity * usedPart.Part.UnitPrice;
                }
            }
        }

        return totalRevenue;
    }
}