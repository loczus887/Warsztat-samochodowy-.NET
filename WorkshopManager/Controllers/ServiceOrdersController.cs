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

    public ServiceOrdersController(
        IServiceOrderService orderService,
        IVehicleService vehicleService,
        UserManager<ApplicationUser> userManager,
        ServiceOrderMapper mapper)
    {
        _orderService = orderService;
        _vehicleService = vehicleService;
        _userManager = userManager;
        _mapper = mapper;
    }

    // GET: ServiceOrders
    public async Task<IActionResult> Index(OrderStatus? status)
    {
        var user = await _userManager.GetUserAsync(User);
        var orders = new List<ServiceOrder>();

        if (User.IsInRole("Mechanic"))
        {
            // Mechanik widzi tylko swoje zlecenia
            orders = await _orderService.GetServiceOrdersByMechanicAsync(user.Id);
        }
        else
        {
            // Admin i Recepcjonista widzą wszystkie zlecenia
            if (status.HasValue)
            {
                orders = await _orderService.GetServiceOrdersByStatusAsync(status.Value);
            }
            else
            {
                orders = await _orderService.GetAllServiceOrdersAsync();
            }
        }

        var orderDtos = _mapper.ServiceOrdersToDto(orders);
        ViewBag.CurrentStatus = status;

        return View(orderDtos);
    }

    // GET: ServiceOrders/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var order = await _orderService.GetServiceOrderByIdAsync(id.Value);
        if (order == null)
        {
            return NotFound();
        }

        var orderDto = _mapper.ServiceOrderToDto(order);

        return View(orderDto);
    }

    // GET: ServiceOrders/Create
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Create()
    {
        await PopulateDropDownLists();
        return View();
    }

    // POST: ServiceOrders/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Create(ServiceOrderDto orderDto)
    {
        if (ModelState.IsValid)
        {
            var order = _mapper.DtoToServiceOrder(orderDto);
            order.CreatedAt = DateTime.Now;
            order.Status = OrderStatus.New;

            await _orderService.CreateServiceOrderAsync(order);
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropDownLists(orderDto.VehicleId, orderDto.MechanicId);
        return View(orderDto);
    }

    // GET: ServiceOrders/Edit/5
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var order = await _orderService.GetServiceOrderByIdAsync(id.Value);
        if (order == null)
        {
            return NotFound();
        }

        var orderDto = _mapper.ServiceOrderToDto(order);
        await PopulateDropDownLists(order.VehicleId, order.MechanicId);

        return View(orderDto);
    }

    // POST: ServiceOrders/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Edit(int id, ServiceOrderDto orderDto)
    {
        if (id != orderDto.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var order = _mapper.DtoToServiceOrder(orderDto);
                await _orderService.UpdateServiceOrderAsync(order);
            }
            catch (Exception)
            {
                if (!await ServiceOrderExists(orderDto.Id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropDownLists(orderDto.VehicleId, orderDto.MechanicId);
        return View(orderDto);
    }

    // POST: ServiceOrders/UpdateStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus)
    {
        var order = await _orderService.GetServiceOrderByIdAsync(id);

        if (order == null)
        {
            return NotFound();
        }

        // Sprawdź uprawnienia
        if (User.IsInRole("Mechanic"))
        {
            var userId = _userManager.GetUserId(User);
            if (order.MechanicId != userId)
            {
                return Forbid(); // Tylko przypisany mechanik może zmieniać status
            }
        }

        await _orderService.UpdateOrderStatusAsync(id, newStatus);
        return RedirectToAction(nameof(Details), new { id });
    }

    // GET: ServiceOrders/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var order = await _orderService.GetServiceOrderByIdAsync(id.Value);
        if (order == null)
        {
            return NotFound();
        }

        var orderDto = _mapper.ServiceOrderToDto(order);
        return View(orderDto);
    }

    // POST: ServiceOrders/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _orderService.DeleteServiceOrderAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> ServiceOrderExists(int id)
    {
        var order = await _orderService.GetServiceOrderByIdAsync(id);
        return order != null;
    }

    private async Task PopulateDropDownLists(int? vehicleId = null, string? mechanicId = null)
    {
        // Lista pojazdów
        var vehicles = await _vehicleService.GetAllVehiclesAsync();
        ViewBag.VehicleId = new SelectList(
            vehicles.Select(v => new
            {
                Id = v.Id,
                Info = $"{v.Make} {v.Model} ({v.RegistrationNumber}) - {v.Customer?.FirstName} {v.Customer?.LastName}"
            }),
            "Id",
            "Info",
            vehicleId);

        // Lista mechaników
        var mechanics = await _userManager.GetUsersInRoleAsync("Mechanic");
        ViewBag.MechanicId = new SelectList(
            mechanics.Select(m => new
            {
                Id = m.Id,
                Name = $"{m.FirstName} {m.LastName}"
            }),
            "Id",
            "Name",
            mechanicId);
    }
}