using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.Models;

public enum OrderStatus
{
    New,
    InProgress,
    Completed,
    Cancelled
}

public class ServiceOrder
{
    public int Id { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? CompletedAt { get; set; }

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.New;

    // Relacja N:1 z pojazdem
    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    // Relacja N:1 z mechanikiem
    public string? MechanicId { get; set; }
    public ApplicationUser? Mechanic { get; set; }

    // Relacje 1:N
    public ICollection<ServiceTask> Tasks { get; set; } = new List<ServiceTask>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}