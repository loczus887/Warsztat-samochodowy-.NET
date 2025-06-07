using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WorkshopManager.DTOs;
using WorkshopManager.Mappers;
using WorkshopManager.Models;
using WorkshopManager.Services.Interfaces;

namespace WorkshopManager.Controllers;

[Authorize]
public class ServiceOrdersController : Controller
{
    private readonly IServiceOrderService _orderService;
    private readonly IVehicleService _vehicleService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ServiceOrderMapper _mapper;
    private readonly ILogger<ServiceOrdersController> _logger;

    public ServiceOrdersController(
        IServiceOrderService orderService,
        IVehicleService vehicleService,
        UserManager<ApplicationUser> userManager,
        ServiceOrderMapper mapper,
        ILogger<ServiceOrdersController> logger)
    {
        _orderService = orderService;
        _vehicleService = vehicleService;
        _userManager = userManager;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IActionResult> Index(
        string search,
        OrderStatus? status,
        string mechanicId,
        DateTime? dateFrom,
        DateTime? dateTo,
        string sortBy = "CreatedAt")
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            var orders = new List<ServiceOrder>();

            if (User.IsInRole("Mechanic"))
            {
                orders = await _orderService.GetServiceOrdersByMechanicAsync(user.Id);
            }
            else
            {
                orders = await _orderService.GetAllServiceOrdersAsync();
            }

            var orderDtos = orders.Select(o => _mapper.ServiceOrderToDtoWithDetails(o)).ToList();

            if (!string.IsNullOrEmpty(search))
            {
                orderDtos = orderDtos.Where(o =>
                    o.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(o.VehicleInfo) && o.VehicleInfo.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(o.CustomerName) && o.CustomerName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(o.MechanicName) && o.MechanicName.Contains(search, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            if (status.HasValue)
            {
                orderDtos = orderDtos.Where(o => o.Status == status.Value).ToList();
            }

            if (!string.IsNullOrEmpty(mechanicId))
            {
                orderDtos = orderDtos.Where(o => o.MechanicId == mechanicId).ToList();
            }

            if (dateFrom.HasValue)
            {
                orderDtos = orderDtos.Where(o => o.CreatedAt.Date >= dateFrom.Value.Date).ToList();
            }

            if (dateTo.HasValue)
            {
                orderDtos = orderDtos.Where(o => o.CreatedAt.Date <= dateTo.Value.Date).ToList();
            }

            orderDtos = sortBy switch
            {
                "Status" => orderDtos.OrderBy(o => o.Status).ToList(),
                "VehicleInfo" => orderDtos.OrderBy(o => o.VehicleInfo).ToList(),
                "CustomerName" => orderDtos.OrderBy(o => o.CustomerName).ToList(),
                "MechanicName" => orderDtos.OrderBy(o => o.MechanicName).ToList(),
                "TotalCost" => orderDtos.OrderByDescending(o => o.TotalCost).ToList(),
                "CompletedAt" => orderDtos.OrderByDescending(o => o.CompletedAt).ToList(),
                _ => orderDtos.OrderByDescending(o => o.CreatedAt).ToList()
            };

            await LoadFilterOptions();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.MechanicId = mechanicId;
            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
            ViewBag.SortBy = sortBy;
            ViewBag.TotalCount = orderDtos.Count();
            ViewBag.CurrentStatus = status;

            return View(orderDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading service orders");
            TempData["ErrorMessage"] = "Wystąpił błąd podczas ładowania zleceń.";
            return View(new List<ServiceOrderDto>());
        }
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            var order = await _orderService.GetServiceOrderByIdAsync(id.Value);
            if (order == null)
            {
                return NotFound();
            }

            var orderDto = _mapper.ServiceOrderToDtoWithDetails(order);
            return View(orderDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading service order {OrderId}", id);
            return NotFound();
        }
    }

    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Create()
    {
        await PopulateDropDownLists();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Create(ServiceOrderDto orderDto)
    {
        try
        {
            if (ModelState.IsValid)
            {
                var order = _mapper.DtoToServiceOrder(orderDto);
                order.CreatedAt = DateTime.Now;
                order.Status = OrderStatus.New;

                await _orderService.CreateServiceOrderAsync(order);
                TempData["SuccessMessage"] = "Zlecenie zostało pomyślnie utworzone.";
                return RedirectToAction(nameof(Index));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating service order");
            ModelState.AddModelError("", "Wystąpił błąd podczas tworzenia zlecenia.");
        }

        await PopulateDropDownLists(orderDto.VehicleId, orderDto.MechanicId);
        return View(orderDto);
    }

    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            var order = await _orderService.GetServiceOrderByIdAsync(id.Value);
            if (order == null)
            {
                return NotFound();
            }

            var orderDto = _mapper.ServiceOrderToDtoWithDetails(order);
            await PopulateDropDownLists(order.VehicleId, order.MechanicId);

            return View(orderDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading service order {OrderId} for edit", id);
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Edit(int id, ServiceOrderDto orderDto)
    {
        if (id != orderDto.Id)
        {
            return NotFound();
        }

        try
        {
            if (ModelState.IsValid)
            {
                var order = _mapper.DtoToServiceOrder(orderDto);
                await _orderService.UpdateServiceOrderAsync(order);
                TempData["SuccessMessage"] = "Zlecenie zostało pomyślnie zaktualizowane.";
                return RedirectToAction(nameof(Index));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating service order {OrderId}", id);
            if (!await ServiceOrderExists(orderDto.Id))
            {
                return NotFound();
            }
            ModelState.AddModelError("", "Wystąpił błąd podczas aktualizacji zlecenia.");
        }

        await PopulateDropDownLists(orderDto.VehicleId, orderDto.MechanicId);
        return View(orderDto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus)
    {
        try
        {
            var order = await _orderService.GetServiceOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            if (User.IsInRole("Mechanic"))
            {
                var userId = _userManager.GetUserId(User);
                if (order.MechanicId != userId)
                {
                    return Forbid();
                }
            }

            await _orderService.UpdateOrderStatusAsync(id, newStatus);
            TempData["SuccessMessage"] = "Status zlecenia został zaktualizowany.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for service order {OrderId}", id);
            TempData["ErrorMessage"] = "Wystąpił błąd podczas aktualizacji statusu.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            var order = await _orderService.GetServiceOrderByIdAsync(id.Value);
            if (order == null)
            {
                return NotFound();
            }

            var orderDto = _mapper.ServiceOrderToDtoWithDetails(order);
            return View(orderDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading service order {OrderId} for delete", id);
            return NotFound();
        }
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _orderService.DeleteServiceOrderAsync(id);
            TempData["SuccessMessage"] = "Zlecenie zostało pomyślnie usunięte.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting service order {OrderId}", id);
            TempData["ErrorMessage"] = "Wystąpił błąd podczas usuwania zlecenia.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> ServiceOrderExists(int id)
    {
        try
        {
            var order = await _orderService.GetServiceOrderByIdAsync(id);
            return order != null;
        }
        catch
        {
            return false;
        }
    }

    private async Task LoadFilterOptions()
    {
        try
        {
            var mechanics = await _userManager.GetUsersInRoleAsync("Mechanic");
            ViewBag.Mechanics = new SelectList(
                mechanics.Select(m => new { Id = m.Id, Name = $"{m.FirstName} {m.LastName}" }),
                "Id", "Name");

            ViewBag.StatusOptions = new SelectList(
                Enum.GetValues<OrderStatus>().Select(s => new { Value = (int)s, Text = GetStatusDisplayName(s) }),
                "Value", "Text");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading filter options");
            ViewBag.Mechanics = new SelectList(new List<object>(), "Id", "Name");
            ViewBag.StatusOptions = new SelectList(new List<object>(), "Value", "Text");
        }
    }

    private async Task PopulateDropDownLists(int? vehicleId = null, string? mechanicId = null)
    {
        try
        {
            var vehicles = await _vehicleService.GetAllVehiclesAsync();
            ViewBag.VehicleId = new SelectList(
                vehicles.Select(v => new
                {
                    Id = v.Id,
                    Info = $"{v.Make} {v.Model} ({v.RegistrationNumber})"
                }),
                "Id", "Info", vehicleId);

            var mechanics = await _userManager.GetUsersInRoleAsync("Mechanic");
            ViewBag.MechanicId = new SelectList(
                mechanics.Select(m => new { Id = m.Id, Name = $"{m.FirstName} {m.LastName}" }),
                "Id", "Name", mechanicId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dropdown lists");
            ViewBag.VehicleId = new SelectList(new List<object>(), "Id", "Info");
            ViewBag.MechanicId = new SelectList(new List<object>(), "Id", "Name");
        }
    }

    private static string GetStatusDisplayName(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.New => "Nowe",
            OrderStatus.InProgress => "W trakcie",
            OrderStatus.Completed => "Ukończone",
            OrderStatus.Cancelled => "Anulowane",
            _ => status.ToString()
        };
    }
}