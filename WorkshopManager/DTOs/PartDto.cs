using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.DTOs;

public class PartDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nazwa części jest wymagana")]
    [StringLength(100, ErrorMessage = "Nazwa nie może być dłuższa niż 100 znaków")]
    [Display(Name = "Nazwa części")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Kategoria nie może być dłuższa niż 50 znaków")]
    [Display(Name = "Kategoria")]
    public string? Category { get; set; }

    [Required(ErrorMessage = "Cena jednostkowa jest wymagana")]
    [Range(0, 100000, ErrorMessage = "Cena musi być między 0 a 100000")]
    [Display(Name = "Cena jednostkowa (zł)")]
    public decimal UnitPrice { get; set; }

    // Właściwość tylko do odczytu
    [Display(Name = "Liczba użyć")]
    public int UsageCount { get; set; }
}