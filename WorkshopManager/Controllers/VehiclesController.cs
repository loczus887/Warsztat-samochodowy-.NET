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
    private readonly IFileUploadService _fileUploadService; // NOWY SERWIS
    private readonly VehicleMapper _mapper;
    private readonly ILogger<VehiclesController> _logger; // DODANE LOGOWANIE

    public VehiclesController(
        IVehicleService vehicleService,
        ICustomerService customerService,
        IFileUploadService fileUploadService, // NOWY PARAMETR
        VehicleMapper mapper,
        ILogger<VehiclesController> logger) // NOWY PARAMETR
    {
        _vehicleService = vehicleService;
        _customerService = customerService;
        _fileUploadService = fileUploadService;
        _mapper = mapper;
        _logger = logger;
    }

    // GET: Vehicles - Z WYSZUKIWANIEM
    public async Task<IActionResult> Index(string search)
    {
        try
        {
            var vehicles = await _vehicleService.GetAllVehiclesAsync();
            var vehicleDtos = _mapper.VehiclesToDto(vehicles);

            // FUNKCJONALNOŚĆ WYSZUKIWANIA
            if (!string.IsNullOrEmpty(search))
            {
                vehicleDtos = vehicleDtos.Where(v =>
                    v.Make.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    v.Model.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    v.RegistrationNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(v.VIN) && v.VIN.Contains(search, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            ViewBag.Search = search;
            return View(vehicleDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading vehicles");
            TempData["ErrorMessage"] = "Wystąpił błąd podczas ładowania pojazdów.";
            return View(new List<VehicleDto>());
        }
    }

    // GET: Vehicles/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id.Value);
            if (vehicle == null)
            {
                return NotFound();
            }

            var vehicleDto = _mapper.VehicleToDto(vehicle);
            return View(vehicleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading vehicle details {VehicleId}", id);
            return NotFound();
        }
    }

    // GET: Vehicles/Create
    public async Task<IActionResult> Create(int? customerId)
    {
        await PopulateCustomersDropDownList(customerId);
        return View();
    }

    // POST: Vehicles/Create - Z UPLOADEM ZDJĘĆ
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VehicleDto vehicleDto)
    {
        try
        {
            if (ModelState.IsValid)
            {
                // OBSŁUGA UPLOADU ZDJĘCIA
                if (vehicleDto.ImageFile != null)
                {
                    if (_fileUploadService.IsValidImage(vehicleDto.ImageFile))
                    {
                        vehicleDto.ImageUrl = await _fileUploadService.UploadImageAsync(vehicleDto.ImageFile, "vehicles");
                    }
                    else
                    {
                        ModelState.AddModelError("ImageFile", "Nieprawidłowy format pliku. Dozwolone: JPG, PNG, GIF (max 5MB).");
                        await PopulateCustomersDropDownList(vehicleDto.CustomerId);
                        return View(vehicleDto);
                    }
                }

                var vehicle = _mapper.DtoToVehicle(vehicleDto);
                await _vehicleService.CreateVehicleAsync(vehicle);

                TempData["SuccessMessage"] = "Pojazd został pomyślnie dodany.";
                return RedirectToAction(nameof(Index));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vehicle");
            ModelState.AddModelError("", "Wystąpił błąd podczas dodawania pojazdu.");
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

        try
        {
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id.Value);
            if (vehicle == null)
            {
                return NotFound();
            }

            var vehicleDto = _mapper.VehicleToDto(vehicle);
            await PopulateCustomersDropDownList(vehicle.CustomerId);

            return View(vehicleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading vehicle {VehicleId} for edit", id);
            return NotFound();
        }
    }

    // POST: Vehicles/Edit/5 - Z UPLOADEM ZDJĘĆ
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VehicleDto vehicleDto)
    {
        if (id != vehicleDto.Id)
        {
            return NotFound();
        }

        try
        {
            if (ModelState.IsValid)
            {
                var existingVehicle = await _vehicleService.GetVehicleByIdAsync(id);
                if (existingVehicle == null)
                {
                    return NotFound();
                }

                // OBSŁUGA NOWEGO ZDJĘCIA
                if (vehicleDto.ImageFile != null)
                {
                    if (_fileUploadService.IsValidImage(vehicleDto.ImageFile))
                    {
                        // Usuń stare zdjęcie
                        if (!string.IsNullOrEmpty(existingVehicle.ImageUrl))
                        {
                            await _fileUploadService.DeleteImageAsync(existingVehicle.ImageUrl);
                        }

                        // Upload nowego zdjęcia
                        vehicleDto.ImageUrl = await _fileUploadService.UploadImageAsync(vehicleDto.ImageFile, "vehicles");
                    }
                    else
                    {
                        ModelState.AddModelError("ImageFile", "Nieprawidłowy format pliku. Dozwolone: JPG, PNG, GIF (max 5MB).");
                        await PopulateCustomersDropDownList(vehicleDto.CustomerId);
                        return View(vehicleDto);
                    }
                }
                else
                {
                    // Zachowaj stare zdjęcie jeśli nie przesłano nowego
                    vehicleDto.ImageUrl = existingVehicle.ImageUrl;
                }

                var vehicle = _mapper.DtoToVehicle(vehicleDto);
                await _vehicleService.UpdateVehicleAsync(vehicle);

                TempData["SuccessMessage"] = "Pojazd został pomyślnie zaktualizowany.";
                return RedirectToAction(nameof(Index));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vehicle {VehicleId}", id);
            if (!await VehicleExists(vehicleDto.Id))
            {
                return NotFound();
            }
            ModelState.AddModelError("", "Wystąpił błąd podczas aktualizacji pojazdu.");
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

        try
        {
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id.Value);
            if (vehicle == null)
            {
                return NotFound();
            }

            var vehicleDto = _mapper.VehicleToDto(vehicle);
            return View(vehicleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading vehicle {VehicleId} for delete", id);
            return NotFound();
        }
    }

    // POST: Vehicles/Delete/5 - Z USUWANIEM ZDJĘĆ
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
            if (vehicle != null)
            {
                // USUŃ ZDJĘCIE Z DYSKU
                if (!string.IsNullOrEmpty(vehicle.ImageUrl))
                {
                    await _fileUploadService.DeleteImageAsync(vehicle.ImageUrl);
                }

                await _vehicleService.DeleteVehicleAsync(id);
                TempData["SuccessMessage"] = "Pojazd został pomyślnie usunięty.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting vehicle {VehicleId}", id);
            TempData["ErrorMessage"] = "Wystąpił błąd podczas usuwania pojazdu.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> VehicleExists(int id)
    {
        var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
        return vehicle != null;
    }

    private async Task PopulateCustomersDropDownList(int? selectedCustomerId = null)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customers for dropdown");
            ViewBag.CustomerId = new SelectList(new List<object>(), "Id", "FullName");
        }
    }
}