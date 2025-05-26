using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.Models;

public class ServiceTask
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    [Range(0, 100000)]
    public decimal LaborCost { get; set; }

    // Relacja N:1 ze zleceniem
    public int ServiceOrderId { get; set; }
    public ServiceOrder? ServiceOrder { get; set; }

    // Relacja 1:N z użytymi częściami
    public ICollection<UsedPart> UsedParts { get; set; } = new List<UsedPart>();
}