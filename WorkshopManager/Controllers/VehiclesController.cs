using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WorkshopManager.DTOs;
using WorkshopManager.Mappers;
using WorkshopManager.Models;
using WorkshopManager.Services.Interfaces;

namespace WorkshopManager.Controllers;

[Authorize(Roles = "Admin,Receptionist")]
public class VehiclesController : Controller
{
    private readonly IVehicleService _vehicleService;
    private readonly ICustomerService _customerService;
    private readonly VehicleMapper _mapper;

    public VehiclesController(
        IVehicleService vehicleService,
        ICustomerService customerService,
        VehicleMapper mapper)
    {
        _vehicleService = vehicleService;
        _customerService = customerService;
        _mapper = mapper;
    }

    // GET: Vehicles
    public async Task<IActionResult> Index()
    {
        var vehicles = await _vehicleService.GetAllVehiclesAsync();
        var vehicleDtos = _mapper.VehiclesToDto(vehicles);
        return View(vehicleDtos);
    }

    // GET: Vehicles/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var vehicle = await _vehicleService.GetVehicleByIdAsync(id.Value);
        if (vehicle == null)
        {
            return NotFound();
        }

        var vehicleDto = _mapper.VehicleToDto(vehicle);
        return View(vehicleDto);
    }

    // GET: Vehicles/Create
    public async Task<IActionResult> Create(int? customerId)
    {
        await PopulateCustomersDropDownList(customerId);
        return View();
    }

    // POST: Vehicles/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VehicleDto vehicleDto)
    {
        if (ModelState.IsValid)
        {
            var vehicle = _mapper.DtoToVehicle(vehicleDto);

            // Obsługa uploadu zdjęcia
            if (vehicleDto.ImageFile != null)
            {
                vehicle.ImageUrl = await _vehicleService.SaveVehicleImageAsync(vehicleDto.ImageFile);
            }

            await _vehicleService.CreateVehicleAsync(vehicle);
            return RedirectToAction(nameof(Index));
        }

        await PopulateCustomersDropDownList(vehicleDto.CustomerId);
        return View(vehicleDto);
    }

    // GET: Vehicles/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var vehicle = await _vehicleService.GetVehicleByIdAsync(id.Value);
        if (vehicle == null)
        {
            return NotFound();
        }

        var vehicleDto = _mapper.VehicleToDto(vehicle);
        await PopulateCustomersDropDownList(vehicle.CustomerId);

        return View(vehicleDto);
    }

    // POST: Vehicles/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VehicleDto vehicleDto)
    {
        if (id != vehicleDto.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var vehicle = _mapper.DtoToVehicle(vehicleDto);

                // Sprawdź, czy aktualizujemy zdjęcie
                if (vehicleDto.ImageFile != null)
                {
                    vehicle.ImageUrl = await _vehicleService.SaveVehicleImageAsync(vehicleDto.ImageFile);
                }
                else
                {
                    // Zachowaj istniejące zdjęcie
                    var existingVehicle = await _vehicleService.GetVehicleByIdAsync(id);
                    if (existingVehicle != null)
                    {
                        vehicle.ImageUrl = existingVehicle.ImageUrl;
                    }
                }

                await _vehicleService.UpdateVehicleAsync(vehicle);
            }
            catch (Exception)
            {
                if (!await VehicleExists(vehicleDto.Id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        await PopulateCustomersDropDownList(vehicleDto.CustomerId);
        return View(vehicleDto);
    }

    // GET: Vehicles/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var vehicle = await _vehicleService.GetVehicleByIdAsync(id.Value);
        if (vehicle == null)
        {
            return NotFound();
        }

        var vehicleDto = _mapper.VehicleToDto(vehicle);
        return View(vehicleDto);
    }

    // POST: Vehicles/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _vehicleService.DeleteVehicleAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> VehicleExists(int id)
    {
        var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
        return vehicle != null;
    }

    private async Task PopulateCustomersDropDownList(int? selectedCustomerId = null)
    {
        var customers = await _customerService.GetAllCustomersAsync();
        ViewBag.CustomerId = new SelectList(
            customers.Select(c => new
            {
                Id = c.Id,
                FullName = $"{c.FirstName} {c.LastName}"
            }),
            "Id",
            "FullName",
            selectedCustomerId);
    }
}