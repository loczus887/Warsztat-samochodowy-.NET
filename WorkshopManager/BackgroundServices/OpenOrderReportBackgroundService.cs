using Microsoft.EntityFrameworkCore;
using WorkshopManager.Data;
using WorkshopManager.Models;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using System.Net.Mail;
using System.Net;

namespace WorkshopManager.BackgroundServices
{
    public class OpenOrderReportBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OpenOrderReportBackgroundService> _logger;
        private readonly IConfiguration _configuration;

        public OpenOrderReportBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<OpenOrderReportBackgroundService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OpenOrderReportBackgroundService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await GenerateAndSendReport();

                    // Dla testów: co 2 minuty: TimeSpan.FromMinutes(1)
                    var delay = TimeSpan.FromDays(1);

                    _logger.LogInformation($"Next report will be generated in {delay.TotalMinutes} minutes.");
                    await Task.Delay(delay, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while generating daily report.");
                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                }
            }
        }

        private async Task GenerateAndSendReport()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Pobierz otwarte zlecenia
                var openOrders = await context.ServiceOrders
                    .Include(so => so.Vehicle)
                        .ThenInclude(v => v.Customer)
                    .Include(so => so.Mechanic)
                    .Include(so => so.Tasks)
                    .Where(so => so.Status != OrderStatus.Completed && so.Status != OrderStatus.Cancelled)
                    .OrderBy(so => so.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation($"Found {openOrders.Count} open orders to include in report.");

                // Generuj PDF
                var pdfPath = await GeneratePdfReport(openOrders);

                // Wyślij e-mail
                await SendEmailWithAttachment(pdfPath);

                _logger.LogInformation("Daily report generated and sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GenerateAndSendReport method.");
                throw;
            }
        }

        private async Task<string> GeneratePdfReport(List<ServiceOrder> openOrders)
        {
            await Task.Delay(1); // Dodane żeby usunąć warning o async

            var fileName = $"raport-otwarte-naprawy-{DateTime.Now:yyyy-MM-dd}.pdf";
            var filePath = Path.Combine("reports", fileName);

            // Upewnij się, że folder reports istnieje
            Directory.CreateDirectory("reports");

            using var writer = new PdfWriter(filePath);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            // Czcionki
            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Nagłówek raportu
            var title = new Paragraph("RAPORT - OTWARTE NAPRAWY")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFont(boldFont)
                .SetFontSize(20);
            document.Add(title);

            var dateInfo = new Paragraph($"Data wygenerowania: {DateTime.Now:dd.MM.yyyy HH:mm}")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFont(regularFont)
                .SetFontSize(12);
            document.Add(dateInfo);

            var summary = new Paragraph($"Łączna liczba otwartych zleceń: {openOrders.Count}")
                .SetFont(boldFont)
                .SetFontSize(14);
            document.Add(summary);

            document.Add(new Paragraph("\n"));

            if (openOrders.Any())
            {
                // Tabela ze zleceniami
                var table = new Table(UnitValue.CreatePercentArray(new float[] { 8, 20, 15, 12, 15, 15, 15 }))
                    .UseAllAvailableWidth();

                // Nagłówki tabeli
                table.AddHeaderCell(new Cell().Add(new Paragraph("ID").SetFont(boldFont)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Klient").SetFont(boldFont)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Pojazd").SetFont(boldFont)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Status").SetFont(boldFont)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Data utworzenia").SetFont(boldFont)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Mechanic").SetFont(boldFont)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Koszt robocizny").SetFont(boldFont)));

                // Dane
                foreach (var order in openOrders)
                {
                    table.AddCell(new Cell().Add(new Paragraph(order.Id.ToString()).SetFont(regularFont)));

                    // Klient (przez Vehicle)
                    var customerName = order.Vehicle?.Customer != null
                        ? $"{order.Vehicle.Customer.FirstName ?? ""} {order.Vehicle.Customer.LastName ?? ""}".Trim()
                        : "Nieznany klient";
                    if (string.IsNullOrEmpty(customerName)) customerName = "Nieznany klient";
                    table.AddCell(new Cell().Add(new Paragraph(customerName).SetFont(regularFont)));

                    // Pojazd
                    var vehicleInfo = order.Vehicle != null
                        ? $"{order.Vehicle.Make} {order.Vehicle.Model}"
                        : "Nieznany pojazd";
                    table.AddCell(new Cell().Add(new Paragraph(vehicleInfo).SetFont(regularFont)));

                    // Status
                    table.AddCell(new Cell().Add(new Paragraph(order.Status.ToString()).SetFont(regularFont)));

                    // Data utworzenia
                    table.AddCell(new Cell().Add(new Paragraph(order.CreatedAt.ToString("dd.MM.yyyy")).SetFont(regularFont)));

                    // Mechanic
                    var mechanicName = order.Mechanic != null
                        ? $"{order.Mechanic.FirstName ?? ""} {order.Mechanic.LastName ?? ""}".Trim()
                        : "Nieprzypisany";
                    if (string.IsNullOrEmpty(mechanicName)) mechanicName = "Nieprzypisany";
                    table.AddCell(new Cell().Add(new Paragraph(mechanicName).SetFont(regularFont)));

                    // Koszt robocizny z zadań
                    var laborCost = order.Tasks?.Sum(task => task.LaborCost) ?? 0;
                    table.AddCell(new Cell().Add(new Paragraph($"{laborCost:C}").SetFont(regularFont)));
                }

                document.Add(table);

                // Podsumowanie finansowe
                var totalLaborCost = openOrders.Sum(o => o.Tasks?.Sum(task => task.LaborCost) ?? 0);
                var totalTasks = openOrders.Sum(o => o.Tasks?.Count ?? 0);

                document.Add(new Paragraph($"\nŁączny koszt robocizny otwartych zleceń: {totalLaborCost:C}")
                    .SetFont(boldFont)
                    .SetFontSize(14));

                document.Add(new Paragraph($"Łączna liczba zadań: {totalTasks}")
                    .SetFont(boldFont)
                    .SetFontSize(12));

                // Statystyki statusów
                document.Add(new Paragraph("\nStatystyki statusów:").SetFont(boldFont).SetFontSize(12));
                var statusStats = openOrders
                    .GroupBy(o => o.Status)
                    .Select(g => $"• {g.Key}: {g.Count()} zleceń")
                    .ToList();

                foreach (var stat in statusStats)
                {
                    document.Add(new Paragraph(stat).SetFont(regularFont).SetFontSize(10));
                }
            }
            else
            {
                document.Add(new Paragraph("Brak otwartych zleceń w systemie.")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFont(regularFont)
                    .SetFontSize(14));
            }

            // Stopka
            var footer = new Paragraph("Raport wygenerowany automatycznie przez WorkshopManager")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFont(regularFont)
                .SetFontSize(10);
            document.Add(new Paragraph("\n"));
            document.Add(footer);

            _logger.LogInformation($"PDF report generated: {filePath}");
            return filePath;
        }

        private async Task SendEmailWithAttachment(string pdfPath)
        {
            try
            {
                var smtpHost = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var smtpUsername = _configuration["EmailSettings:Username"];
                var smtpPassword = _configuration["EmailSettings:Password"];
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromName = _configuration["EmailSettings:FromName"];
                var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");

                // Użyj fromEmail jako adminEmail
                var adminEmail = fromEmail;

                if (string.IsNullOrEmpty(smtpUsername) ||
                    string.IsNullOrEmpty(smtpPassword) ||
                    string.IsNullOrEmpty(adminEmail) ||
                    string.IsNullOrEmpty(fromEmail))
                {
                    _logger.LogWarning("Email configuration is incomplete. Skipping email sending.");
                    return;
                }

                using var smtpClient = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                    EnableSsl = enableSsl
                };

                var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName ?? "WorkshopManager"),
                    Subject = $"Dzienny raport zleceń - {DateTime.Now:dd.MM.yyyy}",
                    Body = $@"
Dzień dobry,

W załączniku przesyłamy dzienny raport zleceń z systemu WorkshopManager.

Data wygenerowania: {DateTime.Now:dd.MM.yyyy HH:mm}

Raport zawiera wszystkie zlecenia serwisowe z systemu.

Pozdrawienia,
System WorkshopManager",
                    IsBodyHtml = false
                };

                message.To.Add(adminEmail);

                if (File.Exists(pdfPath))
                {
                    message.Attachments.Add(new Attachment(pdfPath));
                }

                await smtpClient.SendMailAsync(message);
                _logger.LogInformation($"Report email sent successfully to {adminEmail}");


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email with report.");
                throw;
            }
        }
    }
}