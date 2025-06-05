using Riok.Mapperly.Abstractions;
using WorkshopManager.Models;
using WorkshopManager.DTOs;

namespace WorkshopManager.Mappers;

[Mapper]
public partial class ServiceOrderMapper
{
    // Mapowanie z ServiceOrder do ServiceOrderDto
    [MapProperty(nameof(ServiceOrder.Tasks), nameof(ServiceOrderDto.TaskCount), Use = nameof(GetTaskCount))]
    [MapperIgnoreTarget(nameof(ServiceOrderDto.VehicleInfo))] // Ustawiane ręcznie
    [MapperIgnoreTarget(nameof(ServiceOrderDto.CustomerName))] // Ustawiane ręcznie
    [MapperIgnoreTarget(nameof(ServiceOrderDto.MechanicName))] // Ustawiane ręcznie
    [MapperIgnoreTarget(nameof(ServiceOrderDto.TotalCost))] // Obliczane ręcznie
    [MapperIgnoreSource(nameof(ServiceOrder.Vehicle))] // Ignoruj relację Vehicle
    [MapperIgnoreSource(nameof(ServiceOrder.Mechanic))] // Ignoruj relację Mechanic
    [MapperIgnoreSource(nameof(ServiceOrder.Tasks))] // Ignoruj kolekcję Tasks (używamy GetTaskCount)
    [MapperIgnoreSource(nameof(ServiceOrder.Comments))] // Ignoruj kolekcję Comments
    public partial ServiceOrderDto ServiceOrderToDto(ServiceOrder order);

    // Mapowanie z ServiceOrderDto do ServiceOrder
    [MapperIgnoreSource(nameof(ServiceOrderDto.VehicleInfo))] // Tylko do wyświetlania
    [MapperIgnoreSource(nameof(ServiceOrderDto.CustomerName))] // Tylko do wyświetlania
    [MapperIgnoreSource(nameof(ServiceOrderDto.MechanicName))] // Tylko do wyświetlania
    [MapperIgnoreSource(nameof(ServiceOrderDto.TaskCount))] // Tylko do wyświetlania
    [MapperIgnoreSource(nameof(ServiceOrderDto.TotalCost))] // Tylko do wyświetlania
    [MapperIgnoreTarget(nameof(ServiceOrder.Vehicle))] // Vehicle będzie załadowany przez EF
    [MapperIgnoreTarget(nameof(ServiceOrder.Mechanic))] // Mechanic będzie załadowany przez EF
    [MapperIgnoreTarget(nameof(ServiceOrder.Tasks))] // Tasks będą załadowane przez EF
    [MapperIgnoreTarget(nameof(ServiceOrder.Comments))] // Comments będą załadowane przez EF
    public partial ServiceOrder DtoToServiceOrder(ServiceOrderDto dto);

    // Mapowanie kolekcji
    public partial IEnumerable<ServiceOrderDto> ServiceOrdersToDto(IEnumerable<ServiceOrder> orders);

    // Pomocnicza metoda do liczenia zadań
    private int GetTaskCount(ICollection<ServiceTask>? tasks)
    {
        return tasks?.Count ?? 0;
    }

    // Metoda z pełnymi detalami (ręczna implementacja)
    public ServiceOrderDto ServiceOrderToDtoWithDetails(ServiceOrder order)
    {
        var dto = ServiceOrderToDto(order);

        // Ustaw informacje o pojeździe i kliencie
        if (order.Vehicle != null)
        {
            dto.VehicleInfo = $"{order.Vehicle.Make} {order.Vehicle.Model} ({order.Vehicle.RegistrationNumber})";

            if (order.Vehicle.Customer != null)
            {
                dto.CustomerName = $"{order.Vehicle.Customer.FirstName} {order.Vehicle.Customer.LastName}";
            }
        }

        // Ustaw nazwę mechanika
        if (order.Mechanic != null)
        {
            dto.MechanicName = $"{order.Mechanic.FirstName} {order.Mechanic.LastName}";
        }

        // TaskCount już jest mapowany automatycznie

        // Oblicz całkowity koszt
        dto.TotalCost = CalculateTotalCost(order);

        return dto;
    }

    // Alternatywna metoda bez automatycznego mapowania (jeśli nadal są problemy)
    public ServiceOrderDto ServiceOrderToDtoManual(ServiceOrder order)
    {
        return new ServiceOrderDto
        {
            Id = order.Id,
            CreatedAt = order.CreatedAt,
            CompletedAt = order.CompletedAt,
            Description = order.Description,
            Status = order.Status,
            VehicleId = order.VehicleId,
            MechanicId = order.MechanicId,
            VehicleInfo = order.Vehicle != null
                ? $"{order.Vehicle.Make} {order.Vehicle.Model} ({order.Vehicle.RegistrationNumber})"
                : null,
            CustomerName = order.Vehicle?.Customer != null
                ? $"{order.Vehicle.Customer.FirstName} {order.Vehicle.Customer.LastName}"
                : null,
            MechanicName = order.Mechanic != null
                ? $"{order.Mechanic.FirstName} {order.Mechanic.LastName}"
                : null,
            TaskCount = order.Tasks?.Count ?? 0,
            TotalCost = CalculateTotalCost(order)
        };
    }

    // Obliczanie całkowitego kosztu
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