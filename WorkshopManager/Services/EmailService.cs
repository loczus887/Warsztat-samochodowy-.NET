// Services/EmailService.cs
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using WorkshopManager.Models;
using WorkshopManager.Services.Interfaces;

namespace WorkshopManager.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, byte[]? attachment = null, string? attachmentName = null)
    {
        try
        {
            using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
            {
                EnableSsl = _emailSettings.EnableSsl,
                Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            // Dodaj załącznik jeśli istnieje
            if (attachment != null && !string.IsNullOrEmpty(attachmentName))
            {
                var stream = new MemoryStream(attachment);
                var att = new Attachment(stream, attachmentName, "application/pdf");
                message.Attachments.Add(att);
            }

            await client.SendMailAsync(message);
            _logger.LogInformation("Email wysłany pomyślnie do: {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd wysyłania emaila do: {Email}", toEmail);
            throw;
        }
    }

    public async Task SendDailyReportAsync(string toEmail, byte[] reportData)
    {
        var subject = $"Raport dzienny - aktywne zlecenia ({DateTime.Now:dd.MM.yyyy})";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; margin: 20px;'>
                <h2 style='color: #333366;'>Dzienny raport aktywnych zleceń</h2>
                <p>Dzień dobry,</p>
                <p>W załączniku przesyłamy raport aktywnych zleceń na dzień <strong>{DateTime.Now:dd.MM.yyyy}</strong>.</p>
                
                <div style='background-color: #e8f4fd; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0;'>Raport zawiera:</h3>
                    <ul>
                        <li>Listę wszystkich aktywnych zleceń</li>
                        <li>Podział według mechaników</li>
                        <li>Zlecenia wymagające pilnej uwagi (starsze niż 7 dni)</li>
                        <li>Podsumowanie statystyczne</li>
                    </ul>
                </div>
                
                <p>W razie pytań prosimy o kontakt.</p>
                <p>Pozdrawiamy,<br/>
                <strong>System WorkshopManager</strong></p>
                
                <hr style='margin-top: 30px; border: none; border-top: 1px solid #ddd;'>
                <p style='font-size: 12px; color: #666;'>
                    Email wygenerowany automatycznie przez system WorkshopManager w dniu {DateTime.Now:dd.MM.yyyy HH:mm:ss}
                </p>
            </body>
            </html>";

        var fileName = $"aktywne_zlecenia_{DateTime.Now:yyyyMMdd}.pdf";
        await SendEmailAsync(toEmail, subject, body, reportData, fileName);
    }

    public async Task SendOrderStatusNotificationAsync(ServiceOrder order, string toEmail)
    {
        var subject = $"Zmiana statusu zlecenia #{order.Id} - {order.Vehicle?.RegistrationNumber}";
        var statusColor = GetStatusColor(order.Status);
        var statusDescription = GetStatusDescription(order.Status);

        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; margin: 20px;'>
                <h2 style='color: #333366;'>Informacja o zmianie statusu zlecenia</h2>
                <p>Szanowny Kliencie,</p>
                <p>Informujemy o zmianie statusu Twojego zlecenia:</p>
                
                <table border='1' style='border-collapse: collapse; width: 100%; margin: 20px 0;'>
                    <tr style='background-color: #f2f2f2;'>
                        <td style='padding: 10px; font-weight: bold;'>Numer zlecenia:</td>
                        <td style='padding: 10px;'>#{order.Id}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; font-weight: bold;'>Pojazd:</td>
                        <td style='padding: 10px;'>{order.Vehicle?.Make} {order.Vehicle?.Model} ({order.Vehicle?.RegistrationNumber})</td>
                    </tr>
                    <tr style='background-color: #f9f9f9;'>
                        <td style='padding: 10px; font-weight: bold;'>Aktualny status:</td>
                        <td style='padding: 10px; color: {statusColor}; font-weight: bold;'>{statusDescription}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; font-weight: bold;'>Data utworzenia:</td>
                        <td style='padding: 10px;'>{order.CreatedAt:dd.MM.yyyy}</td>
                    </tr>
                    <tr style='background-color: #f9f9f9;'>
                        <td style='padding: 10px; font-weight: bold;'>Mechanik:</td>
                        <td style='padding: 10px;'>{order.Mechanic?.FirstName} {order.Mechanic?.LastName ?? "Nieprzypisany"}</td>
                    </tr>
                </table>
                
                {GetStatusSpecificMessage(order.Status)}
                
                <div style='background-color: #e8f4fd; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <p style='margin: 0;'><strong>Dane kontaktowe:</strong></p>
                    <p style='margin: 5px 0;'>📞 Telefon: [NUMER_TELEFONU]</p>
                    <p style='margin: 5px 0;'>📧 Email: [EMAIL_WARSZTATU]</p>
                    <p style='margin: 5px 0;'>🕒 Godziny pracy: [GODZINY_PRACY]</p>
                </div>
                
                <p>W razie pytań prosimy o kontakt.</p>
                <p>Pozdrawiamy,<br/>
                <strong>Zespół WorkshopManager</strong></p>
                
                <hr style='margin-top: 30px; border: none; border-top: 1px solid #ddd;'>
                <p style='font-size: 12px; color: #666;'>
                    Email wygenerowany automatycznie przez system WorkshopManager w dniu {DateTime.Now:dd.MM.yyyy HH:mm:ss}
                </p>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendInvoiceEmailAsync(string toEmail, byte[] invoicePdf, string invoiceNumber)
    {
        var subject = $"Faktura {invoiceNumber} - WorkshopManager";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; margin: 20px;'>
                <h2 style='color: #333366;'>Faktura za usługi warsztatowe</h2>
                <p>Szanowny Kliencie,</p>
                <p>W załączniku przesyłamy fakturę nr <strong>{invoiceNumber}</strong> za wykonane usługi warsztatowe.</p>
                
                <div style='background-color: #d4edda; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #28a745;'>
                    <p style='margin: 0; color: #155724;'><strong>Ważne informacje:</strong></p>
                    <ul style='margin: 10px 0; color: #155724;'>
                        <li>Termin płatności: [TERMIN_PLATNOSCI]</li>
                        <li>Sposób płatności: [SPOSOB_PLATNOSCI]</li>
                        <li>Numer konta: [NUMER_KONTA]</li>
                    </ul>
                </div>
                
                <p>Dziękujemy za skorzystanie z naszych usług i zapraszamy ponownie.</p>
                
                <div style='background-color: #e8f4fd; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <p style='margin: 0;'><strong>Dane kontaktowe:</strong></p>
                    <p style='margin: 5px 0;'>Telefon: [NUMER_TELEFONU]</p>
                    <p style='margin: 5px 0;'>Email: [EMAIL_WARSZTATU]</p>
                    <p style='margin: 5px 0;'>Godziny pracy: [GODZINY_PRACY]</p>
                </div>
                
                <p>Pozdrawiamy,<br/>
                <strong>Zespół WorkshopManager</strong></p>
                
                <hr style='margin-top: 30px; border: none; border-top: 1px solid #ddd;'>
                <p style='font-size: 12px; color: #666;'>
                    Email wygenerowany automatycznie przez system WorkshopManager w dniu {DateTime.Now:dd.MM.yyyy HH:mm:ss}
                </p>
            </body>
            </html>";

        var fileName = $"faktura_{invoiceNumber}.pdf";
        await SendEmailAsync(toEmail, subject, body, invoicePdf, fileName);
    }

    private string GetStatusDescription(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.New => "Nowe zlecenie",
            OrderStatus.InProgress => "W trakcie realizacji",
            OrderStatus.Completed => "Ukończone",
            OrderStatus.Cancelled => "Anulowane",
            _ => status.ToString()
        };
    }

    private string GetStatusColor(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.New => "#007bff",           // Niebieski
            OrderStatus.InProgress => "#ffc107",    // Żółty
            OrderStatus.Completed => "#28a745",     // Zielony
            OrderStatus.Cancelled => "#dc3545",     // Czerwony
            _ => "#6c757d"                          // Szary
        };
    }

    private string GetStatusSpecificMessage(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.New => @"
                <div style='background-color: #cce5ff; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #007bff;'>
                    <p style='margin: 0; color: #004085;'><strong>Zlecenie zostało przyjęte!</strong></p>
                    <p style='margin: 10px 0 0 0; color: #004085;'>Wkrótce mechanik przystąpi do diagnozy. Będziemy informować o kolejnych etapach realizacji.</p>
                </div>",

            OrderStatus.InProgress => @"
                <div style='background-color: #fff3cd; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #ffc107;'>
                    <p style='margin: 0; color: #856404;'><strong>Zlecenie jest w trakcie realizacji!</strong></p>
                    <p style='margin: 10px 0 0 0; color: #856404;'>Mechanik pracuje nad Twoim pojazdem. Będziemy informować o postępach.</p>
                </div>",

            OrderStatus.Completed => @"
                <div style='background-color: #d4edda; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #28a745;'>
                    <p style='margin: 0; color: #155724;'><strong>Zlecenie zostało ukończone!</strong></p>
                    <p style='margin: 10px 0 0 0; color: #155724;'>Prosimy o kontakt w celu odbioru pojazdu. Pojazd jest gotowy do odbioru.</p>
                </div>",

            OrderStatus.Cancelled => @"
                <div style='background-color: #f8d7da; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #dc3545;'>
                    <p style='margin: 0; color: #721c24;'><strong>Zlecenie zostało anulowane.</strong></p>
                    <p style='margin: 10px 0 0 0; color: #721c24;'>W razie pytań prosimy o kontakt z obsługą klienta.</p>
                </div>",

            _ => "<p>Będziemy informować o kolejnych etapach realizacji zlecenia.</p>"
        };
    }
}