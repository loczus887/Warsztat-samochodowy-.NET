using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.Models;

public class Customer
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(15)]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(100)]
    public string? Email { get; set; }

    // Relacja 1:N z pojazdami
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}