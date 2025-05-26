using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.Models;

public class Vehicle
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Make { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Model { get; set; } = string.Empty;

    [StringLength(17, MinimumLength = 17)]
    public string? VIN { get; set; }

    [Required]
    [StringLength(10)]
    public string RegistrationNumber { get; set; } = string.Empty;

    public int Year { get; set; }

    public string? ImageUrl { get; set; }

    // Relacja N:1 z klientem
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    // Relacja 1:N ze zleceniami
    public ICollection<ServiceOrder> ServiceOrders { get; set; } = new List<ServiceOrder>();
}