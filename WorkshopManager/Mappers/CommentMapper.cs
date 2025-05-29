using Riok.Mapperly.Abstractions;
using WorkshopManager.Models;
using WorkshopManager.DTOs;

namespace WorkshopManager.Mappers;

[Mapper]
public partial class CommentMapper
{
    public partial CommentDto CommentToDto(Comment comment);

    [MapperIgnoreTarget(nameof(CommentDto.AuthorName))]
    public partial Comment DtoToComment(CommentDto dto);

    public partial IEnumerable<CommentDto> CommentsToDto(IEnumerable<Comment> comments);

    // Metoda z ręczną implementacją
    public CommentDto CommentToDtoWithAuthor(Comment comment)
    {
        var dto = CommentToDto(comment);
        dto.AuthorName = comment.Author != null
            ? $"{comment.Author.FirstName} {comment.Author.LastName}"
            : null;
        return dto;
    }
}