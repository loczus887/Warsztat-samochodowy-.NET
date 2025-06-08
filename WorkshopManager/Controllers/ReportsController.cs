using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WorkshopManager.Services.Interfaces;

namespace WorkshopManager.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;
    private readonly ICustomerService _customerService;
    private readonly IVehicleService _vehicleService;
    private readonly IEmailService _emailService; 

    public ReportsController(
        IReportService reportService,
        ICustomerService customerService,
        IVehicleService vehicleService,
        IEmailService emailService) 
    {
        _reportService = reportService;
        _customerService = customerService;
        _vehicleService = vehicleService;
        _emailService = emailService; 
    }

    // GET: Reports
    public IActionResult Index()
    {
        return View();
    }

    // GET: Reports/Customer
    public async Task<IActionResult> Customer()
    {
        var customers = await _customerService.GetAllCustomersAsync();
        ViewBag.Customers = new SelectList(
            customers.Select(c => new
            {
                Id = c.Id,
                Name = $"{c.FirstName} {c.LastName}"
            }),
            "Id",
            "Name");

        return View();
    }

    // POST: Reports/CustomerReport
    [HttpPost]
    public async Task<IActionResult> CustomerReport(int customerId, DateTime? startDate, DateTime? endDate)
    {
        try
        {
            var reportData = await _reportService.GenerateCustomerReportAsync(customerId, startDate, endDate);

            var customer = await _customerService.GetCustomerByIdAsync(customerId);
            var fileName = $"raport_klienta_{customer.LastName}_{customer.FirstName}_{DateTime.Now:yyyyMMdd}.pdf";
 
            return File(reportData, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Błąd generowania raportu: {ex.Message}";
            return RedirectToAction(nameof(Customer));
        }
    }

    // GET: Reports/Vehicle
    public async Task<IActionResult> Vehicle()
    {
        var vehicles = await _vehicleService.GetAllVehiclesAsync();
        ViewBag.Vehicles = new SelectList(
            vehicles.Select(v => new
            {
                Id = v.Id,
                Info = $"{v.Make} {v.Model} ({v.RegistrationNumber}) - {v.Customer?.FirstName} {v.Customer?.LastName}"
            }),
            "Id",
            "Info");

        return View();
    }

    // POST: Reports/VehicleReport
    [HttpPost]
    public async Task<IActionResult> VehicleReport(int vehicleId, DateTime? startDate, DateTime? endDate)
    {
        try
        {
            var reportData = await _reportService.GenerateVehicleReportAsync(vehicleId, startDate, endDate);

            var vehicle = await _vehicleService.GetVehicleByIdAsync(vehicleId);
            var fileName = $"raport_pojazdu_{vehicle.Make}_{vehicle.Model}_{vehicle.RegistrationNumber}_{DateTime.Now:yyyyMMdd}.pdf";

            return File(reportData, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Błąd generowania raportu: {ex.Message}";
            return RedirectToAction(nameof(Vehicle));
        }
    }

    // GET: Reports/Monthly
    [Authorize(Roles = "Admin")]
    public IActionResult Monthly()
    {
        ViewBag.CurrentMonth = DateTime.Now.Month;
        ViewBag.CurrentYear = DateTime.Now.Year;

        return View();
    }

    // POST: Reports/MonthlyReport
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> MonthlyReport(int month, int year)
    {
        try
        {
            var reportData = await _reportService.GenerateMonthlyReportAsync(month, year);

            var monthName = new DateTime(year, month, 1).ToString("MMMM");
            var fileName = $"raport_miesięczny_{monthName}_{year}.pdf";

            return File(reportData, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Błąd generowania raportu: {ex.Message}";
            return RedirectToAction(nameof(Monthly));
        }
    }

    // GET: Reports/ActiveOrders
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> ActiveOrders()
    {
        var reportData = await _reportService.GenerateActiveOrdersReportAsync();
        return File(reportData, "application/pdf", $"aktywne_zlecenia_{DateTime.Now:yyyyMMdd}.pdf");
    }

    // ============ NOWE AKCJE EMAIL ============

    // GET: Reports/EmailCustomerReport
    public async Task<IActionResult> EmailCustomerReport()
    {
        var customers = await _customerService.GetAllCustomersAsync();
        ViewBag.Customers = new SelectList(
            customers.Select(c => new
            {
                Id = c.Id,
                Name = $"{c.FirstName} {c.LastName}"
            }),
            "Id",
            "Name");

        return View();
    }

    // POST: Reports/SendCustomerReportEmail
    [HttpPost]
    public async Task<IActionResult> SendCustomerReportEmail(int customerId, DateTime? startDate, DateTime? endDate, string emailTo)
    {
        try
        {
            var reportData = await _reportService.GenerateCustomerReportAsync(customerId, startDate, endDate);
            var customer = await _customerService.GetCustomerByIdAsync(customerId);

            var subject = $"Raport klienta - {customer.FirstName} {customer.LastName}";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Raport klienta</h2>
                    <p>W załączniku przesyłamy raport dla klienta: <strong>{customer.FirstName} {customer.LastName}</strong></p>
                    {(startDate.HasValue && endDate.HasValue ? $"<p>Okres: {startDate.Value:dd.MM.yyyy} - {endDate.Value:dd.MM.yyyy}</p>" : "")}
                    <p>Pozdrawiamy,<br/>WorkshopManager</p>
                </body>
                </html>";

            var fileName = $"raport_klienta_{customer.LastName}_{customer.FirstName}_{DateTime.Now:yyyyMMdd}.pdf";

            await _emailService.SendEmailAsync(emailTo, subject, body, reportData, fileName);

            TempData["SuccessMessage"] = $"Raport został wysłany na adres: {emailTo}";
            return RedirectToAction(nameof(EmailCustomerReport));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Błąd wysyłania raportu: {ex.Message}";
            return RedirectToAction(nameof(EmailCustomerReport));
        }
    }

    // GET: Reports/EmailVehicleReport
    public async Task<IActionResult> EmailVehicleReport()
    {
        var vehicles = await _vehicleService.GetAllVehiclesAsync();
        ViewBag.Vehicles = new SelectList(
            vehicles.Select(v => new
            {
                Id = v.Id,
                Info = $"{v.Make} {v.Model} ({v.RegistrationNumber}) - {v.Customer?.FirstName} {v.Customer?.LastName}"
            }),
            "Id",
            "Info");

        return View();
    }

    // POST: Reports/SendVehicleReportEmail
    [HttpPost]
    public async Task<IActionResult> SendVehicleReportEmail(int vehicleId, DateTime? startDate, DateTime? endDate, string emailTo)
    {
        try
        {
            var reportData = await _reportService.GenerateVehicleReportAsync(vehicleId, startDate, endDate);
            var vehicle = await _vehicleService.GetVehicleByIdAsync(vehicleId);

            var subject = $"Raport pojazdu - {vehicle.Make} {vehicle.Model} ({vehicle.RegistrationNumber})";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Raport pojazdu</h2>
                    <p>W załączniku przesyłamy raport dla pojazdu: <strong>{vehicle.Make} {vehicle.Model} ({vehicle.RegistrationNumber})</strong></p>
                    {(startDate.HasValue && endDate.HasValue ? $"<p>Okres: {startDate.Value:dd.MM.yyyy} - {endDate.Value:dd.MM.yyyy}</p>" : "")}
                    <p>Pozdrawiamy,<br/>WorkshopManager</p>
                </body>
                </html>";

            var fileName = $"raport_pojazdu_{vehicle.Make}_{vehicle.Model}_{vehicle.RegistrationNumber}_{DateTime.Now:yyyyMMdd}.pdf";

            await _emailService.SendEmailAsync(emailTo, subject, body, reportData, fileName);

            TempData["SuccessMessage"] = $"Raport został wysłany na adres: {emailTo}";
            return RedirectToAction(nameof(EmailVehicleReport));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Błąd wysyłania raportu: {ex.Message}";
            return RedirectToAction(nameof(EmailVehicleReport));
        }
    }

    // GET: Reports/EmailMonthlyReport
    [Authorize(Roles = "Admin")]
    public IActionResult EmailMonthlyReport()
    {
        ViewBag.CurrentMonth = DateTime.Now.Month;
        ViewBag.CurrentYear = DateTime.Now.Year;
        return View();
    }

    // POST: Reports/SendMonthlyReportEmail
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendMonthlyReportEmail(int month, int year, string emailTo)
    {
        try
        {
            var reportData = await _reportService.GenerateMonthlyReportAsync(month, year);

            var monthName = new DateTime(year, month, 1).ToString("MMMM");
            var subject = $"Raport miesięczny - {monthName} {year}";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Raport miesięczny</h2>
                    <p>W załączniku przesyłamy raport miesięczny za <strong>{monthName} {year}</strong></p>
                    <p>Pozdrawiamy,<br/>WorkshopManager</p>
                </body>
                </html>";

            var fileName = $"raport_miesieczny_{monthName}_{year}.pdf";

            await _emailService.SendEmailAsync(emailTo, subject, body, reportData, fileName);

            TempData["SuccessMessage"] = $"Raport został wysłany na adres: {emailTo}";
            return RedirectToAction(nameof(EmailMonthlyReport));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Błąd wysyłania raportu: {ex.Message}";
            return RedirectToAction(nameof(EmailMonthlyReport));
        }
    }

    // GET: Reports/EmailActiveOrders
    [Authorize(Roles = "Admin,Receptionist")]
    public IActionResult EmailActiveOrders()
    {
        return View();
    }

    // POST: Reports/SendActiveOrdersEmail
    [HttpPost]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> SendActiveOrdersEmail(string emailTo)
    {
        try
        {
            await _reportService.SendDailyReportEmailAsync(emailTo);

            TempData["SuccessMessage"] = $"Raport aktywnych zleceń został wysłany na adres: {emailTo}";
            return RedirectToAction(nameof(EmailActiveOrders));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Błąd wysyłania raportu: {ex.Message}";
            return RedirectToAction(nameof(EmailActiveOrders));
        }
    }
}