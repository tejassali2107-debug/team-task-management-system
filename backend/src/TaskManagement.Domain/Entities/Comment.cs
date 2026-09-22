using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Entities;

public class Comment : BaseEntity
{
    public string Content { get; set; } = string.Empty;

    public Guid TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }
}
