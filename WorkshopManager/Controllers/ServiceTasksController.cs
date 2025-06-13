using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WorkshopManager.Data;
using WorkshopManager.DTOs;
using WorkshopManager.Mappers;
using WorkshopManager.Models;
using WorkshopManager.Services.Interfaces;

namespace WorkshopManager.Controllers;

[Authorize(Roles = "Admin,Mechanic")]
public class ServiceTasksController : Controller
{
    private readonly IServiceOrderService _orderService;
    private readonly IPartService _partService;
    private readonly ServiceTaskMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ServiceTasksController> _logger;

    public ServiceTasksController(
        IServiceOrderService orderService,
        IPartService partService,
        ServiceTaskMapper mapper,
        ApplicationDbContext context,
        ILogger<ServiceTasksController> logger)
    {
        _orderService = orderService;
        _partService = partService;
        _mapper = mapper;
        _context = context;
        _logger = logger;
    }

    //METODA INDEX - Lista wszystkich zadań serwisowych
    public async Task<IActionResult> Index(string search = "")
    {
        try
        {
            var tasks = await _context.ServiceTasks
                .Include(t => t.ServiceOrder)
                    .ThenInclude(o => o.Vehicle)
                        .ThenInclude(v => v.Customer)
                .Include(t => t.UsedParts)
                    .ThenInclude(up => up.Part)
                .OrderByDescending(t => t.Id)
                .ToListAsync();

            // Wyszukiwanie
            if (!string.IsNullOrEmpty(search))
            {
                tasks = tasks.Where(t =>
                    t.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (t.ServiceOrder?.Vehicle?.Make?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                    (t.ServiceOrder?.Vehicle?.Model?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                    (t.ServiceOrder?.Vehicle?.RegistrationNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)
                ).ToList();
            }

            ViewBag.Search = search;
            ViewBag.TotalCount = tasks.Count;

            return View(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading service tasks");
            TempData["ErrorMessage"] = "Wystąpił błąd podczas ładowania zadań serwisowych.";
            return View(new List<ServiceTask>());
        }
    }

    //METODA DETAILS - Szczegóły zadania
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var task = await _context.ServiceTasks
                .Include(t => t.ServiceOrder)
                    .ThenInclude(o => o.Vehicle)
                        .ThenInclude(v => v.Customer)
                .Include(t => t.UsedParts)
                    .ThenInclude(up => up.Part)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading service task details {TaskId}", id);
            return NotFound();
        }
    }

    // GET: ServiceTasks/Create/5 (orderId)
    public async Task<IActionResult> Create(int orderId)
    {
        // Sprawdź czy zlecenie istnieje
        var order = await _orderService.GetServiceOrderByIdAsync(orderId);
        if (order == null)
        {
            return NotFound();
        }

        var taskDto = new ServiceTaskDto { ServiceOrderId = orderId };
        ViewBag.OrderDescription = order.Description;
        ViewBag.OrderId = orderId;

        return View(taskDto);
    }

    // POST: ServiceTasks/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceTaskDto taskDto)
    {
        try
        {
            if (ModelState.IsValid)
            {
                var task = _mapper.DtoToServiceTask(taskDto);
                await _orderService.AddTaskToOrderAsync(taskDto.ServiceOrderId, task);

                TempData["SuccessMessage"] = "Zadanie zostało dodane pomyślnie!";
                return RedirectToAction("Details", "ServiceOrders", new { id = taskDto.ServiceOrderId });
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Błąd podczas dodawania zadania: {ex.Message}");
        }

        // Jeśli modelState nie jest valid, pobierz ponownie dane dla widoku
        var order = await _orderService.GetServiceOrderByIdAsync(taskDto.ServiceOrderId);
        ViewBag.OrderDescription = order?.Description;
        ViewBag.OrderId = taskDto.ServiceOrderId;

        return View(taskDto);
    }

    // GET: ServiceTasks/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var task = await _context.ServiceTasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            var taskDto = _mapper.ServiceTaskToDto(task);
            ViewBag.OrderId = task.ServiceOrderId;

            return View(taskDto);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Błąd podczas ładowania zadania: {ex.Message}";
            return RedirectToAction("Index");
        }
    }

    // POST: ServiceTasks/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ServiceTaskDto taskDto)
    {
        if (id != taskDto.Id)
        {
            return NotFound();
        }

        try
        {
            if (ModelState.IsValid)
            {
                var task = await _context.ServiceTasks.FindAsync(id);
                if (task == null)
                {
                    return NotFound();
                }

                task.Description = taskDto.Description;
                task.LaborCost = taskDto.LaborCost;

                _context.Update(task);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Zadanie zostało zaktualizowane!";
                return RedirectToAction("Index");
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Błąd podczas aktualizacji zadania: {ex.Message}");
        }

        ViewBag.OrderId = taskDto.ServiceOrderId;
        return View(taskDto);
    }

    // GET: ServiceTasks/AddPart/5 (taskId)
    public async Task<IActionResult> AddPart(int taskId)
    {
        try
        {
            var task = await _context.ServiceTasks.FindAsync(taskId);
            if (task == null)
            {
                return NotFound();
            }

            var parts = await _partService.GetAllPartsAsync();
            ViewBag.PartId = new SelectList(parts, "Id", "Name");
            ViewBag.TaskId = taskId;
            ViewBag.TaskDescription = task.Description;

            return View(new UsedPartDto { ServiceTaskId = taskId });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Błąd podczas ładowania części: {ex.Message}";
            return RedirectToAction("Index");
        }
    }

    // POST: ServiceTasks/AddPart
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPart(UsedPartDto partDto)
    {
        try
        {
            if (ModelState.IsValid)
            {
                var task = await _context.ServiceTasks.FindAsync(partDto.ServiceTaskId);
                if (task == null)
                {
                    ModelState.AddModelError("", "Nie znaleziono zadania.");
                    return View(partDto);
                }

                var usedPart = new UsedPart
                {
                    PartId = partDto.PartId,
                    Quantity = partDto.Quantity,
                    ServiceTaskId = partDto.ServiceTaskId
                };

                _context.UsedParts.Add(usedPart);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Część została dodana do zadania!";
                return RedirectToAction("Details", "ServiceOrders", new { id = task.ServiceOrderId });
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Błąd podczas dodawania części: {ex.Message}");
        }

        var parts = await _partService.GetAllPartsAsync();
        ViewBag.PartId = new SelectList(parts, "Id", "Name", partDto.PartId);
        ViewBag.TaskId = partDto.ServiceTaskId;

        return View(partDto);
    }

    // DELETE: ServiceTasks/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var task = await _context.ServiceTasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            var orderId = task.ServiceOrderId;

            _context.ServiceTasks.Remove(task);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Zadanie zostało usunięte!";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Błąd podczas usuwania zadania: {ex.Message}";
            return RedirectToAction("Index");
        }
    }
}