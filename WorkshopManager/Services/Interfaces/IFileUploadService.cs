namespace WorkshopManager.Services.Interfaces;

public interface IFileUploadService
{
    Task<string> UploadImageAsync(IFormFile file, string folder = "vehicles");
    Task<bool> DeleteImageAsync(string imagePath);
    bool IsValidImage(IFormFile file);
}