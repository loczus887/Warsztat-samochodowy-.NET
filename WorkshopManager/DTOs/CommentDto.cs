using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.DTOs;

public class CommentDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Treść komentarza jest wymagana")]
    [StringLength(1000, ErrorMessage = "Komentarz nie może być dłuższy niż 1000 znaków")]
    [Display(Name = "Komentarz")]
    public string Content { get; set; } = string.Empty;

    [DataType(DataType.DateTime)]
    [Display(Name = "Data dodania")]
    public DateTime CreatedAt { get; set; }

    [Required]
    public int ServiceOrderId { get; set; }

    [Required]
    public string AuthorId { get; set; } = string.Empty;

    // Właściwość tylko do odczytu
    [Display(Name = "Autor")]
    public string? AuthorName { get; set; }
}