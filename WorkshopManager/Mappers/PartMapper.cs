using Riok.Mapperly.Abstractions;
using WorkshopManager.Models;
using WorkshopManager.DTOs;

namespace WorkshopManager.Mappers;

[Mapper]
public partial class PartMapper
{
    public partial PartDto PartToDto(Part part);

    [MapperIgnoreTarget(nameof(PartDto.UsageCount))]
    public partial Part DtoToPart(PartDto dto);

    public partial IEnumerable<PartDto> PartsToDto(IEnumerable<Part> parts);

    // Metoda z ręczną implementacją
    public PartDto PartToDtoWithUsage(Part part)
    {
        var dto = PartToDto(part);
        dto.UsageCount = part.UsedParts?.Count ?? 0;
        return dto;
    }
}