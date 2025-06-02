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

        // Konfiguracja typów decimal - NAPRAWA OSTRZEŻEŃ
        modelBuilder.Entity<Part>()
            .Property(p => p.UnitPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<ServiceTask>()
            .Property(st => st.LaborCost)
            .HasPrecision(18, 2);

        // Konfiguracja indeksów nieklastrowanych dla optymalizacji zapytań

        // Indeksy dla tabeli Customers
        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.PhoneNumber)
            .HasDatabaseName("IX_Customers_PhoneNumber");

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Email)
            .HasDatabaseName("IX_Customers_Email");

        // Indeksy dla tabeli Vehicles
        modelBuilder.Entity<Vehicle>()
            .HasIndex(v => v.RegistrationNumber)
            .HasDatabaseName("IX_Vehicles_RegistrationNumber");

        modelBuilder.Entity<Vehicle>()
            .HasIndex(v => v.VIN)
            .HasDatabaseName("IX_Vehicles_VIN");

        // Indeksy dla tabeli ServiceOrders
        modelBuilder.Entity<ServiceOrder>()
            .HasIndex(o => o.Status)
            .HasDatabaseName("IX_ServiceOrders_Status");

        modelBuilder.Entity<ServiceOrder>()
            .HasIndex(o => o.CreatedAt)
            .HasDatabaseName("IX_ServiceOrders_CreatedAt");

        modelBuilder.Entity<ServiceOrder>()
            .HasIndex(o => o.MechanicId)
            .HasDatabaseName("IX_ServiceOrders_MechanicId");

        // Indeks złożony dla ServiceOrders
        modelBuilder.Entity<ServiceOrder>()
            .HasIndex(o => new { o.Status, o.CreatedAt })
            .HasDatabaseName("IX_ServiceOrders_Status_CreatedAt");

        // Indeksy dla tabeli Parts
        modelBuilder.Entity<Part>()
            .HasIndex(p => p.Name)
            .HasDatabaseName("IX_Parts_Name");

        modelBuilder.Entity<Part>()
            .HasIndex(p => p.Category)
            .HasDatabaseName("IX_Parts_Category");
    }
}