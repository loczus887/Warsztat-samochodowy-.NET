using Riok.Mapperly.Abstractions;
using WorkshopManager.Models;
using WorkshopManager.DTOs;

namespace WorkshopManager.Mappers;

[Mapper]
public partial class UsedPartMapper
{
    // Mapowanie z UsedPart do UsedPartDto
    [MapperIgnoreTarget(nameof(UsedPartDto.PartName))] // Ustawiane ręcznie
    [MapperIgnoreTarget(nameof(UsedPartDto.UnitPrice))] // Ustawiane ręcznie
    [MapperIgnoreTarget(nameof(UsedPartDto.TotalPrice))] // Obliczane ręcznie
    [MapperIgnoreSource(nameof(UsedPart.Part))] // Ignoruj relację Part
    [MapperIgnoreSource(nameof(UsedPart.ServiceTask))] // Ignoruj relację ServiceTask
    public partial UsedPartDto UsedPartToDto(UsedPart usedPart);

    // Mapowanie z UsedPartDto do UsedPart
    [MapperIgnoreSource(nameof(UsedPartDto.PartName))] // Tylko do wyświetlania
    [MapperIgnoreSource(nameof(UsedPartDto.UnitPrice))] // Tylko do wyświetlania
    [MapperIgnoreSource(nameof(UsedPartDto.TotalPrice))] // Tylko do wyświetlania
    [MapperIgnoreTarget(nameof(UsedPart.Part))] // Part będzie załadowany przez EF
    [MapperIgnoreTarget(nameof(UsedPart.ServiceTask))] // ServiceTask będzie załadowany przez EF
    public partial UsedPart DtoToUsedPart(UsedPartDto dto);

    // Mapowanie kolekcji
    public partial IEnumerable<UsedPartDto> UsedPartsToDto(IEnumerable<UsedPart> usedParts);

    // Metoda z pełnymi detalami (ręczna implementacja)
    public UsedPartDto UsedPartToDtoWithDetails(UsedPart usedPart)
    {
        var dto = UsedPartToDto(usedPart);

        // Ustaw szczegóły części
        dto.PartName = usedPart.Part?.Name;
        dto.UnitPrice = usedPart.Part?.UnitPrice ?? 0;
        dto.TotalPrice = usedPart.Quantity * dto.UnitPrice;

        return dto;
    }

    // Alternatywna metoda bez automatycznego mapowania 
    public UsedPartDto UsedPartToDtoManual(UsedPart usedPart)
    {
        var unitPrice = usedPart.Part?.UnitPrice ?? 0;

        return new UsedPartDto
        {
            Id = usedPart.Id,
            PartId = usedPart.PartId,
            Quantity = usedPart.Quantity,
            ServiceTaskId = usedPart.ServiceTaskId,
            PartName = usedPart.Part?.Name,
            UnitPrice = unitPrice,
            TotalPrice = usedPart.Quantity * unitPrice
        };
    }
}