using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using WorkshopManager.Data;
using WorkshopManager.Models;
using WorkshopManager.Services;

namespace WorkshopManager.Tests.Services;

[TestFixture]
public class CustomerServiceTests
{
    private ApplicationDbContext _context;
    private CustomerService _customerService;
    private Fixture _fixture;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _customerService = new CustomerService(_context);
        _fixture = new Fixture();

        // Configure AutoFixture to avoid circular references
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }

    [Test]
    public async Task GetAllCustomersAsync_ShouldReturnAllCustomersOrderedByLastName()
    {
        // Arrange
        var customers = new List<Customer>
        {
            CreateTestCustomer("John", "Smith"),
            CreateTestCustomer("Jane", "Adams"),
            CreateTestCustomer("Bob", "Johnson")
        };

        _context.Customers.AddRange(customers);
        await _context.SaveChangesAsync();

        // Act
        var result = await _customerService.GetAllCustomersAsync();

        // Assert
        result.Should().HaveCount(3);
        result[0].LastName.Should().Be("Adams");
        result[1].LastName.Should().Be("Johnson");
        result[2].LastName.Should().Be("Smith");
    }

    [Test]
    public async Task GetCustomerByIdAsync_WithValidId_ShouldReturnCustomerWithVehicles()
    {
        // Arrange
        var customer = CreateTestCustomer();
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var vehicle = CreateTestVehicle(customer.Id);
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        // Act
        var result = await _customerService.GetCustomerByIdAsync(customer.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(customer.Id);
        result.FirstName.Should().Be(customer.FirstName);
        result.LastName.Should().Be(customer.LastName);
        result.Vehicles.Should().HaveCount(1);
    }

    [Test]
    public async Task GetCustomerByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var invalidId = 99999;

        // Act
        var result = await _customerService.GetCustomerByIdAsync(invalidId);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task CreateCustomerAsync_WithValidCustomer_ShouldCreateAndReturnCustomer()
    {
        // Arrange
        var customer = CreateTestCustomerWithoutId();

        // Act
        var result = await _customerService.CreateCustomerAsync(customer);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.FirstName.Should().Be(customer.FirstName);
        result.LastName.Should().Be(customer.LastName);
        result.Email.Should().Be(customer.Email);
        result.PhoneNumber.Should().Be(customer.PhoneNumber);

        // Verify in database
        var dbCustomer = await _context.Customers.FindAsync(result.Id);
        dbCustomer.Should().NotBeNull();
    }

    [Test]
    public async Task UpdateCustomerAsync_WithValidCustomer_ShouldUpdateCustomer()
    {
        // Arrange
        var customer = CreateTestCustomer();
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var updatedFirstName = "UpdatedFirstName";
        var updatedLastName = "UpdatedLastName";
        customer.FirstName = updatedFirstName;
        customer.LastName = updatedLastName;

        // Act
        await _customerService.UpdateCustomerAsync(customer);

        // Assert - Verify in database
        var dbCustomer = await _context.Customers.FindAsync(customer.Id);
        dbCustomer.Should().NotBeNull();
        dbCustomer!.FirstName.Should().Be(updatedFirstName);
        dbCustomer.LastName.Should().Be(updatedLastName);
    }

    [Test]
    public async Task UpdateCustomerAsync_WithNonExistentCustomer_ShouldThrowException()
    {
        // Arrange
        var nonExistentCustomer = CreateTestCustomer();
        nonExistentCustomer.Id = 99999; // Non-existent ID

        // Act & Assert
        var act = async () => await _customerService.UpdateCustomerAsync(nonExistentCustomer);
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Test]
    public async Task DeleteCustomerAsync_WithValidId_ShouldDeleteCustomer()
    {
        // Arrange
        var customer = CreateTestCustomer();
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        // Act
        await _customerService.DeleteCustomerAsync(customer.Id);

        // Assert - Verify deletion
        var dbCustomer = await _context.Customers.FindAsync(customer.Id);
        dbCustomer.Should().BeNull();
    }

    [Test]
    public async Task DeleteCustomerAsync_WithInvalidId_ShouldNotThrowException()
    {
        // Arrange
        var invalidId = 99999;

        // Act & Assert - Should not throw exception
        var act = async () => await _customerService.DeleteCustomerAsync(invalidId);
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task SearchCustomersAsync_WithValidSearchTerm_ShouldReturnMatchingCustomers()
    {
        // Arrange
        var customers = new List<Customer>
        {
            CreateTestCustomer("John", "Smith"),
            CreateTestCustomer("Jane", "Doe"),
            CreateTestCustomer("Bob", "Johnson")
        };

        _context.Customers.AddRange(customers);
        await _context.SaveChangesAsync();

        // Act
        var result = await _customerService.SearchCustomersAsync("John");

        // Assert
        result.Should().HaveCount(2); // John Smith and Bob Johnson
        result.Should().Contain(c => c.FirstName == "John");
        result.Should().Contain(c => c.LastName == "Johnson");
    }

    [Test]
    public async Task SearchCustomersAsync_WithEmptySearchTerm_ShouldReturnAllCustomers()
    {
        // Arrange
        var customers = CreateTestCustomers(3);
        _context.Customers.AddRange(customers);
        await _context.SaveChangesAsync();

        // Act
        var result = await _customerService.SearchCustomersAsync("");

        // Assert
        result.Should().HaveCount(3);
    }

    [Test]
    public async Task SearchCustomersAsync_WithEmailSearch_ShouldReturnMatchingCustomers()
    {
        // Arrange
        var customers = new List<Customer>
        {
            CreateTestCustomer("John", "Smith", "john.smith@gmail.com"),
            CreateTestCustomer("Jane", "Doe", "jane.doe@yahoo.com"),
            CreateTestCustomer("Bob", "Johnson", "bob@gmail.com")
        };

        _context.Customers.AddRange(customers);
        await _context.SaveChangesAsync();

        // Act
        var result = await _customerService.SearchCustomersAsync("gmail");

        // Assert
        result.Should().HaveCount(2); // John and Bob with gmail addresses
        result.Should().Contain(c => c.Email!.Contains("gmail"));
    }

    [Test]
    public async Task SearchCustomersAsync_WithPhoneSearch_ShouldReturnMatchingCustomers()
    {
        // Arrange
        var customers = new List<Customer>
        {
            CreateTestCustomer("John", "Smith", phone: "123-456-7890"),
            CreateTestCustomer("Jane", "Doe", phone: "987-654-3210"),
            CreateTestCustomer("Bob", "Johnson", phone: "123-999-8888")
        };

        _context.Customers.AddRange(customers);
        await _context.SaveChangesAsync();

        // Act
        var result = await _customerService.SearchCustomersAsync("123");

        // Assert
        result.Should().HaveCount(2); // John and Bob with 123 in phone
        result.Should().Contain(c => c.PhoneNumber.Contains("123"));
    }

    [Test]
    public async Task GetCustomerVehiclesAsync_WithValidCustomerId_ShouldReturnVehiclesOrderedByYear()
    {
        // Arrange
        var customer = CreateTestCustomer();
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var vehicles = new List<Vehicle>
        {
            CreateTestVehicle(customer.Id, "Toyota", "Camry", 2018),
            CreateTestVehicle(customer.Id, "Honda", "Civic", 2022),
            CreateTestVehicle(customer.Id, "Ford", "Focus", 2020)
        };

        _context.Vehicles.AddRange(vehicles);
        await _context.SaveChangesAsync();

        // Act
        var result = await _customerService.GetCustomerVehiclesAsync(customer.Id);

        // Assert
        result.Should().HaveCount(3);
        result[0].Year.Should().Be(2022); // Newest first
        result[1].Year.Should().Be(2020);
        result[2].Year.Should().Be(2018);
    }

    [Test]
    public async Task GetCustomerVehiclesAsync_WithInvalidCustomerId_ShouldReturnEmptyList()
    {
        // Arrange
        var invalidCustomerId = 99999;

        // Act
        var result = await _customerService.GetCustomerVehiclesAsync(invalidCustomerId);

        // Assert
        result.Should().BeEmpty();
    }

    // Helper methods
    private Customer CreateTestCustomer(string firstName = "Test", string lastName = "Customer", string? email = null, string phone = "123-456-7890")
    {
        return new Customer
        {
            Id = GenerateId(),
            FirstName = firstName,
            LastName = lastName,
            Email = email ?? $"{firstName.ToLower()}.{lastName.ToLower()}@test.com",
            PhoneNumber = phone,
            Vehicles = new List<Vehicle>()
        };
    }

    private Customer CreateTestCustomerWithoutId()
    {
        return new Customer
        {
            // Id will be auto-generated by EF
            FirstName = "New",
            LastName = "Customer",
            Email = "new.customer@test.com",
            PhoneNumber = "123-456-7890",
            Vehicles = new List<Vehicle>()
        };
    }

    private Vehicle CreateTestVehicle(int customerId, string make = "Toyota", string model = "Camry", int year = 2020)
    {
        return new Vehicle
        {
            Id = GenerateId(),
            CustomerId = customerId,
            Make = make,
            Model = model,
            Year = year,
            RegistrationNumber = $"ABC{new Random().Next(100, 999)}",
            VIN = $"VIN{Guid.NewGuid().ToString()[..10].ToUpper()}",
            ServiceOrders = new List<ServiceOrder>()
        };
    }

    private List<Customer> CreateTestCustomers(int count)
    {
        var customers = new List<Customer>();
        for (int i = 0; i < count; i++)
        {
            customers.Add(CreateTestCustomer($"Customer{i}", $"LastName{i}"));
        }
        return customers;
    }

    private int GenerateId()
    {
        return new Random().Next(1, 10000);
    }
}