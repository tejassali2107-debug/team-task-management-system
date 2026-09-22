using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Interfaces;

public interface INotificationService
{
    Task<Result<List<NotificationDto>>> GetForCurrentUserAsync();
    Task<Result> MarkAsReadAsync(Guid id);
    Task<Result> MarkAllAsReadAsync();

    Task NotifyTaskAssignedAsync(TaskItem task);
    Task NotifyTaskStatusChangedAsync(TaskItem task, TaskItemStatus oldStatus);
    Task NotifyCommentAddedAsync(Comment comment, TaskItem task);
    Task NotifyAddedToTeamAsync(User user, Team team);
}
