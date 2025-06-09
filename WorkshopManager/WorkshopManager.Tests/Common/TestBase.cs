using AutoFixture;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using WorkshopManager.Data;

namespace WorkshopManager.Tests.Common;

/// <summary>
/// Base class for unit tests providing common setup and utilities
/// </summary>
public abstract class TestBase
{
    protected Fixture Fixture { get; private set; } = null!;
    protected ApplicationDbContext Context { get; private set; } = null!;

    [SetUp]
    public virtual void BaseSetUp()
    {
        // Configure AutoFixture
        Fixture = new Fixture();
        Fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => Fixture.Behaviors.Remove(b));
        Fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new ApplicationDbContext(options);
    }

    [TearDown]
    public virtual void BaseTearDown()
    {
        Context?.Dispose();
    }

    /// <summary>
    /// Creates a test customer with minimal configuration
    /// </summary>
    protected Models.Customer CreateTestCustomer()
    {
        return Fixture.Build<Models.Customer>()
            .With(c => c.Id, GenerateId())
            .With(c => c.Vehicles, new List<Models.Vehicle>())
            .Create();
    }

    /// <summary>
    /// Creates a test vehicle with minimal configuration
    /// </summary>
    protected Models.Vehicle CreateTestVehicle(int? customerId = null)
    {
        return Fixture.Build<Models.Vehicle>()
            .With(v => v.Id, GenerateId())
            .With(v => v.CustomerId, customerId ?? GenerateId())
            .With(v => v.ServiceOrders, new List<Models.ServiceOrder>())
            .Without(v => v.Customer)
            .Create();
    }

    /// <summary>
    /// Creates a test service order with minimal configuration
    /// </summary>
    protected Models.ServiceOrder CreateTestServiceOrder(int? vehicleId = null)
    {
        return Fixture.Build<Models.ServiceOrder>()
            .With(so => so.Id, GenerateId())
            .With(so => so.VehicleId, vehicleId ?? GenerateId())
            .With(so => so.Status, Models.OrderStatus.New)
            .With(so => so.Tasks, new List<Models.ServiceTask>())
            .With(so => so.Comments, new List<Models.Comment>())
            .Without(so => so.Vehicle)
            .Without(so => so.Mechanic)
            .Without(so => so.MechanicId)
            .Create();
    }

    /// <summary>
    /// Generates a random ID for testing
    /// </summary>
    protected int GenerateId()
    {
        return new Random().Next(1, 10000);
    }

    /// <summary>
    /// Seeds the test database with sample data
    /// </summary>
    protected async Task SeedTestDataAsync()
    {
        var customer = CreateTestCustomer();
        Context.Customers.Add(customer);
        await Context.SaveChangesAsync();

        var vehicle = CreateTestVehicle(customer.Id);
        Context.Vehicles.Add(vehicle);
        await Context.SaveChangesAsync();

        var serviceOrder = CreateTestServiceOrder(vehicle.Id);
        Context.ServiceOrders.Add(serviceOrder);
        await Context.SaveChangesAsync();
    }
}