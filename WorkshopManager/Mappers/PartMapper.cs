using Riok.Mapperly.Abstractions;
using WorkshopManager.Models;
using WorkshopManager.DTOs;

namespace WorkshopManager.Mappers;

[Mapper]
public partial class PartMapper
{
    // Mapowanie z Part do PartDto
    [MapProperty(nameof(Part.UsedParts), nameof(PartDto.UsageCount), Use = nameof(GetUsageCount))]
    [MapperIgnoreSource(nameof(Part.UsedParts))] // Ignoruj kolekcję UsedParts (używamy GetUsageCount)
    public partial PartDto PartToDto(Part part);

    // Mapowanie z PartDto do Part
    [MapperIgnoreSource(nameof(PartDto.UsageCount))] // UsageCount to tylko właściwość wyświetlania
    [MapperIgnoreTarget(nameof(Part.UsedParts))] // UsedParts będą załadowane przez EF
    public partial Part DtoToPart(PartDto dto);

    // Mapowanie kolekcji
    public partial IEnumerable<PartDto> PartsToDto(IEnumerable<Part> parts);

    // Pomocnicza metoda do liczenia użyć
    private int GetUsageCount(ICollection<UsedPart>? usedParts)
    {
        return usedParts?.Count ?? 0;
    }

    // Metoda z pełnymi detalami (ręczna implementacja)
    public PartDto PartToDtoWithUsage(Part part)
    {
        var dto = PartToDto(part);

        // UsageCount już jest mapowany automatycznie

        return dto;
    }

    // Alternatywna metoda bez automatycznego mapowania 
    public PartDto PartToDtoManual(Part part)
    {
        return new PartDto
        {
            Id = part.Id,
            Name = part.Name,
            Category = part.Category,
            UnitPrice = part.UnitPrice,
            UsageCount = part.UsedParts?.Count ?? 0
        };
    }
}