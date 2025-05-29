using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.DTOs;

public class CustomerDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Imię jest wymagane")]
    [StringLength(50, ErrorMessage = "Imię nie może być dłuższe niż 50 znaków")]
    [Display(Name = "Imię")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nazwisko jest wymagane")]
    [StringLength(50, ErrorMessage = "Nazwisko nie może być dłuższe niż 50 znaków")]
    [Display(Name = "Nazwisko")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Numer telefonu jest wymagany")]
    [Phone(ErrorMessage = "Nieprawidłowy format numeru telefonu")]
    [StringLength(15, ErrorMessage = "Numer telefonu nie może być dłuższy niż 15 znaków")]
    [Display(Name = "Telefon")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Nieprawidłowy format adresu email")]
    [StringLength(100, ErrorMessage = "Email nie może być dłuższy niż 100 znaków")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Display(Name = "Liczba pojazdów")]
    public int VehicleCount { get; set; }

    [Display(Name = "Imię i nazwisko")]
    public string FullName => $"{FirstName} {LastName}";
}