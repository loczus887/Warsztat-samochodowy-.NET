using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WorkshopManager.Models;

namespace WorkshopManager.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<ServiceOrder> ServiceOrders { get; set; }
    public DbSet<ServiceTask> ServiceTasks { get; set; }
    public DbSet<Part> Parts { get; set; }
    public DbSet<UsedPart> UsedParts { get; set; }
    public DbSet<Comment> Comments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Konfiguracja relacji
        modelBuilder.Entity<Vehicle>()
            .HasOne(v => v.Customer)
            .WithMany(c => c.Vehicles)
            .HasForeignKey(v => v.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ServiceOrder>()
            .HasOne(o => o.Vehicle)
            .WithMany(v => v.ServiceOrders)
            .HasForeignKey(o => o.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ServiceOrder>()
            .HasOne(o => o.Mechanic)
            .WithMany(m => m.AssignedOrders)
            .HasForeignKey(o => o.MechanicId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ServiceTask>()
            .HasOne(t => t.ServiceOrder)
            .WithMany(o => o.Tasks)
            .HasForeignKey(t => t.ServiceOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UsedPart>()
            .HasOne(up => up.Part)
            .WithMany(p => p.UsedParts)
            .HasForeignKey(up => up.PartId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UsedPart>()
            .HasOne(up => up.ServiceTask)
            .WithMany(t => t.UsedParts)
            .HasForeignKey(up => up.ServiceTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.ServiceOrder)
            .WithMany(o => o.Comments)
            .HasForeignKey(c => c.ServiceOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Author)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indeksy nieklastrowane
        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.PhoneNumber);

        modelBuilder.Entity<Vehicle>()
            .HasIndex(v => v.RegistrationNumber);

        modelBuilder.Entity<Vehicle>()
            .HasIndex(v => v.VIN);
    }
}