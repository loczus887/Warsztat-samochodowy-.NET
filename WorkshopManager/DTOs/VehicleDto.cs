using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.DTOs;

public class VehicleDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Marka jest wymagana")]
    [StringLength(50, ErrorMessage = "Marka nie może być dłuższa niż 50 znaków")]
    [Display(Name = "Marka")]
    public string Make { get; set; } = string.Empty;

    [Required(ErrorMessage = "Model jest wymagany")]
    [StringLength(50, ErrorMessage = "Model nie może być dłuższy niż 50 znaków")]
    [Display(Name = "Model")]
    public string Model { get; set; } = string.Empty;

    [StringLength(17, MinimumLength = 17, ErrorMessage = "VIN musi mieć dokładnie 17 znaków")]
    [Display(Name = "VIN")]
    public string? VIN { get; set; }

    [Required(ErrorMessage = "Numer rejestracyjny jest wymagany")]
    [StringLength(10, ErrorMessage = "Numer rejestracyjny nie może być dłuższy niż 10 znaków")]
    [Display(Name = "Numer rejestracyjny")]
    public string RegistrationNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rok produkcji jest wymagany")]
    [Range(1900, 2100, ErrorMessage = "Podaj prawidłowy rok produkcji")]
    [Display(Name = "Rok produkcji")]
    public int Year { get; set; }

    public string? ImageUrl { get; set; }

    [Display(Name = "Zdjęcie pojazdu")]
    public IFormFile? ImageFile { get; set; }

    [Required(ErrorMessage = "Klient jest wymagany")]
    [Display(Name = "Klient")]
    public int CustomerId { get; set; }

    [Display(Name = "Klient")]
    public string? CustomerFullName { get; set; }

    [Display(Name = "Liczba zleceń")]
    public int ServiceOrderCount { get; set; }

    [Display(Name = "Pojazd")]
    public string VehicleInfo => $"{Make} {Model} ({RegistrationNumber})";
}