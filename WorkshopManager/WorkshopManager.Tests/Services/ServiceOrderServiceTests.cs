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
public class ServiceOrderServiceTests
{
    private ApplicationDbContext _context;
    private ServiceOrderService _serviceOrderService;
    private Fixture _fixture;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _serviceOrderService = new ServiceOrderService(_context);
        _fixture = new Fixture();

        // Configure AutoFixture
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
    public async Task GetAllServiceOrdersAsync_ShouldReturnAllOrdersOrderedByCreatedAtDesc()
    {
        // Arrange
        var customer = CreateTestCustomer();
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var vehicle = CreateTestVehicle(customer.Id);
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        var serviceOrders = new List<ServiceOrder>
        {
            CreateTestServiceOrder(vehicle.Id, createdAt: DateTime.Now.AddDays(-2)),
            CreateTestServiceOrder(vehicle.Id, createdAt: DateTime.Now.AddDays(-1)),
            CreateTestServiceOrder(vehicle.Id, createdAt: DateTime.Now)
        };

        _context.ServiceOrders.AddRange(serviceOrders);
        await _context.SaveChangesAsync();

        // Act
        var result = await _serviceOrderService.GetAllServiceOrdersAsync();

        // Assert
        result.Should().HaveCount(3);
        result[0].CreatedAt.Should().BeAfter(result[1].CreatedAt); // Newest first
        result[1].CreatedAt.Should().BeAfter(result[2].CreatedAt);
    }

    [Test]
    public async Task GetServiceOrderByIdAsync_WithValidId_ShouldReturnOrderWithAllRelations()
    {
        // Arrange
        var customer = CreateTestCustomer();
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var vehicle = CreateTestVehicle(customer.Id);
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        var mechanic = CreateTestUser();
        _context.Users.Add(mechanic);
        await _context.SaveChangesAsync();

        var serviceOrder = CreateTestServiceOrder(vehicle.Id, mechanic.Id);
        _context.ServiceOrders.Add(serviceOrder);
        await _context.SaveChangesAsync();

        // Act
        var result = await _serviceOrderService.GetServiceOrderByIdAsync(serviceOrder.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(serviceOrder.Id);
        result.Vehicle.Should().NotBeNull();
        result.Vehicle!.Customer.Should().NotBeNull();
        result.Mechanic.Should().NotBeNull();
    }

    [Test]
    public async Task GetServiceOrderByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var invalidId = 99999;

        // Act
        var result = await _serviceOrderService.GetServiceOrderByIdAsync(invalidId);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetServiceOrdersByStatusAsync_ShouldReturnOnlyOrdersWithSpecifiedStatus()
    {
        // Arrange
        var customer = CreateTestCustomer();
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var vehicle = CreateTestVehicle(customer.Id);
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        var serviceOrders = new List<ServiceOrder>
        {
            CreateTestServiceOrder(vehicle.Id, status: OrderStatus.New),
            CreateTestServiceOrder(vehicle.Id, status: OrderStatus.InProgress),
            CreateTestServiceOrder(vehicle.Id, status: OrderStatus.New),
            CreateTestServiceOrder(vehicle.Id, status: OrderStatus.Completed)
        };

        _context.ServiceOrders.AddRange(serviceOrders);
        await _context.SaveChangesAsync();

        // Act
        var result = await _serviceOrderService.GetServiceOrdersByStatusAsync(OrderStatus.New);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(o => o.Status.Should().Be(OrderStatus.New));
    }

    [Test]
    public async Task GetServiceOrdersByMechanicAsync_ShouldReturnOnlyMechanicOrders()
    {
        // Arrange
        var customer = CreateTestCustomer();
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var vehicle = CreateTestVehicle(customer.Id);
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        var mechanic1 = CreateTestUser("mechanic1@test.com");
        var mechanic2 = CreateTestUser("mechanic2@test.com");
        _context.Users.AddRange(mechanic1, mechanic2);
        await _context.SaveChangesAsync();

        var serviceOrders = new List<ServiceOrder>
        {
            CreateTestServiceOrder(vehicle.Id, mechanic1.Id),
            CreateTestServiceOrder(vehicle.Id, mechanic2.Id),
            CreateTestServiceOrder(vehicle.Id, mechanic1.Id),
            CreateTestServiceOrder(vehicle.Id) // No mechanic assigned
        };

        _context.ServiceOrders.AddRange(serviceOrders);
        await _context.SaveChangesAsync();

        // Act
        var result = await _serviceOrderService.GetServiceOrdersByMechanicAsync(mechanic1.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(o => o.MechanicId.Should().Be(mechanic1.Id));
    }

    [Test]
    public async Task GetServiceOrdersByVehicleAsync_ShouldReturnOnlyVehicleOrders()
    {
        // Arrange
        var customer = CreateTestCustomer();
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var vehicle1 = CreateTestVehicle(customer.Id, "Toyota", "Camry");
        var vehicle2 = CreateTestVehicle(customer.Id, "Honda", "Civic");
        _context.Vehicles.AddRange(vehicle1, vehicle2);
        await _context.SaveChangesAsync();

        var serviceOrders = new List<ServiceOrder>
        {
            CreateTestServiceOrder(vehicle1.Id),
            CreateTestServiceOrder(vehicle2.Id),
            CreateTestServiceOrder(vehicle1.Id),
        };

        _context.ServiceOrders.AddRange(serviceOrders);
        await _context.SaveChangesAsync();

        // Act
        var result = await _serviceOrderService.GetServiceOrdersByVehicleAsync(vehicle1.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(o => o.VehicleId.Should().Be(vehicle1.Id));
    }

    [Test]
    public async Task CreateServiceOrderAsync_WithValidOrder_ShouldCreateAndReturnOrder()
    {
        // Arrange
        var customer = CreateTestCustomer();
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var vehicle = CreateTestVehicle(customer.Id);
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        var serviceOrder = CreateTestServiceOrderWithoutId(vehicle.Id);

        // Act
        var result = await _serviceOrderService.CreateServiceOrderAsync(serviceOrder);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Description.Should().Be(serviceOrder.Description);
        result.VehicleId.Should().Be(vehicle.Id);
        result.Status.Should().Be(OrderStatus.New);

        // Verify in database
        var dbOrder = await _context.ServiceOrders.FindAsync(result.Id);
        dbOrder.Should().NotBeNull();
    }

    [Test]
    public async Task UpdateServiceOrderAsync_WithValidOrder_ShouldUpdateOrder()
    {
        // Arrange
        var customer = CreateTestCustomer();
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var vehicle = CreateTestVehicle(customer.Id);
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        var serviceOrder = CreateTestServiceOrder(vehicle.Id);
        _context.ServiceOrders.Add(serviceOrder);
        await _context.SaveChangesAsync();

        var updatedDescription = "Updated description";
        serviceOrder.Description = updatedDescription;

        // Act
        await _serviceOrderService.UpdateServiceOrderAsync(serviceOrder);

        // Assert - Verify in database
        var dbOrder = await _context.ServiceOrders.FindAsync(serviceOrder.Id);
        dbOrder.Should().NotBeNull();
        dbOrder!.Description.Should().Be(updatedDescription);
    }

    [Test]
    public async Task DeleteServiceOrderAsync_WithValidId_ShouldDeleteOrder()
    {
        // Arrange
        var customer = CreateTestCustomer();
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var vehicle = CreateTestVehicle(customer.Id);
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        var serviceOrder = CreateTestServiceOrder(vehicle.Id);
        _context.ServiceOrders.Add(serviceOrder);
        await _context.SaveChangesAsync();

        // Act
        await _serviceOrderService.DeleteServiceOrderAsync(serviceOrder.Id);

        // Assert - Verify deletion
        var dbOrder = await _context.ServiceOrders.FindAsync(serviceOrder.Id);
        dbOrder.Should().BeNull();
    }

    [Test]
    public async Task DeleteServiceOrderAsync_WithInvalidId_ShouldNotThrowException()
    {
        // Arrange
        var invalidId = 99999;

        // Act & Assert - Should not throw exception
        var act = async () => await _serviceOrderService.DeleteServiceOrderAsync(invalidId);
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task UpdateOrderStatusAsync_WithValidIdAndStatus_ShouldUpdateStatus()
    {
        // Arrange
        var customer = CreateTestCustomer();
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var vehicle = CreateTestVehicle(customer.Id);
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        var serviceOrder = CreateTestServiceOrder(vehicle.Id, status: OrderStatus.New);
        _context.ServiceOrders.Add(serviceOrder);
        await _context.SaveChangesAsync();

        // Act
        var result = await _serviceOrderService.UpdateOrderStatusAsync(serviceOrder.Id, OrderStatus.InProgress);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(OrderStatus.InProgress);

        // Verify in database
        var dbOrder = await _context.ServiceOrders.FindAsync(serviceOrder.Id);
        dbOrder!.Status.Should().Be(OrderStatus.InProgress);
    }

    [Test]
    public async Task UpdateOrderStatusAsync_ToCompleted_ShouldSetCompletedAt()
    {
        // Arrange
        var customer = CreateTestCustomer();
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var vehicle = CreateTestVehicle(customer.Id);
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        var serviceOrder = CreateTestServiceOrder(vehicle.Id, status: OrderStatus.InProgress);
        _context.ServiceOrders.Add(serviceOrder);
        await _context.SaveChangesAsync();

        // Act
        var result = await _serviceOrderService.UpdateOrderStatusAsync(serviceOrder.Id, OrderStatus.Completed);

        // Assert
        result.Status.Should().Be(OrderStatus.Completed);
        result.CompletedAt.Should().NotBeNull();
        result.CompletedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
    }

    [Test]
    public async Task UpdateOrderStatusAsync_WithInvalidId_ShouldThrowException()
    {
        // Arrange
        var invalidId = 99999;

        // Act & Assert
        var act = async () => await _serviceOrderService.UpdateOrderStatusAsync(invalidId, OrderStatus.InProgress);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Zlecenie nie istnieje");
    }

    [Test]
    public async Task CalculateOrderTotalAsync_WithTasksAndParts_ShouldReturnCorrectTotal()
    {
        // Arrange
        var customer = CreateTestCustomer();
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var vehicle = CreateTestVehicle(customer.Id);
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        var part = CreateTestPart("Test Part", 50.00m);
        _context.Parts.Add(part);
        await _context.SaveChangesAsync();

        var serviceOrder = CreateTestServiceOrder(vehicle.Id);
        _context.ServiceOrders.Add(serviceOrder);
        await _context.SaveChangesAsync();

        var task = CreateTestServiceTask(serviceOrder.Id, laborCost: 100.00m);
        _context.ServiceTasks.Add(task);
        await _context.SaveChangesAsync();

        var usedPart = CreateTestUsedPart(task.Id, part.Id, quantity: 2);
        _context.UsedParts.Add(usedPart);
        await _context.SaveChangesAsync();

        // Act
        var result = await _serviceOrderService.CalculateOrderTotalAsync(serviceOrder.Id);

        // Assert
        // Labor: 100.00 + Parts: 2 * 50.00 = 200.00
        result.Should().Be(200.00m);
    }

    [Test]
    public async Task CalculateOrderTotalAsync_WithInvalidId_ShouldReturnZero()
    {
        // Arrange
        var invalidId = 99999;

        // Act
        var result = await _serviceOrderService.CalculateOrderTotalAsync(invalidId);

        // Assert
        result.Should().Be(0);
    }

    [Test]
    public async Task AddTaskToOrderAsync_WithValidData_ShouldAddTaskAndReturnOrder()
    {
        // Arrange
        var customer = CreateTestCustomer();
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var vehicle = CreateTestVehicle(customer.Id);
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        var serviceOrder = CreateTestServiceOrder(vehicle.Id);
        _context.ServiceOrders.Add(serviceOrder);
        await _context.SaveChangesAsync();

        var task = CreateTestServiceTaskWithoutId(laborCost: 75.00m);

        // Act
        var result = await _serviceOrderService.AddTaskToOrderAsync(serviceOrder.Id, task);

        // Assert
        result.Should().NotBeNull();
        result.Tasks.Should().HaveCount(1);
        result.Tasks.First().ServiceOrderId.Should().Be(serviceOrder.Id);
        result.Tasks.First().LaborCost.Should().Be(75.00m);
    }

    [Test]
    public async Task AddCommentToOrderAsync_WithValidData_ShouldAddCommentAndReturnOrder()
    {
        // Arrange
        var customer = CreateTestCustomer();
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var vehicle = CreateTestVehicle(customer.Id);
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        var author = CreateTestUser();
        _context.Users.Add(author);
        await _context.SaveChangesAsync();

        var serviceOrder = CreateTestServiceOrder(vehicle.Id);
        _context.ServiceOrders.Add(serviceOrder);
        await _context.SaveChangesAsync();

        var comment = CreateTestCommentWithoutId(author.Id, "Test comment");

        // Act
        var result = await _serviceOrderService.AddCommentToOrderAsync(serviceOrder.Id, comment);

        // Assert
        result.Should().NotBeNull();
        result.Comments.Should().HaveCount(1);
        result.Comments.First().ServiceOrderId.Should().Be(serviceOrder.Id);
        result.Comments.First().Content.Should().Be("Test comment");
        result.Comments.First().CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
    }

    // Helper methods
    private Customer CreateTestCustomer()
    {
        return new Customer
        {
            Id = GenerateId(),
            FirstName = "Test",
            LastName = "Customer",
            Email = "test@test.com",
            PhoneNumber = "123-456-7890",
            Vehicles = new List<Vehicle>()
        };
    }

    private Vehicle CreateTestVehicle(int customerId, string make = "Toyota", string model = "Camry")
    {
        return new Vehicle
        {
            Id = GenerateId(),
            CustomerId = customerId,
            Make = make,
            Model = model,
            Year = 2020,
            RegistrationNumber = $"ABC{new Random().Next(100, 999)}",
            VIN = $"VIN{Guid.NewGuid().ToString()[..10].ToUpper()}1234567",
            ServiceOrders = new List<ServiceOrder>()
        };
    }

    private ApplicationUser CreateTestUser(string email = "test@test.com")
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            AssignedOrders = new List<ServiceOrder>(),
            Comments = new List<Comment>()
        };
    }

    private ServiceOrder CreateTestServiceOrder(int vehicleId, string? mechanicId = null, OrderStatus status = OrderStatus.New, DateTime? createdAt = null)
    {
        return new ServiceOrder
        {
            Id = GenerateId(),
            VehicleId = vehicleId,
            MechanicId = mechanicId,
            Description = "Test service order",
            Status = status,
            CreatedAt = createdAt ?? DateTime.Now,
            Tasks = new List<ServiceTask>(),
            Comments = new List<Comment>()
        };
    }

    private ServiceOrder CreateTestServiceOrderWithoutId(int vehicleId, string? mechanicId = null)
    {
        return new ServiceOrder
        {
            VehicleId = vehicleId,
            MechanicId = mechanicId,
            Description = "New service order",
            Status = OrderStatus.New,
            CreatedAt = DateTime.Now,
            Tasks = new List<ServiceTask>(),
            Comments = new List<Comment>()
        };
    }

    private Part CreateTestPart(string name, decimal unitPrice)
    {
        return new Part
        {
            Id = GenerateId(),
            Name = name,
            Category = "Test Category",
            UnitPrice = unitPrice,
            UsedParts = new List<UsedPart>()
        };
    }

    private ServiceTask CreateTestServiceTask(int serviceOrderId, decimal laborCost = 50.00m)
    {
        return new ServiceTask
        {
            Id = GenerateId(),
            ServiceOrderId = serviceOrderId,
            Description = "Test task",
            LaborCost = laborCost,
            UsedParts = new List<UsedPart>()
        };
    }

    private ServiceTask CreateTestServiceTaskWithoutId(decimal laborCost = 50.00m)
    {
        return new ServiceTask
        {
            Description = "New test task",
            LaborCost = laborCost,
            UsedParts = new List<UsedPart>()
        };
    }

    private UsedPart CreateTestUsedPart(int serviceTaskId, int partId, int quantity = 1)
    {
        return new UsedPart
        {
            Id = GenerateId(),
            ServiceTaskId = serviceTaskId,
            PartId = partId,
            Quantity = quantity
        };
    }

    private Comment CreateTestCommentWithoutId(string authorId, string content)
    {
        return new Comment
        {
            AuthorId = authorId,
            Content = content
        };
    }

    private int GenerateId()
    {
        return new Random().Next(1, 10000);
    }
}