using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore; // DODANE
using WorkshopManager.Data; // DODANE
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
    private readonly ApplicationDbContext _context; // DODANE

    public ServiceOrdersController(
        IServiceOrderService orderService,
        IVehicleService vehicleService,
        UserManager<ApplicationUser> userManager,
        ServiceOrderMapper mapper,
        ILogger<ServiceOrdersController> logger,
        ApplicationDbContext context) // DODANE
    {
        _orderService = orderService;
        _vehicleService = vehicleService;
        _userManager = userManager;
        _mapper = mapper;
        _logger = logger;
        _context = context; // DODANE
    }

    public async Task<IActionResult> Index(
        string search,
        OrderStatus? status,
        string mechanicId,
        DateTime? dateFrom,
        DateTime? dateTo,
        string sortBy = "CreatedAt")
    {
        _logger.LogInformation("Rozpoczęto ładowanie listy zleceń. Parametry: search={Search}, status={Status}, mechanicId={MechanicId}, sortBy={SortBy}",
            search, status, mechanicId, sortBy);

        try
        {
            var user = await _userManager.GetUserAsync(User);
            _logger.LogInformation("Załadowano użytkownika: {UserId}, Role: {IsInMechanicRole}",
                user?.Id, User.IsInRole("Mechanic"));

            var orders = new List<ServiceOrder>();

            if (User.IsInRole("Mechanic"))
            {
                orders = await _orderService.GetServiceOrdersByMechanicAsync(user.Id);
                _logger.LogInformation("Załadowano {Count} zleceń dla mechanika {UserId}", orders.Count, user.Id);
            }
            else
            {
                orders = await _orderService.GetAllServiceOrdersAsync();
                _logger.LogInformation("Załadowano {Count} wszystkich zleceń", orders.Count);
            }

            var orderDtos = orders.Select(o => _mapper.ServiceOrderToDtoWithDetails(o)).ToList();

            // Logowanie filtrów
            if (!string.IsNullOrEmpty(search))
            {
                orderDtos = orderDtos.Where(o =>
                    o.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(o.VehicleInfo) && o.VehicleInfo.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(o.CustomerName) && o.CustomerName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(o.MechanicName) && o.MechanicName.Contains(search, StringComparison.OrdinalIgnoreCase))
                ).ToList();
                _logger.LogDebug("Zastosowano filtr wyszukiwania '{Search}', pozostało {Count} zleceń", search, orderDtos.Count);
            }

            if (status.HasValue)
            {
                orderDtos = orderDtos.Where(o => o.Status == status.Value).ToList();
                _logger.LogDebug("Zastosowano filtr statusu '{Status}', pozostało {Count} zleceń", status.Value, orderDtos.Count);
            }

            if (!string.IsNullOrEmpty(mechanicId))
            {
                orderDtos = orderDtos.Where(o => o.MechanicId == mechanicId).ToList();
                _logger.LogDebug("Zastosowano filtr mechanika '{MechanicId}', pozostało {Count} zleceń", mechanicId, orderDtos.Count);
            }

            if (dateFrom.HasValue)
            {
                orderDtos = orderDtos.Where(o => o.CreatedAt.Date >= dateFrom.Value.Date).ToList();
                _logger.LogDebug("Zastosowano filtr daty od '{DateFrom}', pozostało {Count} zleceń", dateFrom.Value, orderDtos.Count);
            }

            if (dateTo.HasValue)
            {
                orderDtos = orderDtos.Where(o => o.CreatedAt.Date <= dateTo.Value.Date).ToList();
                _logger.LogDebug("Zastosowano filtr daty do '{DateTo}', pozostało {Count} zleceń", dateTo.Value, orderDtos.Count);
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

            _logger.LogInformation("Pomyślnie załadowano {Count} zleceń dla użytkownika {UserId}", orderDtos.Count(), user?.Id);
            return View(orderDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas ładowania listy zleceń dla użytkownika {UserId}",
                _userManager.GetUserId(User));
            TempData["ErrorMessage"] = "Wystąpił błąd podczas ładowania zleceń.";
            return View(new List<ServiceOrderDto>());
        }
    }

    // ZAKTUALIZOWANA METODA Details - UPROSZCZONA
    public async Task<IActionResult> Details(int? id)
    {
        _logger.LogInformation("Rozpoczęto ładowanie szczegółów zlecenia {OrderId}", id);

        if (id == null)
        {
            _logger.LogWarning("Próba dostępu do szczegółów zlecenia bez podania ID");
            return NotFound();
        }

        try
        {
            var order = await _orderService.GetServiceOrderByIdAsync(id.Value);
            if (order == null)
            {
                _logger.LogWarning("Nie znaleziono zlecenia o ID {OrderId}", id.Value);
                return NotFound();
            }

            var orderDto = _mapper.ServiceOrderToDtoWithDetails(order);

            // DODANE: Załaduj zadania serwisowe dla tego zlecenia
            var tasks = await _context.ServiceTasks
                .Where(t => t.ServiceOrderId == id.Value)
                .ToListAsync();

            // Mapuj zadania do DTO (tylko podstawowe właściwości)
            orderDto.Tasks = tasks.Select(t => new ServiceTaskDto
            {
                Id = t.Id,
                Description = t.Description,
                LaborCost = t.LaborCost,
                ServiceOrderId = t.ServiceOrderId
            }).ToList();

            _logger.LogInformation("Pomyślnie załadowano szczegóły zlecenia {OrderId}, Status: {Status}, Zadań: {TaskCount}",
                order.Id, order.Status, orderDto.Tasks.Count);

            return View(orderDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas ładowania szczegółów zlecenia {OrderId}", id);
            return NotFound();
        }
    }

    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Create()
    {
        _logger.LogInformation("Rozpoczęto tworzenie nowego zlecenia przez użytkownika {UserId}",
            _userManager.GetUserId(User));

        await PopulateDropDownLists();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Create(ServiceOrderDto orderDto)
    {
        _logger.LogInformation("Próba utworzenia nowego zlecenia. VehicleId: {VehicleId}, MechanicId: {MechanicId}",
            orderDto.VehicleId, orderDto.MechanicId);

        try
        {
            if (ModelState.IsValid)
            {
                var order = _mapper.DtoToServiceOrder(orderDto);
                order.CreatedAt = DateTime.Now;
                order.Status = OrderStatus.New;

                await _orderService.CreateServiceOrderAsync(order);

                _logger.LogInformation("Pomyślnie utworzono zlecenie {OrderId} przez użytkownika {UserId}",
                    order.Id, _userManager.GetUserId(User));

                TempData["SuccessMessage"] = "Zlecenie zostało pomyślnie utworzone.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                _logger.LogWarning("Nieprawidłowe dane w formularzu tworzenia zlecenia. Błędy: {ModelErrors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas tworzenia zlecenia przez użytkownika {UserId}",
                _userManager.GetUserId(User));
            ModelState.AddModelError("", "Wystąpił błąd podczas tworzenia zlecenia.");
        }

        await PopulateDropDownLists(orderDto.VehicleId, orderDto.MechanicId);
        return View(orderDto);
    }

    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Edit(int? id)
    {
        _logger.LogInformation("Rozpoczęto edycję zlecenia {OrderId} przez użytkownika {UserId}",
            id, _userManager.GetUserId(User));

        if (id == null)
        {
            _logger.LogWarning("Próba edycji zlecenia bez podania ID");
            return NotFound();
        }

        try
        {
            var order = await _orderService.GetServiceOrderByIdAsync(id.Value);
            if (order == null)
            {
                _logger.LogWarning("Nie znaleziono zlecenia {OrderId} do edycji", id.Value);
                return NotFound();
            }

            var orderDto = _mapper.ServiceOrderToDtoWithDetails(order);
            await PopulateDropDownLists(order.VehicleId, order.MechanicId);

            _logger.LogInformation("Załadowano formularz edycji zlecenia {OrderId}", order.Id);
            return View(orderDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas ładowania formularza edycji zlecenia {OrderId}", id);
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Edit(int id, ServiceOrderDto orderDto)
    {
        _logger.LogInformation("Próba aktualizacji zlecenia {OrderId} przez użytkownika {UserId}",
            id, _userManager.GetUserId(User));

        if (id != orderDto.Id)
        {
            _logger.LogWarning("Niezgodność ID zlecenia: ścieżka={PathId}, formularz={FormId}", id, orderDto.Id);
            return NotFound();
        }

        try
        {
            if (ModelState.IsValid)
            {
                var order = _mapper.DtoToServiceOrder(orderDto);
                await _orderService.UpdateServiceOrderAsync(order);

                _logger.LogInformation("Pomyślnie zaktualizowano zlecenie {OrderId} przez użytkownika {UserId}",
                    id, _userManager.GetUserId(User));

                TempData["SuccessMessage"] = "Zlecenie zostało pomyślnie zaktualizowane.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                _logger.LogWarning("Nieprawidłowe dane w formularzu edycji zlecenia {OrderId}. Błędy: {ModelErrors}",
                    id, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas aktualizacji zlecenia {OrderId} przez użytkownika {UserId}",
                id, _userManager.GetUserId(User));

            if (!await ServiceOrderExists(orderDto.Id))
            {
                _logger.LogWarning("Zlecenie {OrderId} nie istnieje podczas próby aktualizacji", orderDto.Id);
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
        _logger.LogInformation("Próba zmiany statusu zlecenia {OrderId} na {NewStatus} przez użytkownika {UserId}",
            id, newStatus, _userManager.GetUserId(User));

        try
        {
            var order = await _orderService.GetServiceOrderByIdAsync(id);
            if (order == null)
            {
                _logger.LogWarning("Nie znaleziono zlecenia {OrderId} do zmiany statusu", id);
                return NotFound();
            }

            if (User.IsInRole("Mechanic"))
            {
                var userId = _userManager.GetUserId(User);
                if (order.MechanicId != userId)
                {
                    _logger.LogWarning("Mechanik {UserId} próbował zmienić status zlecenia {OrderId} przypisanego do innego mechanika {AssignedMechanicId}",
                        userId, id, order.MechanicId);
                    return Forbid();
                }
            }

            var oldStatus = order.Status;
            await _orderService.UpdateOrderStatusAsync(id, newStatus);

            _logger.LogInformation("Pomyślnie zmieniono status zlecenia {OrderId} z {OldStatus} na {NewStatus} przez użytkownika {UserId}",
                id, oldStatus, newStatus, _userManager.GetUserId(User));

            TempData["SuccessMessage"] = "Status zlecenia został zaktualizowany.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas zmiany statusu zlecenia {OrderId} na {NewStatus} przez użytkownika {UserId}",
                id, newStatus, _userManager.GetUserId(User));
            TempData["ErrorMessage"] = "Wystąpił błąd podczas aktualizacji statusu.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        _logger.LogInformation("Rozpoczęto proces usuwania zlecenia {OrderId} przez użytkownika {UserId}",
            id, _userManager.GetUserId(User));

        if (id == null)
        {
            _logger.LogWarning("Próba usunięcia zlecenia bez podania ID");
            return NotFound();
        }

        try
        {
            var order = await _orderService.GetServiceOrderByIdAsync(id.Value);
            if (order == null)
            {
                _logger.LogWarning("Nie znaleziono zlecenia {OrderId} do usunięcia", id.Value);
                return NotFound();
            }

            var orderDto = _mapper.ServiceOrderToDtoWithDetails(order);
            _logger.LogInformation("Załadowano formularz potwierdzenia usunięcia zlecenia {OrderId}", order.Id);
            return View(orderDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas ładowania formularza usunięcia zlecenia {OrderId}", id);
            return NotFound();
        }
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        _logger.LogInformation("Potwierdzenie usunięcia zlecenia {OrderId} przez użytkownika {UserId}",
            id, _userManager.GetUserId(User));

        try
        {
            await _orderService.DeleteServiceOrderAsync(id);

            _logger.LogInformation("Pomyślnie usunięto zlecenie {OrderId} przez użytkownika {UserId}",
                id, _userManager.GetUserId(User));

            TempData["SuccessMessage"] = "Zlecenie zostało pomyślnie usunięte.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas usuwania zlecenia {OrderId} przez użytkownika {UserId}",
                id, _userManager.GetUserId(User));
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas sprawdzania istnienia zlecenia {OrderId}", id);
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

            _logger.LogDebug("Załadowano opcje filtrów: {MechanicsCount} mechaników", mechanics.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas ładowania opcji filtrów");
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

            _logger.LogDebug("Załadowano dropdown listy: {VehiclesCount} pojazdów, {MechanicsCount} mechaników",
                vehicles.Count(), mechanics.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas ładowania dropdown list");
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