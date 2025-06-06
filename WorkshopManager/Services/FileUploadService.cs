using WorkshopManager.Services.Interfaces;

namespace WorkshopManager.Services;

public class FileUploadService : IFileUploadService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileUploadService> _logger;

    public FileUploadService(IWebHostEnvironment environment, ILogger<FileUploadService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder = "vehicles")
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty");

        if (!IsValidImage(file))
            throw new ArgumentException("Invalid image format");

        try
        {
            // Utwórz folder jeśli nie istnieje
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsFolder);

            // Generuj unikalną nazwę pliku
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Zapisz plik
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Zwróć względną ścieżkę
            return $"/uploads/{folder}/{fileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image");
            throw;
        }
    }

    public async Task<bool> DeleteImageAsync(string imagePath)
    {
        try
        {
            if (string.IsNullOrEmpty(imagePath))
                return true;

            var fullPath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/'));

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Deleted image: {ImagePath}", imagePath);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image: {ImagePath}", imagePath);
            return false;
        }
    }

    public bool IsValidImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return false;

        // Sprawdź rozszerzenie
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
            return false;

        // Sprawdź typ MIME
        var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/bmp" };
        if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            return false;

        // Sprawdź rozmiar (max 5MB)
        if (file.Length > 5 * 1024 * 1024)
            return false;

        return true;
    }
}