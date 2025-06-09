using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using System.Security.Claims;
using WorkshopManager.Models;

namespace WorkshopManager.Tests.Utilities;

/// <summary>
/// Utility methods for testing
/// </summary>
public static class TestUtilities
{
    /// <summary>
    /// Creates a mock HttpContext with an authenticated user
    /// </summary>
    public static HttpContext CreateMockHttpContext(string userId = "test-user-id", string role = "Admin")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(principal);

        return mockHttpContext.Object;
    }

    /// <summary>
    /// Creates a controller context with authenticated user
    /// </summary>
    public static ControllerContext CreateControllerContext(string userId = "test-user-id", string role = "Admin")
    {
        var httpContext = CreateMockHttpContext(userId, role);

        return new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    /// <summary>
    /// Adds model state errors to a controller for testing validation scenarios
    /// </summary>
    public static void AddModelStateErrors(Controller controller, params (string key, string error)[] errors)
    {
        foreach (var (key, error) in errors)
        {
            controller.ModelState.AddModelError(key, error);
        }
    }

    /// <summary>
    /// Creates a TempData provider for testing
    /// </summary>
    public static ITempDataProvider CreateTempDataProvider()
    {
        return new Mock<ITempDataProvider>().Object;
    }

    /// <summary>
    /// Creates test customer data with specified properties
    /// </summary>
    public static Customer CreateCustomer(
        string firstName = "Test",
        string lastName = "Customer",
        string email = "test@example.com",
        string phoneNumber = "123-456-7890")
    {
        return new Customer
        {
            Id = GenerateId(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = phoneNumber,
            Vehicles = new List<Vehicle>()
        };
    }

    /// <summary>
    /// Creates test vehicle data with specified properties
    /// </summary>
    public static Vehicle CreateVehicle(
        int customerId,
        string make = "Toyota",
        string model = "Camry",
        int year = 2020,
        string registrationNumber = "ABC123",
        string vin = "1HGCM82633A123456")
    {
        return new Vehicle
        {
            Id = GenerateId(),
            CustomerId = customerId,
            Make = make,
            Model = model,
            Year = year,
            RegistrationNumber = registrationNumber,
            VIN = vin,
            ServiceOrders = new List<ServiceOrder>()
        };
    }

    /// <summary>
    /// Creates test service order data with specified properties
    /// </summary>
    public static ServiceOrder CreateServiceOrder(
        int vehicleId,
        string description = "Test service order",
        OrderStatus status = OrderStatus.New)
    {
        return new ServiceOrder
        {
            Id = GenerateId(),
            VehicleId = vehicleId,
            Description = description,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            Tasks = new List<ServiceTask>(),
            Comments = new List<Comment>()
        };
    }

    /// <summary>
    /// Creates test part data with specified properties
    /// </summary>
    public static Part CreatePart(
        string name = "Test Part",
        string category = "Test Category",
        decimal unitPrice = 10.99m)
    {
        return new Part
        {
            Id = GenerateId(),
            Name = name,
            Category = category,
            UnitPrice = unitPrice,
            UsedParts = new List<UsedPart>()
        };
    }

    /// <summary>
    /// Creates test application user data
    /// </summary>
    public static ApplicationUser CreateApplicationUser(
        string firstName = "Test",
        string lastName = "User",
        string email = "test@example.com")
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(), // User ID remains string
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            AssignedOrders = new List<ServiceOrder>(),
            Comments = new List<Comment>()
        };
    }

    /// <summary>
    /// Validates that two dates are approximately equal (within specified tolerance)
    /// </summary>
    public static bool DatesAreApproximatelyEqual(DateTime date1, DateTime date2, TimeSpan tolerance)
    {
        return Math.Abs((date1 - date2).TotalMilliseconds) <= tolerance.TotalMilliseconds;
    }

    /// <summary>
    /// Creates a list of test customers for bulk testing
    /// </summary>
    public static List<Customer> CreateCustomerList(int count = 5)
    {
        var customers = new List<Customer>();

        for (int i = 0; i < count; i++)
        {
            customers.Add(CreateCustomer(
                firstName: $"Customer{i}",
                lastName: $"LastName{i}",
                email: $"customer{i}@test.com",
                phoneNumber: $"123-456-78{i:D2}"
            ));
        }

        return customers;
    }

    /// <summary>
    /// Creates a list of test vehicles for bulk testing
    /// </summary>
    public static List<Vehicle> CreateVehicleList(int customerId, int count = 3)
    {
        var vehicles = new List<Vehicle>();
        var makes = new[] { "Toyota", "Honda", "Ford", "Chevrolet", "BMW" };
        var models = new[] { "Camry", "Accord", "F-150", "Silverado", "X3" };

        for (int i = 0; i < count; i++)
        {
            vehicles.Add(CreateVehicle(
                customerId: customerId,
                make: makes[i % makes.Length],
                model: models[i % models.Length],
                year: 2020 + i,
                registrationNumber: $"REG{i:D3}",
                vin: $"VIN{i:D17}"
            ));
        }

        return vehicles;
    }

    /// <summary>
    /// Generates a random integer ID for testing
    /// </summary>
    private static int GenerateId()
    {
        return new Random().Next(1, 10000);
    }

    /// <summary>
    /// Asserts that a redirect result points to the expected action
    /// </summary>
    public static void AssertRedirectToAction(IActionResult result, string expectedAction, string? expectedController = null)
    {
        if (result is not RedirectToActionResult redirectResult)
            throw new AssertionException($"Expected RedirectToActionResult but got {result?.GetType().Name}");

        if (redirectResult.ActionName != expectedAction)
            throw new AssertionException($"Expected redirect to action '{expectedAction}' but got '{redirectResult.ActionName}'");

        if (expectedController != null && redirectResult.ControllerName != expectedController)
            throw new AssertionException($"Expected redirect to controller '{expectedController}' but got '{redirectResult.ControllerName}'");
    }

    /// <summary>
    /// Asserts that a view result contains the expected model
    /// </summary>
    public static T AssertViewResultWithModel<T>(IActionResult result) where T : class
    {
        if (result is not ViewResult viewResult)
            throw new AssertionException($"Expected ViewResult but got {result?.GetType().Name}");

        if (viewResult.Model is not T model)
            throw new AssertionException($"Expected model of type {typeof(T).Name} but got {viewResult.Model?.GetType().Name}");

        return model;
    }

    /// <summary>
    /// Exception for test assertions
    /// </summary>
    public class AssertionException : Exception
    {
        public AssertionException(string message) : base(message) { }
    }
}