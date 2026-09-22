using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailSender _emailSender;

    public NotificationService(AppDbContext db, ICurrentUserService currentUser, IEmailSender emailSender)
    {
        _db = db;
        _currentUser = currentUser;
        _emailSender = emailSender;
    }

    public async Task<Result<List<NotificationDto>>> GetForCurrentUserAsync()
    {
        var notifications = await _db.Notifications
            .Where(n => n.UserId == _currentUser.UserId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                TaskItemId = n.TaskItemId,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return Result<List<NotificationDto>>.Success(notifications);
    }

    public async Task<Result> MarkAsReadAsync(Guid id)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == _currentUser.UserId);

        if (notification is null)
        {
            return Result.Failure("Notification not found.");
        }

        notification.IsRead = true;
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> MarkAllAsReadAsync()
    {
        await _db.Notifications
            .Where(n => n.UserId == _currentUser.UserId && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));

        return Result.Success();
    }

    public async Task NotifyTaskAssignedAsync(TaskItem task)
    {
        if (!task.AssignedToId.HasValue)
        {
            return;
        }

        var assignee = await _db.Users.FindAsync(task.AssignedToId.Value);
        if (assignee is null)
        {
            return;
        }

        var title = "New task assigned";
        var message = $"You have been assigned to the task \"{task.Title}\".";

        _db.Notifications.Add(new Notification
        {
            UserId = assignee.Id,
            Title = title,
            Message = message,
            Type = NotificationType.TaskAssigned,
            TaskItemId = task.Id
        });
        await _db.SaveChangesAsync();

        await _emailSender.SendAsync(assignee.Email, title, message);
    }

    public async Task NotifyTaskStatusChangedAsync(TaskItem task, TaskItemStatus oldStatus)
    {
        var recipientIds = new HashSet<Guid>();
        if (task.AssignedToId.HasValue) recipientIds.Add(task.AssignedToId.Value);
        recipientIds.Add(task.CreatedById);
        recipientIds.Remove(_currentUser.UserId);

        if (recipientIds.Count == 0)
        {
            return;
        }

        var recipients = await _db.Users.Where(u => recipientIds.Contains(u.Id)).ToListAsync();
        var title = "Task status updated";
        var message = $"Task \"{task.Title}\" moved from {oldStatus} to {task.Status}.";

        foreach (var recipient in recipients)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = recipient.Id,
                Title = title,
                Message = message,
                Type = NotificationType.TaskStatusUpdated,
                TaskItemId = task.Id
            });
        }
        await _db.SaveChangesAsync();

        foreach (var recipient in recipients)
        {
            await _emailSender.SendAsync(recipient.Email, title, message);
        }
    }

    public async Task NotifyCommentAddedAsync(Comment comment, TaskItem task)
    {
        var recipientIds = new HashSet<Guid>();
        if (task.AssignedToId.HasValue) recipientIds.Add(task.AssignedToId.Value);
        recipientIds.Add(task.CreatedById);
        recipientIds.Remove(comment.UserId);

        if (recipientIds.Count == 0)
        {
            return;
        }

        var recipients = await _db.Users.Where(u => recipientIds.Contains(u.Id)).ToListAsync();
        var title = "New comment on task";
        var message = $"A new comment was added to task \"{task.Title}\".";

        foreach (var recipient in recipients)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = recipient.Id,
                Title = title,
                Message = message,
                Type = NotificationType.CommentAdded,
                TaskItemId = task.Id
            });
        }
        await _db.SaveChangesAsync();

        foreach (var recipient in recipients)
        {
            await _emailSender.SendAsync(recipient.Email, title, message);
        }
    }

    public async Task NotifyAddedToTeamAsync(User user, Team team)
    {
        var title = "Added to a team";
        var message = $"You have been added to the team \"{team.Name}\".";

        _db.Notifications.Add(new Notification
        {
            UserId = user.Id,
            Title = title,
            Message = message,
            Type = NotificationType.AddedToTeam
        });
        await _db.SaveChangesAsync();

        await _emailSender.SendAsync(user.Email, title, message);
    }
}
