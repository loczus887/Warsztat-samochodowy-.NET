// Services/Interfaces/IEmailService.cs
using WorkshopManager.Models;

namespace WorkshopManager.Services.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody, byte[]? attachment = null, string? attachmentName = null);
    Task SendDailyReportAsync(string toEmail, byte[] reportData);
    Task SendOrderStatusNotificationAsync(ServiceOrder order, string toEmail);
    Task SendInvoiceEmailAsync(string toEmail, byte[] invoicePdf, string invoiceNumber);
}