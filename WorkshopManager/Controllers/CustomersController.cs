using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkshopManager.DTOs;
using WorkshopManager.Mappers;
using WorkshopManager.Models;
using WorkshopManager.Services.Interfaces;

namespace WorkshopManager.Controllers;

[Authorize(Roles = "Admin,Receptionist")]
public class CustomersController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly CustomerMapper _mapper;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        ICustomerService customerService,
        CustomerMapper mapper,
        ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string search, string sortBy = "LastName")
    {
        try
        {
            var customers = await _customerService.GetAllCustomersAsync();
            var customerDtos = _mapper.CustomersToDto(customers);

            if (!string.IsNullOrEmpty(search))
            {
                customerDtos = customerDtos.Where(c =>
                    c.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    c.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(c.Email) && c.Email.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    c.PhoneNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            customerDtos = sortBy switch
            {
                "FirstName" => customerDtos.OrderBy(c => c.FirstName).ToList(),
                "Email" => customerDtos.OrderBy(c => c.Email).ToList(),
                "PhoneNumber" => customerDtos.OrderBy(c => c.PhoneNumber).ToList(),
                _ => customerDtos.OrderBy(c => c.LastName).ToList()
            };

            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            ViewBag.TotalCount = customerDtos.Count();

            _logger.LogDebug("Customers list loaded successfully");
            return View(customerDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customers");
            TempData["ErrorMessage"] = "Wystąpił błąd podczas ładowania klientów.";
            return View(new List<CustomerDto>());
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
            var customer = await _customerService.GetCustomerByIdAsync(id.Value);
            if (customer == null)
            {
                _logger.LogWarning("Customer with ID {CustomerId} not found", id);
                return NotFound();
            }

            var customerDto = _mapper.CustomerToDto(customer);


            ViewData["Vehicles"] = customer.Vehicles?.ToList() ?? new List<Vehicle>();

            _logger.LogInformation("Successfully loaded customer {CustomerId} with {VehicleCount} vehicles",
                id, customer.Vehicles?.Count ?? 0);

            return View(customerDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customer {CustomerId} details. Message: {Message}", id, ex.Message);
            TempData["ErrorMessage"] = "Wystąpił błąd podczas ładowania szczegółów klienta.";
            return RedirectToAction(nameof(Index));
        }
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerDto customerDto)
    {
        try
        {
            if (ModelState.IsValid)
            {
                var customer = _mapper.DtoToCustomer(customerDto);
                await _customerService.CreateCustomerAsync(customer);
                TempData["SuccessMessage"] = "Klient został pomyślnie dodany.";
                return RedirectToAction(nameof(Index));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            ModelState.AddModelError("", "Wystąpił błąd podczas dodawania klienta.");
        }

        _logger.LogDebug("Customer created successfully");
        return View(customerDto);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            var customer = await _customerService.GetCustomerByIdAsync(id.Value);
            if (customer == null)
            {
                return NotFound();
            }

            var customerDto = _mapper.CustomerToDto(customer);
            return View(customerDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customer {CustomerId} for edit", id);
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerDto customerDto)
    {
        if (id != customerDto.Id)
        {
            return NotFound();
        }

        try
        {
            if (ModelState.IsValid)
            {
                var customer = _mapper.DtoToCustomer(customerDto);
                await _customerService.UpdateCustomerAsync(customer);
                TempData["SuccessMessage"] = "Klient został pomyślnie zaktualizowany.";
                return RedirectToAction(nameof(Index));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId}", id);
            if (!await CustomerExists(customerDto.Id))
            {
                return NotFound();
            }
            ModelState.AddModelError("", "Wystąpił błąd podczas aktualizacji klienta.");
        }

        _logger.LogDebug("Customer updated successfully");
        return View(customerDto);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            var customer = await _customerService.GetCustomerByIdAsync(id.Value);
            if (customer == null)
            {
                return NotFound();
            }

            var customerDto = _mapper.CustomerToDto(customer);

            _logger.LogDebug("Customer deleted successfully");
            return View(customerDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customer {CustomerId} for delete", id);
            return NotFound();
        }
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _customerService.DeleteCustomerAsync(id);
            TempData["SuccessMessage"] = "Klient został pomyślnie usunięty.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer {CustomerId}", id);
            TempData["ErrorMessage"] = "Wystąpił błąd podczas usuwania klienta.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> CustomerExists(int id)
    {
        try
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            return customer != null;
        }
        catch
        {
            return false;
        }
    }
}