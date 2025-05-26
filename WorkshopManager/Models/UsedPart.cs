using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.Models;

public class UsedPart
{
    public int Id { get; set; }

    [Range(1, 1000)]
    public int Quantity { get; set; }

    // Relacja N:1 z częścią
    public int PartId { get; set; }
    public Part? Part { get; set; }

    // Relacja N:1 z czynnością
    public int ServiceTaskId { get; set; }
    public ServiceTask? ServiceTask { get; set; }
}