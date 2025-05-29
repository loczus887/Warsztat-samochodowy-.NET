using WorkshopManager.Models;

namespace WorkshopManager.Services.Interfaces;

public interface IServiceOrderService
{
    Task<List<ServiceOrder>> GetAllServiceOrdersAsync();
    Task<ServiceOrder?> GetServiceOrderByIdAsync(int id);
    Task<List<ServiceOrder>> GetServiceOrdersByStatusAsync(OrderStatus status);
    Task<List<ServiceOrder>> GetServiceOrdersByMechanicAsync(string mechanicId);
    Task<List<ServiceOrder>> GetServiceOrdersByVehicleAsync(int vehicleId);
    Task<ServiceOrder> CreateServiceOrderAsync(ServiceOrder order);
    Task UpdateServiceOrderAsync(ServiceOrder order);
    Task DeleteServiceOrderAsync(int id);
    Task<ServiceOrder> AddTaskToOrderAsync(int orderId, ServiceTask task);
    Task<ServiceOrder> AddCommentToOrderAsync(int orderId, Comment comment);
    Task<ServiceOrder> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus);
    Task<decimal> CalculateOrderTotalAsync(int orderId);
}