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

            return View(dashboardData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading dashboard");
            TempData["ErrorMessage"] = "Wystąpił błąd podczas ładowania pulpitu nawigacyjnego.";
            return View();
        }
    }
}