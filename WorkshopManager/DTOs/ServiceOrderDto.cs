using System.ComponentModel.DataAnnotations;
using WorkshopManager.Models;

namespace WorkshopManager.DTOs;

public class ServiceOrderDto
{
    public int Id { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Data utworzenia")]
    public DateTime CreatedAt { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Data zakończenia")]
    public DateTime? CompletedAt { get; set; }

    [Required(ErrorMessage = "Opis problemu jest wymagany")]
    [StringLength(500, ErrorMessage = "Opis nie może być dłuższy niż 500 znaków")]
    [Display(Name = "Opis problemu")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Status")]
    public OrderStatus Status { get; set; }

    [Required(ErrorMessage = "Pojazd jest wymagany")]
    [Display(Name = "Pojazd")]
    public int VehicleId { get; set; }

    [Display(Name = "Mechanik")]
    public string? MechanicId { get; set; }

    // Właściwości tylko do odczytu
    [Display(Name = "Pojazd")]
    public string? VehicleInfo { get; set; }

    [Display(Name = "Klient")]
    public string? CustomerName { get; set; }

    [Display(Name = "Mechanik")]
    public string? MechanicName { get; set; }

    [Display(Name = "Liczba zadań")]
    public int TaskCount { get; set; }

    [Display(Name = "Koszt całkowity")]
    public decimal TotalCost { get; set; }
}