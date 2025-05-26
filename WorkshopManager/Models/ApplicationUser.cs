using Microsoft.AspNetCore.Identity;

namespace WorkshopManager.Models;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    // Relacja 1:N z przypisanymi zleceniami
    public ICollection<ServiceOrder> AssignedOrders { get; set; } = new List<ServiceOrder>();

    // Relacja 1:N z komentarzami
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}