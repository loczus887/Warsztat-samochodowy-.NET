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

    public CustomersController(
        ICustomerService customerService,
        CustomerMapper mapper)
    {
        _customerService = customerService;
        _mapper = mapper;
    }

    // GET: Customers
    public async Task<IActionResult> Index(string searchString)
    {
        var customers = string.IsNullOrEmpty(searchString)
            ? await _customerService.GetAllCustomersAsync()
            : await _customerService.SearchCustomersAsync(searchString);

        var customerDtos = _mapper.CustomersToDto(customers);

        ViewData["CurrentFilter"] = searchString;

        return View(customerDtos);
    }

    // GET: Customers/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _customerService.GetCustomerByIdAsync(id.Value);
        if (customer == null)
        {
            return NotFound();
        }

        var customerDto = _mapper.CustomerToDto(customer);

        // Pobierz pojazdy klienta
        var vehicles = await _customerService.GetCustomerVehiclesAsync(id.Value);
        ViewData["Vehicles"] = vehicles;

        return View(customerDto);
    }

    // GET: Customers/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Customers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerDto customerDto)
    {
        if (ModelState.IsValid)
        {
            var customer = _mapper.DtoToCustomer(customerDto);
            await _customerService.CreateCustomerAsync(customer);
            return RedirectToAction(nameof(Index));
        }
        return View(customerDto);
    }

    // GET: Customers/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _customerService.GetCustomerByIdAsync(id.Value);
        if (customer == null)
        {
            return NotFound();
        }

        var customerDto = _mapper.CustomerToDto(customer);
        return View(customerDto);
    }

    // POST: Customers/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerDto customerDto)
    {
        if (id != customerDto.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var customer = _mapper.DtoToCustomer(customerDto);
                await _customerService.UpdateCustomerAsync(customer);
            }
            catch (Exception)
            {
                if (!await CustomerExists(customerDto.Id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(customerDto);
    }

    // GET: Customers/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _customerService.GetCustomerByIdAsync(id.Value);
        if (customer == null)
        {
            return NotFound();
        }

        var customerDto = _mapper.CustomerToDto(customer);
        return View(customerDto);
    }

    // POST: Customers/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _customerService.DeleteCustomerAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> CustomerExists(int id)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id);
        return customer != null;
    }
}