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
    private readonly IEmailService _emailService;

    public ReportService(
        ApplicationDbContext context,
        IConverter converter,
        IWebHostEnvironment environment,
        IEmailService emailService)
    {
        _context = context;
        _converter = converter;
        _environment = environment;
        _emailService = emailService;
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
        await _emailService.SendDailyReportAsync(email, reportData);
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
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset='UTF-8'>");
        sb.AppendLine("<title>Raport pojazdu</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 40px; }");
        sb.AppendLine("h1, h2 { color: #333366; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
        sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        sb.AppendLine("th { background-color: #f2f2f2; }");
        sb.AppendLine("tr:nth-child(even) { background-color: #f9f9f9; }");
        sb.AppendLine(".total { font-weight: bold; }");
        sb.AppendLine(".vehicle-info { background-color: #e8f4fd; padding: 15px; border-radius: 5px; margin-bottom: 20px; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine($"<h1>Raport pojazdu</h1>");

        // Informacje o pojeździe
        sb.AppendLine("<div class='vehicle-info'>");
        sb.AppendLine($"<h2>{vehicle.Make} {vehicle.Model} ({vehicle.Year})</h2>");
        sb.AppendLine($"<p><strong>Numer rejestracyjny:</strong> {vehicle.RegistrationNumber}</p>");
        sb.AppendLine($"<p><strong>VIN:</strong> {vehicle.VIN ?? "Brak danych"}</p>");
        sb.AppendLine($"<p><strong>Właściciel:</strong> {vehicle.Customer?.FirstName} {vehicle.Customer?.LastName}</p>");
        sb.AppendLine($"<p><strong>Telefon:</strong> {vehicle.Customer?.PhoneNumber ?? "Brak"}</p>");
        sb.AppendLine("</div>");

        if (startDate.HasValue && endDate.HasValue)
        {
            sb.AppendLine($"<p><strong>Okres raportu:</strong> {startDate.Value:dd.MM.yyyy} - {endDate.Value:dd.MM.yyyy}</p>");
        }
        else if (startDate.HasValue)
        {
            sb.AppendLine($"<p><strong>Od:</strong> {startDate.Value:dd.MM.yyyy}</p>");
        }
        else if (endDate.HasValue)
        {
            sb.AppendLine($"<p><strong>Do:</strong> {endDate.Value:dd.MM.yyyy}</p>");
        }

        // Podsumowanie
        decimal totalVehicleCost = 0;
        int totalOrders = vehicle.ServiceOrders.Count();

        foreach (var order in vehicle.ServiceOrders)
        {
            foreach (var task in order.Tasks)
            {
                totalVehicleCost += task.LaborCost;
                foreach (var usedPart in task.UsedParts)
                {
                    totalVehicleCost += usedPart.Quantity * usedPart.Part.UnitPrice;
                }
            }
        }

        sb.AppendLine("<h2>Podsumowanie</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Liczba zleceń</th><th>Koszt całkowity</th><th>Średni koszt zlecenia</th></tr>");
        sb.AppendLine("<tr>");
        sb.AppendLine($"<td>{totalOrders}</td>");
        sb.AppendLine($"<td>{totalVehicleCost:C}</td>");
        sb.AppendLine($"<td>{(totalOrders > 0 ? totalVehicleCost / totalOrders : 0):C}</td>");
        sb.AppendLine("</tr>");
        sb.AppendLine("</table>");

        // Historia zleceń
        sb.AppendLine("<h2>Historia napraw</h2>");

        if (vehicle.ServiceOrders.Any())
        {
            foreach (var order in vehicle.ServiceOrders.OrderByDescending(o => o.CreatedAt))
            {
                decimal orderTotal = 0;

                sb.AppendLine("<table>");
                sb.AppendLine($"<tr><th colspan='4'>Zlecenie #{order.Id} - {order.CreatedAt:dd.MM.yyyy} - Status: {order.Status}</th></tr>");

                if (!string.IsNullOrEmpty(order.Description))
                {
                    sb.AppendLine($"<tr><td colspan='4'><strong>Opis:</strong> {order.Description}</td></tr>");
                }

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
        else
        {
            sb.AppendLine("<p>Brak zleceń dla tego pojazdu w wybranym okresie.</p>");
        }

        sb.AppendLine("<p>Raport wygenerowany: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "</p>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private string GenerateMonthlyReportHtml(List<ServiceOrder> orders, int month, int year)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset='UTF-8'>");
        sb.AppendLine("<title>Raport miesięczny</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 40px; }");
        sb.AppendLine("h1, h2 { color: #333366; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
        sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        sb.AppendLine("th { background-color: #f2f2f2; }");
        sb.AppendLine("tr:nth-child(even) { background-color: #f9f9f9; }");
        sb.AppendLine(".total { font-weight: bold; background-color: #d4edda; }");
        sb.AppendLine(".summary { background-color: #e8f4fd; padding: 15px; border-radius: 5px; margin-bottom: 20px; }");
        sb.AppendLine(".text-center { text-align: center; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        var monthName = new DateTime(year, month, 1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("pl-PL"));
        sb.AppendLine($"<h1>Raport miesięczny - {monthName}</h1>");

        // Oblicz statystyki
        decimal totalRevenue = 0;
        int totalOrders = orders.Count;
        var ordersByStatus = orders.GroupBy(o => o.Status).ToDictionary(g => g.Key, g => g.Count());
        var mechanicStats = new Dictionary<string, (int orders, decimal revenue)>();

        foreach (var order in orders)
        {
            decimal orderTotal = 0;

            foreach (var task in order.Tasks)
            {
                orderTotal += task.LaborCost;
                foreach (var usedPart in task.UsedParts)
                {
                    orderTotal += usedPart.Quantity * usedPart.Part.UnitPrice;
                }
            }

            totalRevenue += orderTotal;

            // Statystyki mechaników
            var mechanicName = order.Mechanic != null ? $"{order.Mechanic.FirstName} {order.Mechanic.LastName}" : "Nieprzypisany";
            if (!mechanicStats.ContainsKey(mechanicName))
            {
                mechanicStats[mechanicName] = (0, 0);
            }
            mechanicStats[mechanicName] = (mechanicStats[mechanicName].orders + 1, mechanicStats[mechanicName].revenue + orderTotal);
        }

        // Podsumowanie
        sb.AppendLine("<div class='summary'>");
        sb.AppendLine("<h2>Podsumowanie miesiąca</h2>");
        sb.AppendLine($"<p><strong>Liczba zleceń:</strong> {totalOrders}</p>");
        sb.AppendLine($"<p><strong>Przychód całkowity:</strong> {totalRevenue:C}</p>");
        sb.AppendLine($"<p><strong>Średnia wartość zlecenia:</strong> {(totalOrders > 0 ? totalRevenue / totalOrders : 0):C}</p>");
        sb.AppendLine("</div>");

        // Statystyki statusów
        sb.AppendLine("<h2>Zlecenia według statusu</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Status</th><th>Liczba zleceń</th><th>Procent</th></tr>");

        foreach (var status in Enum.GetValues<OrderStatus>())
        {
            int count = ordersByStatus.GetValueOrDefault(status, 0);
            double percentage = totalOrders > 0 ? (double)count / totalOrders * 100 : 0;

            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{status}</td>");
            sb.AppendLine($"<td class='text-center'>{count}</td>");
            sb.AppendLine($"<td class='text-center'>{percentage:F1}%</td>");
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</table>");

        // Statystyki mechaników
        sb.AppendLine("<h2>Statystyki mechaników</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Mechanik</th><th>Liczba zleceń</th><th>Przychód</th><th>Średnia na zlecenie</th></tr>");

        foreach (var mechanic in mechanicStats.OrderByDescending(m => m.Value.revenue))
        {
            var avgPerOrder = mechanic.Value.orders > 0 ? mechanic.Value.revenue / mechanic.Value.orders : 0;

            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{mechanic.Key}</td>");
            sb.AppendLine($"<td class='text-center'>{mechanic.Value.orders}</td>");
            sb.AppendLine($"<td>{mechanic.Value.revenue:C}</td>");
            sb.AppendLine($"<td>{avgPerOrder:C}</td>");
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</table>");

        // Lista wszystkich zleceń
        sb.AppendLine("<h2>Szczegółowa lista zleceń</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>ID</th><th>Data</th><th>Klient</th><th>Pojazd</th><th>Mechanik</th><th>Status</th><th>Wartość</th></tr>");

        foreach (var order in orders.OrderBy(o => o.CreatedAt))
        {
            decimal orderTotal = 0;
            foreach (var task in order.Tasks)
            {
                orderTotal += task.LaborCost;
                foreach (var usedPart in task.UsedParts)
                {
                    orderTotal += usedPart.Quantity * usedPart.Part.UnitPrice;
                }
            }

            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>#{order.Id}</td>");
            sb.AppendLine($"<td>{order.CreatedAt:dd.MM.yyyy}</td>");
            sb.AppendLine($"<td>{order.Vehicle?.Customer?.FirstName} {order.Vehicle?.Customer?.LastName}</td>");
            sb.AppendLine($"<td>{order.Vehicle?.Make} {order.Vehicle?.Model} ({order.Vehicle?.RegistrationNumber})</td>");
            sb.AppendLine($"<td>{order.Mechanic?.FirstName} {order.Mechanic?.LastName ?? "Nieprzypisany"}</td>");
            sb.AppendLine($"<td>{order.Status}</td>");
            sb.AppendLine($"<td>{orderTotal:C}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine($"<tr class='total'>");
        sb.AppendLine($"<td colspan='6'>RAZEM</td>");
        sb.AppendLine($"<td>{totalRevenue:C}</td>");
        sb.AppendLine($"</tr>");

        sb.AppendLine("</table>");

        sb.AppendLine("<p>Raport wygenerowany: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "</p>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private string GenerateActiveOrdersReportHtml(List<ServiceOrder> activeOrders)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset='UTF-8'>");
        sb.AppendLine("<title>Raport aktywnych zleceń</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 40px; }");
        sb.AppendLine("h1, h2 { color: #333366; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
        sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        sb.AppendLine("th { background-color: #f2f2f2; }");
        sb.AppendLine("tr:nth-child(even) { background-color: #f9f9f9; }");
        sb.AppendLine(".status-new { background-color: #fff3cd; }");
        sb.AppendLine(".status-inprogress { background-color: #d1ecf1; }");
        sb.AppendLine(".priority-high { color: #dc3545; font-weight: bold; }");
        sb.AppendLine(".summary { background-color: #e8f4fd; padding: 15px; border-radius: 5px; margin-bottom: 20px; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine($"<h1>Raport aktywnych zleceń</h1>");
        sb.AppendLine($"<p><strong>Data wygenerowania:</strong> {DateTime.Now:dd.MM.yyyy HH:mm:ss}</p>");

        // Podsumowanie
        var newOrders = activeOrders.Where(o => o.Status == OrderStatus.New).Count();
        var inProgressOrders = activeOrders.Where(o => o.Status == OrderStatus.InProgress).Count();

        sb.AppendLine("<div class='summary'>");
        sb.AppendLine("<h2>Podsumowanie</h2>");
        sb.AppendLine($"<p><strong>Łączna liczba aktywnych zleceń:</strong> {activeOrders.Count}</p>");
        sb.AppendLine($"<p><strong>Nowe zlecenia:</strong> {newOrders}</p>");
        sb.AppendLine($"<p><strong>W trakcie realizacji:</strong> {inProgressOrders}</p>");
        sb.AppendLine("</div>");

        // Zlecenia grupowane według mechaników
        sb.AppendLine("<h2>Zlecenia według mechaników</h2>");

        var ordersByMechanic = activeOrders.GroupBy(o => o.Mechanic?.FirstName + " " + o.Mechanic?.LastName ?? "Nieprzypisane");

        foreach (var mechanicGroup in ordersByMechanic.OrderBy(g => g.Key))
        {
            sb.AppendLine($"<h3>Mechanik: {mechanicGroup.Key} ({mechanicGroup.Count()} zleceń)</h3>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>ID</th><th>Data utworzenia</th><th>Klient</th><th>Pojazd</th><th>Status</th><th>Opis</th></tr>");

            foreach (var order in mechanicGroup.OrderBy(o => o.CreatedAt))
            {
                var rowClass = order.Status == OrderStatus.New ? "status-new" : "status-inprogress";

                sb.AppendLine($"<tr class='{rowClass}'>");
                sb.AppendLine($"<td>#{order.Id}</td>");
                sb.AppendLine($"<td>{order.CreatedAt:dd.MM.yyyy}</td>");
                sb.AppendLine($"<td>{order.Vehicle?.Customer?.FirstName} {order.Vehicle?.Customer?.LastName}</td>");
                sb.AppendLine($"<td>{order.Vehicle?.Make} {order.Vehicle?.Model} ({order.Vehicle?.RegistrationNumber})</td>");
                sb.AppendLine($"<td>{order.Status}</td>");
                sb.AppendLine($"<td>{order.Description ?? "Brak opisu"}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<br>");
        }

        // Zlecenia wymagające pilnej uwagi (starsze niż 7 dni)
        var urgentOrders = activeOrders.Where(o => (DateTime.Now - o.CreatedAt).TotalDays > 7).ToList();

        if (urgentOrders.Any())
        {
            sb.AppendLine("<h2>Zlecenia wymagające pilnej uwagi (starsze niż 7 dni)</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>ID</th><th>Dni od utworzenia</th><th>Klient</th><th>Pojazd</th><th>Mechanik</th><th>Status</th></tr>");

            foreach (var order in urgentOrders.OrderBy(o => o.CreatedAt))
            {
                var daysOld = (int)(DateTime.Now - order.CreatedAt).TotalDays;

                sb.AppendLine("<tr class='priority-high'>");
                sb.AppendLine($"<td>#{order.Id}</td>");
                sb.AppendLine($"<td>{daysOld} dni</td>");
                sb.AppendLine($"<td>{order.Vehicle?.Customer?.FirstName} {order.Vehicle?.Customer?.LastName}</td>");
                sb.AppendLine($"<td>{order.Vehicle?.Make} {order.Vehicle?.Model} ({order.Vehicle?.RegistrationNumber})</td>");
                sb.AppendLine($"<td>{order.Mechanic?.FirstName} {order.Mechanic?.LastName ?? "Nieprzypisany"}</td>");
                sb.AppendLine($"<td>{order.Status}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
        }

        // Legenda
        sb.AppendLine("<h3>Legenda</h3>");
        sb.AppendLine("<table style='width: auto;'>");
        sb.AppendLine("<tr class='status-new'><td>Nowe zlecenia</td></tr>");
        sb.AppendLine("<tr class='status-inprogress'><td>W trakcie realizacji</td></tr>");
        sb.AppendLine("<tr class='priority-high'><td>Wymagają pilnej uwagi (>7 dni)</td></tr>");
        sb.AppendLine("</table>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

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