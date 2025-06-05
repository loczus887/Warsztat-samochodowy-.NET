using Riok.Mapperly.Abstractions;
using WorkshopManager.Models;
using WorkshopManager.DTOs;

namespace WorkshopManager.Mappers;

[Mapper]
public partial class VehicleMapper
{
    // Podstawowe mapowanie z Vehicle do VehicleDto
    [MapProperty(nameof(Vehicle.ServiceOrders), nameof(VehicleDto.ServiceOrderCount), Use = nameof(GetServiceOrderCount))]
    [MapperIgnoreTarget(nameof(VehicleDto.ImageFile))]
    [MapperIgnoreTarget(nameof(VehicleDto.CustomerFullName))]
    [MapperIgnoreTarget(nameof(VehicleDto.VehicleInfo))] // VehicleInfo to computed property
    public partial VehicleDto VehicleToDto(Vehicle vehicle);

    // Mapowanie z VehicleDto do Vehicle
    [MapperIgnoreSource(nameof(VehicleDto.ImageFile))]
    [MapperIgnoreSource(nameof(VehicleDto.CustomerFullName))]
    [MapperIgnoreSource(nameof(VehicleDto.ServiceOrderCount))]
    [MapperIgnoreSource(nameof(VehicleDto.VehicleInfo))] // VehicleInfo to computed property
    [MapperIgnoreTarget(nameof(Vehicle.Customer))] // Customer będzie ustawiony przez Entity Framework
    [MapperIgnoreTarget(nameof(Vehicle.ServiceOrders))] // ServiceOrders będą załadowane przez EF
    public partial Vehicle DtoToVehicle(VehicleDto dto);

    // Mapowanie kolekcji
    public partial IEnumerable<VehicleDto> VehiclesToDto(IEnumerable<Vehicle> vehicles);

    // Pomocnicza metoda do liczenia ServiceOrders
    private int GetServiceOrderCount(ICollection<ServiceOrder>? serviceOrders)
    {
        return serviceOrders?.Count ?? 0;
    }

    // Metoda z pełnymi detalami (ręczna implementacja)
    public VehicleDto VehicleToDtoWithDetails(Vehicle vehicle)
    {
        var dto = VehicleToDto(vehicle);

        // Ustaw pełną nazwę klienta
        dto.CustomerFullName = vehicle.Customer != null
            ? $"{vehicle.Customer.FirstName} {vehicle.Customer.LastName}"
            : null;

        // ServiceOrderCount już jest mapowany automatycznie
        // VehicleInfo to computed property, więc nie trzeba go ustawiać

        return dto;
    }

    // Alternatywna metoda bez automatycznego mapowania (jeśli nadal są problemy)
    public VehicleDto VehicleToDtoManual(Vehicle vehicle)
    {
        return new VehicleDto
        {
            Id = vehicle.Id,
            Make = vehicle.Make,
            Model = vehicle.Model,
            VIN = vehicle.VIN,
            RegistrationNumber = vehicle.RegistrationNumber,
            Year = vehicle.Year,
            ImageUrl = vehicle.ImageUrl,
            CustomerId = vehicle.CustomerId,
            CustomerFullName = vehicle.Customer != null
                ? $"{vehicle.Customer.FirstName} {vehicle.Customer.LastName}"
                : null,
            ServiceOrderCount = vehicle.ServiceOrders?.Count ?? 0
            // VehicleInfo nie trzeba ustawiać - to computed property
        };
    }
}