using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs;

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public Guid? TaskItemId { get; set; }
    public DateTime CreatedAt { get; set; }
}
