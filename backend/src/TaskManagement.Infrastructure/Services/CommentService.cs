using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.Services;

public class CommentService : ICommentService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notificationService;

    public CommentService(AppDbContext db, ICurrentUserService currentUser, INotificationService notificationService)
    {
        _db = db;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task<Result<List<CommentDto>>> GetForTaskAsync(Guid taskId)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task is null)
        {
            return Result<List<CommentDto>>.Failure("Task not found.");
        }

        if (!await CanAccessTaskAsync(task))
        {
            return Result<List<CommentDto>>.Failure("You do not have access to this task.");
        }

        var comments = await _db.Comments
            .Include(c => c.User)
            .Where(c => c.TaskItemId == taskId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto
            {
                Id = c.Id,
                Content = c.Content,
                UserId = c.UserId,
                UserName = c.User!.FullName,
                TaskItemId = c.TaskItemId,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return Result<List<CommentDto>>.Success(comments);
    }

    public async Task<Result<CommentDto>> AddAsync(Guid taskId, CreateCommentRequest request)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task is null)
        {
            return Result<CommentDto>.Failure("Task not found.");
        }

        if (!await CanAccessTaskAsync(task))
        {
            return Result<CommentDto>.Failure("You do not have access to this task.");
        }

        var comment = new Comment
        {
            Content = request.Content.Trim(),
            TaskItemId = taskId,
            UserId = _currentUser.UserId
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        await _notificationService.NotifyCommentAddedAsync(comment, task);

        var user = await _db.Users.FindAsync(_currentUser.UserId);

        return Result<CommentDto>.Success(new CommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            UserId = comment.UserId,
            UserName = user?.FullName ?? string.Empty,
            TaskItemId = taskId,
            CreatedAt = comment.CreatedAt
        });
    }

    private async Task<bool> CanAccessTaskAsync(TaskItem task)
    {
        if (_currentUser.Role == UserRole.Admin) return true;
        if (task.AssignedToId == _currentUser.UserId || task.CreatedById == _currentUser.UserId) return true;

        if (_currentUser.Role == UserRole.Manager)
        {
            var managedTeamIds = await _db.Teams
                .Where(t => t.ManagerId == _currentUser.UserId)
                .Select(t => t.Id)
                .ToListAsync();

            return task.TeamId != null && managedTeamIds.Contains(task.TeamId.Value);
        }

        return false;
    }
}
