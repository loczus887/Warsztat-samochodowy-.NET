using Microsoft.EntityFrameworkCore;
using WorkshopManager.Data;
using WorkshopManager.Models;
using WorkshopManager.Services.Interfaces;

namespace WorkshopManager.Services;

public class ServiceOrderService : IServiceOrderService
{
    private readonly ApplicationDbContext _context;

    public ServiceOrderService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ServiceOrder>> GetAllServiceOrdersAsync()
    {
        return await _context.ServiceOrders
            .Include(o => o.Vehicle)
                .ThenInclude(v => v.Customer)
            .Include(o => o.Mechanic)
            .Include(o => o.Tasks)
            .Include(o => o.Comments)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<ServiceOrder?> GetServiceOrderByIdAsync(int id)
    {
        return await _context.ServiceOrders
            .Include(o => o.Vehicle)
                .ThenInclude(v => v.Customer)
            .Include(o => o.Mechanic)
            .Include(o => o.Tasks)
                .ThenInclude(t => t.UsedParts)
                    .ThenInclude(up => up.Part)
            .Include(o => o.Comments)
                .ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<List<ServiceOrder>> GetServiceOrdersByStatusAsync(OrderStatus status)
    {
        return await _context.ServiceOrders
            .Include(o => o.Vehicle)
                .ThenInclude(v => v.Customer)
            .Include(o => o.Mechanic)
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ServiceOrder>> GetServiceOrdersByMechanicAsync(string mechanicId)
    {
        return await _context.ServiceOrders
            .Include(o => o.Vehicle)
                .ThenInclude(v => v.Customer)
            .Include(o => o.Mechanic)
            .Where(o => o.MechanicId == mechanicId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ServiceOrder>> GetServiceOrdersByVehicleAsync(int vehicleId)
    {
        return await _context.ServiceOrders
            .Include(o => o.Vehicle)
                .ThenInclude(v => v.Customer)
            .Include(o => o.Mechanic)
            .Include(o => o.Tasks)
            .Where(o => o.VehicleId == vehicleId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<ServiceOrder> CreateServiceOrderAsync(ServiceOrder order)
    {
        _context.ServiceOrders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task UpdateServiceOrderAsync(ServiceOrder order)
    {
        _context.Entry(order).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteServiceOrderAsync(int id)
    {
        var order = await _context.ServiceOrders.FindAsync(id);
        if (order != null)
        {
            _context.ServiceOrders.Remove(order);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<ServiceOrder> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
    {
        var order = await GetServiceOrderByIdAsync(orderId);
        if (order == null)
            throw new ArgumentException("Zlecenie nie istnieje");

        order.Status = newStatus;

        if (newStatus == OrderStatus.Completed)
        {
            order.CompletedAt = DateTime.Now;
        }

        await UpdateServiceOrderAsync(order);
        return order;
    }

    public async Task<decimal> CalculateOrderTotalAsync(int orderId)
    {
        var order = await GetServiceOrderByIdAsync(orderId);
        if (order == null)
            return 0;

        decimal total = 0;

        foreach (var task in order.Tasks)
        {
            total += task.LaborCost;

            foreach (var usedPart in task.UsedParts)
            {
                total += usedPart.Quantity * usedPart.Part.UnitPrice;
            }
        }

        return total;
    }

    public async Task<ServiceOrder> AddTaskToOrderAsync(int orderId, ServiceTask task)
    {
        task.ServiceOrderId = orderId;
        _context.ServiceTasks.Add(task);
        await _context.SaveChangesAsync();

        return await GetServiceOrderByIdAsync(orderId);
    }

    public async Task<ServiceOrder> AddCommentToOrderAsync(int orderId, Comment comment)
    {
        comment.ServiceOrderId = orderId;
        comment.CreatedAt = DateTime.Now;
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        return await GetServiceOrderByIdAsync(orderId);
    }
}