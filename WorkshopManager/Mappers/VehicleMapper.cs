using Riok.Mapperly.Abstractions;
using WorkshopManager.Models;
using WorkshopManager.DTOs;

namespace WorkshopManager.Mappers;

[Mapper]
public partial class VehicleMapper
{
    public partial VehicleDto VehicleToDto(Vehicle vehicle);

    [MapperIgnoreTarget(nameof(VehicleDto.ImageFile))]
    [MapperIgnoreTarget(nameof(VehicleDto.CustomerFullName))]
    [MapperIgnoreTarget(nameof(VehicleDto.ServiceOrderCount))]
    public partial Vehicle DtoToVehicle(VehicleDto dto);

    public partial IEnumerable<VehicleDto> VehiclesToDto(IEnumerable<Vehicle> vehicles);

    // Metoda z ręczną implementacją dla złożonych mapowań
    public VehicleDto VehicleToDtoWithDetails(Vehicle vehicle)
    {
        var dto = VehicleToDto(vehicle);
        dto.CustomerFullName = vehicle.Customer != null
            ? $"{vehicle.Customer.FirstName} {vehicle.Customer.LastName}"
            : null;
        dto.ServiceOrderCount = vehicle.ServiceOrders?.Count ?? 0;
        return dto;
    }
}