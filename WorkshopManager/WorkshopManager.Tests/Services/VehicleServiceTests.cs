using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using WorkshopManager.Data;
using WorkshopManager.Models;
using WorkshopManager.Services;

namespace WorkshopManager.Tests.Services;

[TestFixture]
public class VehicleServiceTests
{
    private ApplicationDbContext _context;
    private VehicleService _vehicleService;
    private Mock<IWebHostEnvironment> _mockEnvironment;
    private Fixture _fixture;
    private string _testUploadsPath;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        // Setup mock environment
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _testUploadsPath = Path.Combine(Path.GetTempPath(), "test_uploads");
        _mockEnvironment.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());

        _vehicleService = new VehicleService(_context, _mockEnvironment.Object);
        _fixture = new Fixture();

        // Configure AutoFixture
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Create test uploads directory
        if (!Directory.Exists(_testUploadsPath))
        {
            Directory.CreateDirectory(_testUploadsPath);
        }
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();

        // Clean up test uploads directory
        if (Directory.Exists(_testUploadsPath))
        {
            Directory.Delete(_testUploadsPath, true);
        }
    }

    [Test]
    public async Task GetAllVehiclesAsync_ShouldReturnAllVehiclesOrderedByMakeAndModel()
    {
        // Arrange
        var customer = CreateTestCustomer();
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var vehicles = new List<Vehicle>
        {
            CreateTestVehicle(customer.Id, "Toyota", "Camry"),
            CreateTestVehicle(customer.Id, "BMW", "X3"),
            CreateTestVehicle(customer.Id, "Toyota", "Corolla")
        };

        _context.Vehicles.AddRange(vehicles);
        await _context.SaveChangesAsync();

        // Act
        var result = await _vehicleService.GetAllVehiclesAsync();

        // Assert
        result.Should().HaveCount(3);
        result[0].Make.Should().Be("BMW"); // BMW first
        result[1].Make.Should().Be("Toyota");
        result[1].Model.Should().Be("Camry"); // Camry before Corolla
        result[2].Make.Should().Be("Toyota");
        result[2].Model.Should().Be("Corolla");

        // All should have Customer loaded
        result.Should().AllSatisfy(v => v.Customer.Should().NotBeNull());
    }

    [Test]
    public async Task GetVehicleByIdAsync_WithValidId_ShouldReturnVehicleWithCustomerAndServiceOrders()
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
        var result = await _vehicleService.GetVehicleByIdAsync(vehicle.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(vehicle.Id);
        result.Make.Should().Be(vehicle.Make);
        result.Model.Should().Be(vehicle.Model);
        result.VIN.Should().Be(vehicle.VIN);
        result.Customer.Should().NotBeNull();
        result.Customer!.Id.Should().Be(customer.Id);
        result.ServiceOrders.Should().HaveCount(1);
    }

    [Test]
    public async Task GetVehicleByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var invalidId = 99999;

        // Act
        var result = await _vehicleService.GetVehicleByIdAsync(invalidId);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task CreateVehicleAsync_WithValidVehicle_ShouldCreateAndReturnVehicle()
    {
        // Arrange
        var customer = CreateTestCustomer();
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var vehicle = CreateTestVehicleWithoutId(customer.Id);

        // Act
        var result = await _vehicleService.CreateVehicleAsync(vehicle);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Make.Should().Be(vehicle.Make);
        result.Model.Should().Be(vehicle.Model);
        result.VIN.Should().Be(vehicle.VIN);
        result.RegistrationNumber.Should().Be(vehicle.RegistrationNumber);
        result.CustomerId.Should().Be(customer.Id);

        // Verify in database
        var dbVehicle = await _context.Vehicles.FindAsync(result.Id);
        dbVehicle.Should().NotBeNull();
    }

    [Test]
    public async Task UpdateVehicleAsync_WithValidVehicle_ShouldUpdateVehicle()
    {
        // Arrange
        var customer = CreateTestCustomer();
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var vehicle = CreateTestVehicle(customer.Id);
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        var updatedMake = "UpdatedMake";
        var updatedModel = "UpdatedModel";
        var updatedYear = 2023;
        vehicle.Make = updatedMake;
        vehicle.Model = updatedModel;
        vehicle.Year = updatedYear;

        // Act
        await _vehicleService.UpdateVehicleAsync(vehicle);

        // Assert - Verify in database
        var dbVehicle = await _context.Vehicles.FindAsync(vehicle.Id);
        dbVehicle.Should().NotBeNull();
        dbVehicle!.Make.Should().Be(updatedMake);
        dbVehicle.Model.Should().Be(updatedModel);
        dbVehicle.Year.Should().Be(updatedYear);
    }

    [Test]
    public async Task UpdateVehicleAsync_WithNonExistentVehicle_ShouldThrowException()
    {
        // Arrange
        var customer = CreateTestCustomer();
        var nonExistentVehicle = CreateTestVehicle(customer.Id);
        nonExistentVehicle.Id = 99999; // Non-existent ID

        // Act & Assert
        var act = async () => await _vehicleService.UpdateVehicleAsync(nonExistentVehicle);
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Test]
    public async Task DeleteVehicleAsync_WithValidId_ShouldDeleteVehicle()
    {
        // Arrange
        var customer = CreateTestCustomer();
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var vehicle = CreateTestVehicle(customer.Id);
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        // Act
        await _vehicleService.DeleteVehicleAsync(vehicle.Id);

        // Assert - Verify deletion
        var dbVehicle = await _context.Vehicles.FindAsync(vehicle.Id);
        dbVehicle.Should().BeNull();
    }

    [Test]
    public async Task DeleteVehicleAsync_WithInvalidId_ShouldNotThrowException()
    {
        // Arrange
        var invalidId = 99999;

        // Act & Assert - Should not throw exception
        var act = async () => await _vehicleService.DeleteVehicleAsync(invalidId);
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task DeleteVehicleAsync_WithServiceOrders_ShouldDeleteVehicleAndCascadeServiceOrders()
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
        await _vehicleService.DeleteVehicleAsync(vehicle.Id);

        // Assert - Both vehicle and service order should be deleted (cascade)
        var dbVehicle = await _context.Vehicles.FindAsync(vehicle.Id);
        var dbServiceOrder = await _context.ServiceOrders.FindAsync(serviceOrder.Id);

        dbVehicle.Should().BeNull();
        dbServiceOrder.Should().BeNull(); // Assuming cascade delete is configured
    }

    [Test]
    public async Task SaveVehicleImageAsync_WithValidFile_ShouldSaveFileAndReturnPath()
    {
        // Arrange
        var fileName = "test-image.jpg";
        var fileContent = "fake image content";
        var fileBytes = Encoding.UTF8.GetBytes(fileContent);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(fileBytes.Length);
        mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
            .Returns((Stream stream, CancellationToken token) =>
            {
                stream.Write(fileBytes, 0, fileBytes.Length);
                return Task.CompletedTask;
            });

        // Act
        var result = await _vehicleService.SaveVehicleImageAsync(mockFile.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().StartWith("/uploads/");
        result.Should().EndWith("_" + fileName);

        // Verify GUID prefix is added
        var pathParts = result.Split('_');
        pathParts.Should().HaveCountGreaterThan(1);

        // Verify the file path format
        result.Should().MatchRegex(@"^/uploads/[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}_test-image\.jpg$");
    }

    [Test]
    public async Task SaveVehicleImageAsync_ShouldCreateUploadsDirectoryIfNotExists()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), "test_new_uploads");
        if (Directory.Exists(tempPath))
        {
            Directory.Delete(tempPath, true);
        }

        _mockEnvironment.Setup(e => e.WebRootPath).Returns(tempPath);
        var vehicleService = new VehicleService(_context, _mockEnvironment.Object);

        var fileName = "test.jpg";
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(100);
        mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await vehicleService.SaveVehicleImageAsync(mockFile.Object);

        // Assert
        var uploadsDir = Path.Combine(tempPath, "uploads");
        Directory.Exists(uploadsDir).Should().BeTrue();
        result.Should().StartWith("/uploads/");

        // Cleanup
        if (Directory.Exists(tempPath))
        {
            Directory.Delete(tempPath, true);
        }
    }

    [Test]
    public async Task SaveVehicleImageAsync_WithLargeFile_ShouldHandleCorrectly()
    {
        // Arrange
        var fileName = "large-image.jpg";
        var largeContent = new string('x', 1024 * 1024); // 1MB of 'x'
        var fileBytes = Encoding.UTF8.GetBytes(largeContent);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(fileBytes.Length);
        mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
            .Returns((Stream stream, CancellationToken token) =>
            {
                stream.Write(fileBytes, 0, fileBytes.Length);
                return Task.CompletedTask;
            });

        // Act
        var result = await _vehicleService.SaveVehicleImageAsync(mockFile.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().StartWith("/uploads/");
        result.Should().EndWith("_" + fileName);
    }

    [Test]
    public async Task SaveVehicleImageAsync_WithSpecialCharactersInFileName_ShouldHandleCorrectly()
    {
        // Arrange
        var fileName = "test image with spaces & special chars!.jpg";
        var fileContent = "content";
        var fileBytes = Encoding.UTF8.GetBytes(fileContent);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(fileBytes.Length);
        mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
            .Returns((Stream stream, CancellationToken token) =>
            {
                stream.Write(fileBytes, 0, fileBytes.Length);
                return Task.CompletedTask;
            });

        // Act
        var result = await _vehicleService.SaveVehicleImageAsync(mockFile.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().StartWith("/uploads/");
        result.Should().EndWith("_" + fileName);
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
            VIN = $"VIN{Guid.NewGuid().ToString()[..10].ToUpper()}1234567",
            ServiceOrders = new List<ServiceOrder>()
        };
    }

    private Vehicle CreateTestVehicleWithoutId(int customerId, string make = "Toyota", string model = "Camry")
    {
        return new Vehicle
        {
            CustomerId = customerId,
            Make = make,
            Model = model,
            Year = 2020,
            RegistrationNumber = $"NEW{new Random().Next(100, 999)}",
            VIN = $"VIN{Guid.NewGuid().ToString()[..10].ToUpper()}1234567",
            ServiceOrders = new List<ServiceOrder>()
        };
    }

    private ServiceOrder CreateTestServiceOrder(int vehicleId)
    {
        return new ServiceOrder
        {
            Id = GenerateId(),
            VehicleId = vehicleId,
            Description = "Test service order",
            Status = OrderStatus.New,
            CreatedAt = DateTime.Now,
            Tasks = new List<ServiceTask>(),
            Comments = new List<Comment>()
        };
    }

    private int GenerateId()
    {
        return new Random().Next(1, 10000);
    }
}