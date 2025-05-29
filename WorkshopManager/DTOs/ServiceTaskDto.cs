using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.DTOs;

public class ServiceTaskDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Opis czynności jest wymagany")]
    [StringLength(200, ErrorMessage = "Opis nie może być dłuższy niż 200 znaków")]
    [Display(Name = "Opis czynności")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Koszt robocizny jest wymagany")]
    [Range(0, 100000, ErrorMessage = "Koszt musi być między 0 a 100000")]
    [Display(Name = "Koszt robocizny (zł)")]
    [DataType(DataType.Currency)]
    public decimal LaborCost { get; set; }

    [Required]
    public int ServiceOrderId { get; set; }

    // Właściwości tylko do odczytu
    [Display(Name = "Liczba użytych części")]
    public int PartsCount { get; set; }

    [Display(Name = "Koszt części (zł)")]
    [DataType(DataType.Currency)]
    public decimal PartsCost { get; set; }

    [Display(Name = "Koszt całkowity (zł)")]
    [DataType(DataType.Currency)]
    public decimal TotalCost { get; set; }
}