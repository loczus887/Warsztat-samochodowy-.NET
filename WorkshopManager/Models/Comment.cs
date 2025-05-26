using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.Models;

public class Comment
{
    public int Id { get; set; }

    [Required]
    [StringLength(1000)]
    public string Content { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Relacja N:1 ze zleceniem
    public int ServiceOrderId { get; set; }
    public ServiceOrder? ServiceOrder { get; set; }

    // Relacja N:1 z autorem
    public string? AuthorId { get; set; }
    public ApplicationUser? Author { get; set; }
}