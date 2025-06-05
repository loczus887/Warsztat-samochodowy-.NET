using Riok.Mapperly.Abstractions;
using WorkshopManager.Models;
using WorkshopManager.DTOs;

namespace WorkshopManager.Mappers;

[Mapper]
public partial class ServiceTaskMapper
{
    // Mapowanie z ServiceTask do ServiceTaskDto
    [MapProperty(nameof(ServiceTask.UsedParts), nameof(ServiceTaskDto.PartsCount), Use = nameof(GetPartsCount))]
    [MapperIgnoreTarget(nameof(ServiceTaskDto.PartsCost))] // Obliczane ręcznie
    [MapperIgnoreTarget(nameof(ServiceTaskDto.TotalCost))] // Obliczane ręcznie
    [MapperIgnoreSource(nameof(ServiceTask.ServiceOrder))] // Ignoruj relację ServiceOrder
    [MapperIgnoreSource(nameof(ServiceTask.UsedParts))] // Ignoruj kolekcję UsedParts (używamy GetPartsCount)
    public partial ServiceTaskDto ServiceTaskToDto(ServiceTask task);

    // Mapowanie z ServiceTaskDto do ServiceTask
    [MapperIgnoreSource(nameof(ServiceTaskDto.PartsCount))] // Tylko do wyświetlania
    [MapperIgnoreSource(nameof(ServiceTaskDto.PartsCost))] // Tylko do wyświetlania
    [MapperIgnoreSource(nameof(ServiceTaskDto.TotalCost))] // Tylko do wyświetlania
    [MapperIgnoreTarget(nameof(ServiceTask.ServiceOrder))] // ServiceOrder będzie załadowany przez EF
    [MapperIgnoreTarget(nameof(ServiceTask.UsedParts))] // UsedParts będą załadowane przez EF
    public partial ServiceTask DtoToServiceTask(ServiceTaskDto dto);

    // Mapowanie kolekcji
    public partial IEnumerable<ServiceTaskDto> ServiceTasksToDto(IEnumerable<ServiceTask> tasks);

    // Pomocnicza metoda do liczenia części
    private int GetPartsCount(ICollection<UsedPart>? usedParts)
    {
        return usedParts?.Count ?? 0;
    }

    // Metoda z pełnymi detalami (ręczna implementacja)
    public ServiceTaskDto ServiceTaskToDtoWithDetails(ServiceTask task)
    {
        var dto = ServiceTaskToDto(task);

        // PartsCount już jest mapowany automatycznie
        dto.PartsCost = CalculatePartsCost(task);
        dto.TotalCost = task.LaborCost + dto.PartsCost;

        return dto;
    }

    // Alternatywna metoda bez automatycznego mapowania (jeśli nadal są problemy)
    public ServiceTaskDto ServiceTaskToDtoManual(ServiceTask task)
    {
        var partsCost = CalculatePartsCost(task);

        return new ServiceTaskDto
        {
            Id = task.Id,
            Description = task.Description,
            LaborCost = task.LaborCost,
            ServiceOrderId = task.ServiceOrderId,
            PartsCount = task.UsedParts?.Count ?? 0,
            PartsCost = partsCost,
            TotalCost = task.LaborCost + partsCost
        };
    }

    // Obliczanie kosztu części
    private decimal CalculatePartsCost(ServiceTask task)
    {
        if (task.UsedParts == null) return 0;

        return task.UsedParts.Sum(up => up.Quantity * (up.Part?.UnitPrice ?? 0));
    }
}