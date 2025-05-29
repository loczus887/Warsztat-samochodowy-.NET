using Riok.Mapperly.Abstractions;
using WorkshopManager.Models;
using WorkshopManager.DTOs;

namespace WorkshopManager.Mappers;

[Mapper]
public partial class UsedPartMapper
{
    public partial UsedPartDto UsedPartToDto(UsedPart usedPart);

    [MapperIgnoreTarget(nameof(UsedPartDto.PartName))]
    [MapperIgnoreTarget(nameof(UsedPartDto.UnitPrice))]
    [MapperIgnoreTarget(nameof(UsedPartDto.TotalPrice))]
    public partial UsedPart DtoToUsedPart(UsedPartDto dto);

    public partial IEnumerable<UsedPartDto> UsedPartsToDto(IEnumerable<UsedPart> usedParts);

    // Metoda z ręczną implementacją
    public UsedPartDto UsedPartToDtoWithDetails(UsedPart usedPart)
    {
        var dto = UsedPartToDto(usedPart);
        dto.PartName = usedPart.Part?.Name;
        dto.UnitPrice = usedPart.Part?.UnitPrice ?? 0;
        dto.TotalPrice = usedPart.Quantity * dto.UnitPrice;
        return dto;
    }
}