using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.DTOs;

public class UsedPartDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Część jest wymagana")]
    [Display(Name = "Część")]
    public int PartId { get; set; }

    [Required(ErrorMessage = "Ilość jest wymagana")]
    [Range(1, 1000, ErrorMessage = "Ilość musi być między 1 a 1000")]
    [Display(Name = "Ilość")]
    public int Quantity { get; set; }

    [Required]
    public int ServiceTaskId { get; set; }

    // Właściwości tylko do odczytu
    [Display(Name = "Nazwa części")]
    public string? PartName { get; set; }

    [Display(Name = "Cena jednostkowa")]
    public decimal UnitPrice { get; set; }

    [Display(Name = "Wartość")]
    public decimal TotalPrice { get; set; }
}