using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.Models;

public class Part
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Category { get; set; }

    [Range(0, 100000)]
    public decimal UnitPrice { get; set; }

    // Relacja 1:N z użytymi częściami
    public ICollection<UsedPart> UsedParts { get; set; } = new List<UsedPart>();
}