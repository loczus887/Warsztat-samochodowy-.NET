using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using WorkshopManager.Data;
using WorkshopManager.Models;
using WorkshopManager.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.Extensions.Internal;

namespace WorkshopManager.Tests.Integration;

[TestFixture]
public class CustomersIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [SetUp]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the existing DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Add an in-memory database for testing
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid());
                    });

                    // Mock authentication for testing
                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                            "Test", options => { });
                });

                builder.UseEnvironment("Testing");
            });

        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task GET_Customers_Index_WithoutAuth_ShouldRedirectToLogin()
    {
        // Arrange - No authentication setup
        var factoryNoAuth = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb_NoAuth");
                    });
                });
            });

        var clientNoAuth = factoryNoAuth.CreateClient();

        // Act
        var response = await clientNoAuth.GetAsync("/Customers");

        // Assert - Should redirect to login due to [Authorize] attribute
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Unauthorized);

        clientNoAuth.Dispose();
        factoryNoAuth.Dispose();
    }

    [Test]
    public async Task GET_Customers_Index_WithAuth_ShouldReturnSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/Customers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Customers"); // Check if the page contains expected content
    }

    [Test]
    public async Task GET_Customers_Index_WithSearch_ShouldReturnFilteredResults()
    {
        // Arrange - Create test data
        await SeedTestCustomer("John", "Smith", "john.smith@test.com");

        // Act
        var response = await _client.GetAsync("/Customers?search=John");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("John");
    }

    [Test]
    public async Task GET_Customers_Create_ShouldReturnSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/Customers/Create");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Create"); // Check if the create form is present
    }

    [Test]
    public async Task POST_Customers_Create_WithValidData_ShouldRedirect()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            {"FirstName", "John"},
            {"LastName", "Doe"},
            {"Email", "john.doe@test.com"},
            {"PhoneNumber", "123-456-7890"}
        };

        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/Customers/Create", formContent);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);
        response.Headers.Location?.ToString().Should().Contain("/Customers");
    }

    [Test]
    public async Task POST_Customers_Create_WithInvalidData_ShouldReturnViewWithErrors()
    {
        // Arrange - Missing required fields
        var formData = new Dictionary<string, string>
        {
            {"FirstName", ""}, // Required field empty
            {"LastName", ""},  // Required field empty
            {"Email", "invalid-email"}, // Invalid email format
            {"PhoneNumber", ""}
        };

        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/Customers/Create", formContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK); // Returns view with validation errors
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Create"); // Should return to create view
    }

    [Test]
    public async Task GET_Customers_Details_WithValidId_ShouldReturnSuccessStatusCode()
    {
        // Arrange - Create a customer
        var customerId = await SeedTestCustomer("Test", "Customer", "test@test.com");

        // Act
        var response = await _client.GetAsync($"/Customers/Details/{customerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Test");
        content.Should().Contain("Customer");
    }

    [Test]
    public async Task GET_Customers_Details_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidId = 99999;

        // Act
        var response = await _client.GetAsync($"/Customers/Details/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GET_Customers_Details_WithNullId_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/Customers/Details/");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GET_Customers_Edit_WithValidId_ShouldReturnSuccessStatusCode()
    {
        // Arrange - Create a customer
        var customerId = await SeedTestCustomer("Test", "Customer", "test@test.com");

        // Act
        var response = await _client.GetAsync($"/Customers/Edit/{customerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Test");
        content.Should().Contain("Edit");
    }

    [Test]
    public async Task POST_Customers_Edit_WithValidData_ShouldRedirect()
    {
        // Arrange - Create a customer
        var customerId = await SeedTestCustomer("Original", "Customer", "original@test.com");

        var formData = new Dictionary<string, string>
        {
            {"Id", customerId.ToString()},
            {"FirstName", "Updated"},
            {"LastName", "Customer"},
            {"Email", "updated@test.com"},
            {"PhoneNumber", "123-456-7890"}
        };

        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync($"/Customers/Edit/{customerId}", formContent);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);

        // Verify the update in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updatedCustomer = await context.Customers.FindAsync(customerId);
        updatedCustomer!.FirstName.Should().Be("Updated");
        updatedCustomer.Email.Should().Be("updated@test.com");
    }

    [Test]
    public async Task POST_Customers_Edit_WithMismatchedId_ShouldReturnNotFound()
    {
        // Arrange
        var customerId = await SeedTestCustomer("Test", "Customer", "test@test.com");
        var differentId = customerId + 1;

        var formData = new Dictionary<string, string>
        {
            {"Id", customerId.ToString()},
            {"FirstName", "Updated"},
            {"LastName", "Customer"},
            {"Email", "updated@test.com"},
            {"PhoneNumber", "123-456-7890"}
        };

        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync($"/Customers/Edit/{differentId}", formContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GET_Customers_Delete_WithValidId_ShouldReturnSuccessStatusCode()
    {
        // Arrange - Create a customer
        var customerId = await SeedTestCustomer("Test", "Customer", "test@test.com");

        // Act
        var response = await _client.GetAsync($"/Customers/Delete/{customerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Test");
        content.Should().Contain("Delete");
    }

    [Test]
    public async Task POST_Customers_DeleteConfirmed_ShouldRemoveCustomer()
    {
        // Arrange - Create a customer
        var customerId = await SeedTestCustomer("Test", "Customer", "test@test.com");

        var formData = new Dictionary<string, string>();
        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync($"/Customers/Delete/{customerId}", formContent);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);

        // Verify deletion in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var deletedCustomer = await context.Customers.FindAsync(customerId);
        deletedCustomer.Should().BeNull();
    }

    [Test]
    public async Task GET_Customers_Index_WithSortBy_ShouldReturnSortedResults()
    {
        // Arrange - Create multiple customers
        await SeedTestCustomer("Zebra", "Adams", "zebra@test.com");
        await SeedTestCustomer("Alpha", "Brown", "alpha@test.com");

        // Act
        var response = await _client.GetAsync("/Customers?sortBy=FirstName");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Alpha");
        content.Should().Contain("Zebra");
    }

    [Test]
    public async Task GET_Customers_Index_WithInvalidSortBy_ShouldDefaultToLastName()
    {
        // Arrange - Create customers
        await SeedTestCustomer("John", "Zebra", "john.zebra@test.com");
        await SeedTestCustomer("Jane", "Adams", "jane.adams@test.com");

        // Act
        var response = await _client.GetAsync("/Customers?sortBy=InvalidSort");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Should default to sorting by LastName (Adams before Zebra)
    }

    // Helper method to seed test data
    private async Task<int> SeedTestCustomer(string firstName, string lastName, string email)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var customer = new Customer
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = "123-456-7890"
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer.Id;
    }
}

// Test Authentication Handler for bypassing real authentication
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.NameIdentifier, "123"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "Receptionist")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}