using Riok.Mapperly.Abstractions;
using WorkshopManager.Models;
using WorkshopManager.DTOs;

namespace WorkshopManager.Mappers;

[Mapper]
public partial class CommentMapper
{
    // Mapowanie z Comment do CommentDto
    [MapperIgnoreTarget(nameof(CommentDto.AuthorName))] // AuthorName będzie ustawiony ręcznie
    [MapperIgnoreSource(nameof(Comment.ServiceOrder))] // Ignoruj relację ServiceOrder
    [MapperIgnoreSource(nameof(Comment.Author))] // Ignoruj relację Author
    public partial CommentDto CommentToDto(Comment comment);

    // Mapowanie z CommentDto do Comment
    [MapperIgnoreSource(nameof(CommentDto.AuthorName))] // AuthorName to tylko właściwość wyświetlania
    [MapperIgnoreTarget(nameof(Comment.ServiceOrder))] // ServiceOrder będzie załadowany przez EF
    [MapperIgnoreTarget(nameof(Comment.Author))] // Author będzie załadowany przez EF
    public partial Comment DtoToComment(CommentDto dto);

    // Mapowanie kolekcji
    public partial IEnumerable<CommentDto> CommentsToDto(IEnumerable<Comment> comments);

    // Metoda z pełnymi detalami (ręczna implementacja)
    public CommentDto CommentToDtoWithAuthor(Comment comment)
    {
        var dto = CommentToDto(comment);

        // Ustaw nazwę autora
        dto.AuthorName = comment.Author != null
            ? $"{comment.Author.FirstName} {comment.Author.LastName}"
            : null;

        return dto;
    }

    // Alternatywna metoda bez automatycznego mapowania 
    public CommentDto CommentToDtoManual(Comment comment)
    {
        return new CommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            ServiceOrderId = comment.ServiceOrderId,
            AuthorId = comment.AuthorId ?? string.Empty,
            AuthorName = comment.Author != null
                ? $"{comment.Author.FirstName} {comment.Author.LastName}"
                : null
        };
    }
}