using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WorkshopManager.Models;
using WorkshopManager.Services.Interfaces;

namespace WorkshopManager.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IServiceOrderService _orderService;
    private readonly ICustomerService _customerService;
    private readonly IVehicleService _vehicleService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IServiceOrderService orderService,
        ICustomerService customerService,
        IVehicleService vehicleService,
        UserManager<ApplicationUser> userManager,
        ILogger<DashboardController> logger)
    {
        _orderService = orderService;
        _customerService = customerService;
        _vehicleService = vehicleService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);

            // Podstawowe statystyki dla wszystkich ról
            var activeOrders = await _orderService.GetServiceOrdersByStatusAsync(OrderStatus.InProgress);
            var newOrders = await _orderService.GetServiceOrdersByStatusAsync(OrderStatus.New);
            var completedOrders = await _orderService.GetServiceOrdersByStatusAsync(OrderStatus.Completed);

            var dashboardData = new
            {
                ActiveOrdersCount = activeOrders.Count,
                NewOrdersCount = newOrders.Count,
                CompletedOrdersCount = completedOrders.Count,
                TotalOrdersCount = activeOrders.Count + newOrders.Count + completedOrders.Count
            };

            if (await _userManager.IsInRoleAsync(user, "Admin") ||
                await _userManager.IsInRoleAsync(user, "Receptionist"))
            {
                var customers = await _customerService.GetAllCustomersAsync();
                var vehicles = await _vehicleService.GetAllVehiclesAsync();

                ViewBag.CustomersCount = customers.Count;
                ViewBag.VehiclesCount = vehicles.Count;
                ViewBag.UserRole = await _userManager.IsInRoleAsync(user, "Admin") ? "Admin" : "Receptionist";
            }
            else if (await _userManager.IsInRoleAsync(user, "Mechanic"))
            {
                var mechanicOrders = await _orderService.GetServiceOrdersByMechanicAsync(user.Id);
                ViewBag.AssignedOrdersCount = mechanicOrders.Count;
                ViewBag.UserRole = "Mechanic";
            }

            _logger.LogDebug("Dashboard initialization completed successfully");

            return View(dashboardData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading dashboard");
            TempData["ErrorMessage"] = "Wystąpił błąd podczas ładowania pulpitu nawigacyjnego.";
            return View();
        }
    }

    // NOWA METODA - Dashboard dla Pojazdów
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Vehicles()
    {
        try
        {
            var vehicles = await _vehicleService.GetAllVehiclesAsync();

            var dashboardData = new
            {
                TotalVehicles = vehicles.Count,
                VehiclesWithImages = vehicles.Count(v => !string.IsNullOrEmpty(v.ImageUrl)),
                VehiclesWithoutImages = vehicles.Count(v => string.IsNullOrEmpty(v.ImageUrl)),
                RecentVehicles = vehicles.OrderByDescending(v => v.Id).Take(5).ToList(),
                VehiclesByYear = vehicles.GroupBy(v => v.Year)
                                       .OrderByDescending(g => g.Key)
                                       .Take(10)
                                       .ToDictionary(g => g.Key, g => g.Count()),
                VehiclesByMake = vehicles.GroupBy(v => v.Make)
                                       .OrderByDescending(g => g.Count())
                                       .Take(10)
                                       .ToDictionary(g => g.Key, g => g.Count()),
                VehiclesWithActiveOrders = vehicles.Count(v => v.ServiceOrders.Any(o =>
                    o.Status == OrderStatus.New || o.Status == OrderStatus.InProgress))
            };

            _logger.LogDebug("Vehicles dashboard initialization completed successfully");

            return View(dashboardData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading vehicles dashboard");
            TempData["ErrorMessage"] = "Wystąpił błąd podczas ładowania dashboardu pojazdów.";
            return View(new
            {
                TotalVehicles = 0,
                VehiclesWithImages = 0,
                VehiclesWithoutImages = 0,
                RecentVehicles = new List<Vehicle>(),
                VehiclesByYear = new Dictionary<int, int>(),
                VehiclesByMake = new Dictionary<string, int>(),
                VehiclesWithActiveOrders = 0
            });
        }
    }


}