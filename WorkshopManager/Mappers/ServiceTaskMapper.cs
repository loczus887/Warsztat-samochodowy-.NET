using Riok.Mapperly.Abstractions;
using WorkshopManager.Models;
using WorkshopManager.DTOs;

namespace WorkshopManager.Mappers;

[Mapper]
public partial class ServiceTaskMapper
{
    public partial ServiceTaskDto ServiceTaskToDto(ServiceTask task);

    [MapperIgnoreTarget(nameof(ServiceTaskDto.PartsCount))]
    [MapperIgnoreTarget(nameof(ServiceTaskDto.PartsCost))]
    [MapperIgnoreTarget(nameof(ServiceTaskDto.TotalCost))]
    public partial ServiceTask DtoToServiceTask(ServiceTaskDto dto);

    public partial IEnumerable<ServiceTaskDto> ServiceTasksToDto(IEnumerable<ServiceTask> tasks);

    // Metoda z ręczną implementacją dla złożonych mapowań
    public ServiceTaskDto ServiceTaskToDtoWithDetails(ServiceTask task)
    {
        var dto = ServiceTaskToDto(task);
        dto.PartsCount = task.UsedParts?.Count ?? 0;
        dto.PartsCost = CalculatePartsCost(task);
        dto.TotalCost = task.LaborCost + dto.PartsCost;
        return dto;
    }

    private decimal CalculatePartsCost(ServiceTask task)
    {
        if (task.UsedParts == null) return 0;

        return task.UsedParts.Sum(up => up.Quantity * (up.Part?.UnitPrice ?? 0));
    }
}