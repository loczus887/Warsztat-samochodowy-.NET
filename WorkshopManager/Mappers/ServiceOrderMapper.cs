using Riok.Mapperly.Abstractions;
using WorkshopManager.Models;
using WorkshopManager.DTOs;

namespace WorkshopManager.Mappers;

[Mapper]
public partial class ServiceOrderMapper
{
    public partial ServiceOrderDto ServiceOrderToDto(ServiceOrder order);

    [MapperIgnoreTarget(nameof(ServiceOrderDto.VehicleInfo))]
    [MapperIgnoreTarget(nameof(ServiceOrderDto.CustomerName))]
    [MapperIgnoreTarget(nameof(ServiceOrderDto.MechanicName))]
    [MapperIgnoreTarget(nameof(ServiceOrderDto.TaskCount))]
    [MapperIgnoreTarget(nameof(ServiceOrderDto.TotalCost))]
    public partial ServiceOrder DtoToServiceOrder(ServiceOrderDto dto);

    public partial IEnumerable<ServiceOrderDto> ServiceOrdersToDto(IEnumerable<ServiceOrder> orders);

    // Metoda z ręczną implementacją dla złożonych mapowań
    public ServiceOrderDto ServiceOrderToDtoWithDetails(ServiceOrder order)
    {
        var dto = ServiceOrderToDto(order);

        if (order.Vehicle != null)
        {
            dto.VehicleInfo = $"{order.Vehicle.Make} {order.Vehicle.Model} ({order.Vehicle.RegistrationNumber})";

            if (order.Vehicle.Customer != null)
            {
                dto.CustomerName = $"{order.Vehicle.Customer.FirstName} {order.Vehicle.Customer.LastName}";
            }
        }

        if (order.Mechanic != null)
        {
            dto.MechanicName = $"{order.Mechanic.FirstName} {order.Mechanic.LastName}";
        }

        dto.TaskCount = order.Tasks?.Count ?? 0;
        dto.TotalCost = CalculateTotalCost(order);

        return dto;
    }

    private decimal CalculateTotalCost(ServiceOrder order)
    {
        if (order.Tasks == null) return 0;

        decimal total = 0;
        foreach (var task in order.Tasks)
        {
            total += task.LaborCost;

            if (task.UsedParts != null)
            {
                foreach (var usedPart in task.UsedParts)
                {
                    total += usedPart.Quantity * (usedPart.Part?.UnitPrice ?? 0);
                }
            }
        }

        return total;
    }
}