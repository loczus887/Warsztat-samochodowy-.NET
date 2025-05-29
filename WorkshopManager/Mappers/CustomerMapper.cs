using Riok.Mapperly.Abstractions;
using WorkshopManager.Models;
using WorkshopManager.DTOs;

namespace WorkshopManager.Mappers;

[Mapper]
public partial class CustomerMapper
{
    public partial CustomerDto CustomerToDto(Customer customer);

    public partial Customer DtoToCustomer(CustomerDto dto);

    public partial IEnumerable<CustomerDto> CustomersToDto(IEnumerable<Customer> customers);

    // Metoda z ręczną implementacją dla złożonych mapowań
    public CustomerDto CustomerToDtoWithDetails(Customer customer)
    {
        var dto = CustomerToDto(customer);
        dto.VehicleCount = customer.Vehicles?.Count ?? 0;
        return dto;
    }
}