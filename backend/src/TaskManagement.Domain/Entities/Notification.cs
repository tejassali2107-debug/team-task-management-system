using TaskManagement.Domain.Common;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities;

public class Notification : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid? TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }
}
