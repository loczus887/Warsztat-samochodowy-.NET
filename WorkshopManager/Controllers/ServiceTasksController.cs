using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

    public ServiceTasksController(
        IServiceOrderService orderService,
        IPartService partService,
        ServiceTaskMapper mapper)
    {
        _orderService = orderService;
        _partService = partService;
        _mapper = mapper;
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

        return View(taskDto);
    }

    // POST: ServiceTasks/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceTaskDto taskDto)
    {
        if (ModelState.IsValid)
        {
            var task = _mapper.DtoToServiceTask(taskDto);
            await _orderService.AddTaskToOrderAsync(taskDto.ServiceOrderId, task);

            return RedirectToAction("Details", "ServiceOrders", new { id = taskDto.ServiceOrderId });
        }

        // Jeśli modelState nie jest valid, pobierz ponownie dane dla widoku
        var order = await _orderService.GetServiceOrderByIdAsync(taskDto.ServiceOrderId);
        ViewBag.OrderDescription = order?.Description;

        return View(taskDto);
    }

    // GET: ServiceTasks/AddPart/5 (taskId)
    public async Task<IActionResult> AddPart(int taskId)
    {
        // Na razie uproszczona implementacja - w późniejszych commitach dodamy pełną obsługę części
        var parts = await _partService.GetAllPartsAsync();
        ViewBag.PartId = new SelectList(parts, "Id", "Name");
        ViewBag.TaskId = taskId;

        return View(new UsedPartDto { ServiceTaskId = taskId });
    }

    // POST: ServiceTasks/AddPart
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPart(UsedPartDto partDto)
    {
        if (ModelState.IsValid)
        {
            // Znajdź zadanie w bazie danych
            var order = await _orderService.GetServiceOrderByIdAsync(partDto.ServiceTaskId);
            if (order != null)
            {
                var task = order.Tasks.FirstOrDefault(t => t.Id == partDto.ServiceTaskId);
                if (task != null)
                {
                    var usedPart = new UsedPart
                    {
                        PartId = partDto.PartId,
                        Quantity = partDto.Quantity,
                        ServiceTaskId = partDto.ServiceTaskId
                    };

                    // Dodaj bezpośrednio do kontekstu
                    _context.UsedParts.Add(usedPart);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("Details", "ServiceOrders", new { id = order.Id });
                }
            }
        }

        var parts = await _partService.GetAllPartsAsync();
        ViewBag.PartId = new SelectList(parts, "Id", "Name", partDto.PartId);
        ViewBag.TaskId = partDto.ServiceTaskId;

        return View(partDto);
    }

    // Tymczasowo wstrzyknij DbContext dla prostszego dostępu
    private readonly ApplicationDbContext _context;

    // Zaktualizowany konstruktor
    public ServiceTasksController(
        IServiceOrderService orderService,
        IPartService partService,
        ServiceTaskMapper mapper,
        ApplicationDbContext context)
    {
        _orderService = orderService;
        _partService = partService;
        _mapper = mapper;
        _context = context;
    }
}