using Riok.Mapperly.Abstractions;
using WorkshopManager.Models;
using WorkshopManager.DTOs;

namespace WorkshopManager.Mappers;

[Mapper]
public partial class CustomerMapper
{
    // Mapowanie z Customer do CustomerDto
    [MapProperty(nameof(Customer.Vehicles), nameof(CustomerDto.VehicleCount), Use = nameof(GetVehicleCount))]
    [MapperIgnoreTarget(nameof(CustomerDto.FullName))] // FullName to computed property
    [MapperIgnoreSource(nameof(Customer.Vehicles))] // Ignoruj kolekcję Vehicles (używamy GetVehicleCount)
    public partial CustomerDto CustomerToDto(Customer customer);

    // Mapowanie z CustomerDto do Customer
    [MapperIgnoreSource(nameof(CustomerDto.VehicleCount))] // VehicleCount to tylko właściwość wyświetlania
    [MapperIgnoreSource(nameof(CustomerDto.FullName))] // FullName to computed property
    [MapperIgnoreTarget(nameof(Customer.Vehicles))] // Vehicles będą załadowane przez EF
    public partial Customer DtoToCustomer(CustomerDto dto);

    // Mapowanie kolekcji
    public partial IEnumerable<CustomerDto> CustomersToDto(IEnumerable<Customer> customers);

    // Pomocnicza metoda do liczenia pojazdów
    private int GetVehicleCount(ICollection<Vehicle>? vehicles)
    {
        return vehicles?.Count ?? 0;
    }

    // Metoda z pełnymi detalami (ręczna implementacja)
    public CustomerDto CustomerToDtoWithDetails(Customer customer)
    {
        var dto = CustomerToDto(customer);

        // VehicleCount już jest mapowany automatycznie
        // FullName to computed property, więc nie trzeba go ustawiać

        return dto;
    }

    // Alternatywna metoda bez automatycznego mapowania (jeśli nadal są problemy)
    public CustomerDto CustomerToDtoManual(Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            PhoneNumber = customer.PhoneNumber,
            Email = customer.Email,
            VehicleCount = customer.Vehicles?.Count ?? 0
            // FullName nie trzeba ustawiać - to computed property
        };
    }
}