using WorkshopManager.DTOs;

namespace WorkshopManager.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetAdminDashboardAsync();
    Task<DashboardDto> GetMechanicDashboardAsync(string mechanicId);
    Task<DashboardDto> GetReceptionistDashboardAsync();
}