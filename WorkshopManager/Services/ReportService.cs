using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.EntityFrameworkCore;
using WorkshopManager.Data;
using WorkshopManager.Models;
using WorkshopManager.Services.Interfaces;

namespace WorkshopManager.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly IConverter _converter;
    private readonly IWebHostEnvironment _environment;

    public ReportService(
        ApplicationDbContext context,
        IConverter converter,
        IWebHostEnvironment environment)
    {
        _context = context;
        _converter = converter;
        _environment = environment;
    }

    public async Task<byte[]> GenerateCustomerReportAsync(int customerId, DateTime? startDate, DateTime? endDate)
    {
        var customer = await _context.Customers
            .Include(c => c.Vehicles)
                .ThenInclude(v => v.ServiceOrders.Where(o =>
                    (!startDate.HasValue || o.CreatedAt >= startDate) &&
                    (!endDate.HasValue || o.CreatedAt <= endDate)))
                    .ThenInclude(o => o.Tasks)
                        .ThenInclude(t => t.UsedParts)
                            .ThenInclude(up => up.Part)
            .FirstOrDefaultAsync(c => c.Id == customerId);

        if (customer == null)
            throw new ArgumentException("Klient nie istnieje");

        var htmlContent = GenerateCustomerReportHtml(customer, startDate, endDate);
        return GeneratePdfFromHtml(htmlContent);
    }

    public async Task<byte[]> GenerateVehicleReportAsync(int vehicleId, DateTime? startDate, DateTime? endDate)
    {
        var vehicle = await _context.Vehicles
            .Include(v => v.Customer)
            .Include(v => v.ServiceOrders.Where(o =>
                (!startDate.HasValue || o.CreatedAt >= startDate) &&
                (!endDate.HasValue || o.CreatedAt <= endDate)))
                .ThenInclude(o => o.Tasks)
                    .ThenInclude(t => t.UsedParts)
                        .ThenInclude(up => up.Part)
            .FirstOrDefaultAsync(v => v.Id == vehicleId);

        if (vehicle == null)
            throw new ArgumentException("Pojazd nie istnieje");

        var htmlContent = GenerateVehicleReportHtml(vehicle, startDate, endDate);
        return GeneratePdfFromHtml(htmlContent);
    }

    public async Task<byte[]> GenerateMonthlyReportAsync(int month, int year)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var orders = await _context.ServiceOrders
            .Include(o => o.Vehicle)
                .ThenInclude(v => v.Customer)
            .Include(o => o.Mechanic)
            .Include(o => o.Tasks)
                .ThenInclude(t => t.UsedParts)
                    .ThenInclude(up => up.Part)
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        var htmlContent = GenerateMonthlyReportHtml(orders, month, year);
        return GeneratePdfFromHtml(htmlContent);
    }

    public async Task<byte[]> GenerateActiveOrdersReportAsync()
    {
        var activeOrders = await _context.ServiceOrders
            .Include(o => o.Vehicle)
                .ThenInclude(v => v.Customer)
            .Include(o => o.Mechanic)
            .Include(o => o.Tasks)
                .ThenInclude(t => t.UsedParts)
                    .ThenInclude(up => up.Part)
            .Where(o => o.Status == OrderStatus.New || o.Status == OrderStatus.InProgress)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        var htmlContent = GenerateActiveOrdersReportHtml(activeOrders);
        return GeneratePdfFromHtml(htmlContent);
    }

    public async Task SendDailyReportEmailAsync(string email)
    {
        var reportData = await GenerateActiveOrdersReportAsync();
        // Tutaj należy zaimplementować wysyłanie emaila z załącznikiem
        // Można użyć EmailService lub gotowych bibliotek jak MailKit

        // Przykład zapisu do pliku (dla testów)
        var reportsFolder = Path.Combine(_environment.ContentRootPath, "Reports");
        if (!Directory.Exists(reportsFolder))
        {
            Directory.CreateDirectory(reportsFolder);
        }

        var fileName = $"active_orders_{DateTime.Now:yyyyMMdd}.pdf";
        var filePath = Path.Combine(reportsFolder, fileName);

        await File.WriteAllBytesAsync(filePath, reportData);
    }

    #region Pomocnicze metody generowania HTML

    private string GenerateCustomerReportHtml(Customer customer, DateTime? startDate, DateTime? endDate)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset='UTF-8'>");
        sb.AppendLine("<title>Raport klienta</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 40px; }");
        sb.AppendLine("h1, h2 { color: #333366; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
        sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        sb.AppendLine("th { background-color: #f2f2f2; }");
        sb.AppendLine("tr:nth-child(even) { background-color: #f9f9f9; }");
        sb.AppendLine(".total { font-weight: bold; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine($"<h1>Raport klienta: {customer.FirstName} {customer.LastName}</h1>");

        if (startDate.HasValue && endDate.HasValue)
        {
            sb.AppendLine($"<p>Okres: {startDate.Value:dd.MM.yyyy} - {endDate.Value:dd.MM.yyyy}</p>");
        }
        else if (startDate.HasValue)
        {
            sb.AppendLine($"<p>Od: {startDate.Value:dd.MM.yyyy}</p>");
        }
        else if (endDate.HasValue)
        {
            sb.AppendLine($"<p>Do: {endDate.Value:dd.MM.yyyy}</p>");
        }

        sb.AppendLine("<h2>Podsumowanie pojazdów</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Pojazd</th><th>Rejestracja</th><th>Liczba zleceń</th><th>Koszt całkowity</th></tr>");

        decimal totalCustomerCost = 0;

        foreach (var vehicle in customer.Vehicles)
        {
            decimal vehicleTotalCost = 0;
            int orderCount = 0;

            foreach (var order in vehicle.ServiceOrders)
            {
                orderCount++;

                foreach (var task in order.Tasks)
                {
                    vehicleTotalCost += task.LaborCost;

                    foreach (var usedPart in task.UsedParts)
                    {
                        vehicleTotalCost += usedPart.Quantity * usedPart.Part.UnitPrice;
                    }
                }
            }

            totalCustomerCost += vehicleTotalCost;

            sb.AppendLine($"<tr>");
            sb.AppendLine($"<td>{vehicle.Make} {vehicle.Model} ({vehicle.Year})</td>");
            sb.AppendLine($"<td>{vehicle.RegistrationNumber}</td>");
            sb.AppendLine($"<td>{orderCount}</td>");
            sb.AppendLine($"<td>{vehicleTotalCost:C}</td>");
            sb.AppendLine($"</tr>");
        }

        sb.AppendLine($"<tr class='total'>");
        sb.AppendLine($"<td colspan='3'>RAZEM</td>");
        sb.AppendLine($"<td>{totalCustomerCost:C}</td>");
        sb.AppendLine($"</tr>");

        sb.AppendLine("</table>");

        // Szczegóły zleceń
        sb.AppendLine("<h2>Szczegółowe zlecenia</h2>");

        foreach (var vehicle in customer.Vehicles)
        {
            if (vehicle.ServiceOrders.Any())
            {
                sb.AppendLine($"<h3>{vehicle.Make} {vehicle.Model} ({vehicle.RegistrationNumber})</h3>");

                foreach (var order in vehicle.ServiceOrders)
                {
                    decimal orderTotal = 0;

                    sb.AppendLine("<table>");
                    sb.AppendLine($"<tr><th colspan='4'>Zlecenie #{order.Id} - {order.CreatedAt:dd.MM.yyyy} - Status: {order.Status}</th></tr>");
                    sb.AppendLine("<tr><th>Czynność</th><th>Koszt robocizny</th><th>Części</th><th>Koszt części</th></tr>");

                    foreach (var task in order.Tasks)
                    {
                        decimal taskPartsTotal = 0;
                        var partsDescription = new System.Text.StringBuilder();

                        foreach (var usedPart in task.UsedParts)
                        {
                            decimal partCost = usedPart.Quantity * usedPart.Part.UnitPrice;
                            taskPartsTotal += partCost;

                            partsDescription.AppendLine($"{usedPart.Part.Name} x{usedPart.Quantity} ({partCost:C})<br>");
                        }

                        sb.AppendLine("<tr>");
                        sb.AppendLine($"<td>{task.Description}</td>");
                        sb.AppendLine($"<td>{task.LaborCost:C}</td>");
                        sb.AppendLine($"<td>{partsDescription}</td>");
                        sb.AppendLine($"<td>{taskPartsTotal:C}</td>");
                        sb.AppendLine("</tr>");

                        orderTotal += task.LaborCost + taskPartsTotal;
                    }

                    sb.AppendLine($"<tr class='total'><td colspan='3'>Razem za zlecenie</td><td>{orderTotal:C}</td></tr>");
                    sb.AppendLine("</table>");
                    sb.AppendLine("<br>");
                }
            }
        }

        sb.AppendLine("<p>Raport wygenerowany: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "</p>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private string GenerateVehicleReportHtml(Vehicle vehicle, DateTime? startDate, DateTime? endDate)
    {
        // Podobna implementacja jak dla raportu klienta, ale skupiająca się na jednym pojeździe
        var sb = new System.Text.StringBuilder();

        // Kod HTML dla raportu pojazdu...

        return sb.ToString();
    }

    private string GenerateMonthlyReportHtml(List<ServiceOrder> orders, int month, int year)
    {
        // Implementacja HTML dla raportu miesięcznego
        var sb = new System.Text.StringBuilder();

        // Kod HTML dla raportu miesięcznego...

        return sb.ToString();
    }

    private string GenerateActiveOrdersReportHtml(List<ServiceOrder> activeOrders)
    {
        // Implementacja HTML dla raportu aktywnych zleceń
        var sb = new System.Text.StringBuilder();

        // Kod HTML dla raportu aktywnych zleceń...

        return sb.ToString();
    }

    #endregion

    private byte[] GeneratePdfFromHtml(string htmlContent)
    {
        var globalSettings = new GlobalSettings
        {
            ColorMode = ColorMode.Color,
            Orientation = Orientation.Portrait,
            PaperSize = PaperKind.A4,
            Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 },
            DocumentTitle = "Raport"
        };

        var objectSettings = new ObjectSettings
        {
            PagesCount = true,
            HtmlContent = htmlContent,
            WebSettings = { DefaultEncoding = "utf-8" }
        };

        var document = new HtmlToPdfDocument()
        {
            GlobalSettings = globalSettings,
            Objects = { objectSettings }
        };

        return _converter.Convert(document);
    }
}