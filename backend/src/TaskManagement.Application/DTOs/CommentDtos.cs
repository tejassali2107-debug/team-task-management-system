using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs;

public class CommentDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public Guid TaskItemId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCommentRequest
{
    [Required, MaxLength(2000)]
    public string Content { get; set; } = string.Empty;
}
