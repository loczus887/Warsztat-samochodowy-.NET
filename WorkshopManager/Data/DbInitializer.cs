using Microsoft.AspNetCore.Identity;
using WorkshopManager.Models;

namespace WorkshopManager.Data;

public static class DbInitializer
{
    public static async Task Initialize(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // Upewnij się, że baza danych jest utworzona
        context.Database.EnsureCreated();

        // Utwórz role, jeśli nie istnieją
        string[] roles = { "Admin", "Mechanic", "Receptionist" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Utwórz domyślnych użytkowników
        if (!context.Users.Any())
        {
            var admin = new ApplicationUser
            {
                UserName = "admin@workshop.com",
                Email = "admin@workshop.com",
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "User"
            };

            var mechanic = new ApplicationUser
            {
                UserName = "mechanic@workshop.com",
                Email = "mechanic@workshop.com",
                EmailConfirmed = true,
                FirstName = "John",
                LastName = "Smith"
            };

            var receptionist = new ApplicationUser
            {
                UserName = "receptionist@workshop.com",
                Email = "receptionist@workshop.com",
                EmailConfirmed = true,
                FirstName = "Anna",
                LastName = "Johnson"
            };

            await userManager.CreateAsync(admin, "Admin123!");
            await userManager.CreateAsync(mechanic, "Mechanic123!");
            await userManager.CreateAsync(receptionist, "Reception123!");

            await userManager.AddToRoleAsync(admin, "Admin");
            await userManager.AddToRoleAsync(mechanic, "Mechanic");
            await userManager.AddToRoleAsync(receptionist, "Receptionist");
        }

        // Dodaj katalog części, jeśli pusty
        if (!context.Parts.Any())
        {
            var parts = new List<Part>
            {
                new Part { Name = "Filtr oleju", Category = "Filtry", UnitPrice = 35.99m },
                new Part { Name = "Filtr powietrza", Category = "Filtry", UnitPrice = 45.50m },
                new Part { Name = "Klocki hamulcowe (przód)", Category = "Hamulce", UnitPrice = 120.00m },
                new Part { Name = "Tarcze hamulcowe (przód)", Category = "Hamulce", UnitPrice = 180.00m },
                new Part { Name = "Olej silnikowy 5W40 (1L)", Category = "Oleje", UnitPrice = 30.00m }
            };

            context.Parts.AddRange(parts);
        }

        // Dodaj przykładowych klientów
        if (!context.Customers.Any())
        {
            var customers = new List<Customer>
            {
                new Customer
                {
                    FirstName = "Tomasz",
                    LastName = "Nowak",
                    PhoneNumber = "555-123-456",
                    Email = "tomasz.nowak@example.com"
                },
                new Customer
                {
                    FirstName = "Anna",
                    LastName = "Kowalska",
                    PhoneNumber = "555-789-012",
                    Email = "anna.kowalska@example.com"
                }
            };

            context.Customers.AddRange(customers);
            await context.SaveChangesAsync();

            // Dodaj pojazdy
            var vehicles = new List<Vehicle>
            {
                new Vehicle
                {
                    Make = "Toyota",
                    Model = "Corolla",
                    Year = 2018,
                    RegistrationNumber = "WA12345",
                    VIN = "1HGCM82633A123456",
                    CustomerId = customers[0].Id
                },
                new Vehicle
                {
                    Make = "Ford",
                    Model = "Focus",
                    Year = 2020,
                    RegistrationNumber = "WB78901",
                    VIN = "2FMDK48C87BA12345",
                    CustomerId = customers[0].Id
                },
                new Vehicle
                {
                    Make = "Volkswagen",
                    Model = "Golf",
                    Year = 2019,
                    RegistrationNumber = "WC45678",
                    VIN = "WVWZZZ1KZAW123456",
                    CustomerId = customers[1].Id
                }
            };

            context.Vehicles.AddRange(vehicles);
        }

        await context.SaveChangesAsync();
    }
}