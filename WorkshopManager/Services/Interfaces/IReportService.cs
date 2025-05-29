using WorkshopManager.Models;

namespace WorkshopManager.Services.Interfaces;

public interface IReportService
{
    Task<byte[]> GenerateCustomerReportAsync(int customerId, DateTime? startDate, DateTime? endDate);
    Task<byte[]> GenerateVehicleReportAsync(int vehicleId, DateTime? startDate, DateTime? endDate);
    Task<byte[]> GenerateMonthlyReportAsync(int month, int year);
    Task<byte[]> GenerateActiveOrdersReportAsync();
    Task SendDailyReportEmailAsync(string email);
}