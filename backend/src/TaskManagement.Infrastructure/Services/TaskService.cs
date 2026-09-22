using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Persistence;
using DomainTaskStatus = TaskManagement.Domain.Enums.TaskItemStatus;

namespace TaskManagement.Infrastructure.Services;

public class TaskService : ITaskService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notificationService;

    public TaskService(AppDbContext db, ICurrentUserService currentUser, INotificationService notificationService)
    {
        _db = db;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task<Result<List<TaskDto>>> GetAllAsync(TaskFilterRequest filter)
    {
        var query = BaseQuery();
        query = await ApplyScopeAsync(query);

        if (filter.Status.HasValue) query = query.Where(t => t.Status == filter.Status.Value);
        if (filter.Priority.HasValue) query = query.Where(t => t.Priority == filter.Priority.Value);
        if (filter.DueBefore.HasValue) query = query.Where(t => t.DueDate != null && t.DueDate <= filter.DueBefore.Value);
        if (filter.DueAfter.HasValue) query = query.Where(t => t.DueDate != null && t.DueDate >= filter.DueAfter.Value);
        if (filter.AssignedToId.HasValue) query = query.Where(t => t.AssignedToId == filter.AssignedToId.Value);
        if (filter.TeamId.HasValue) query = query.Where(t => t.TeamId == filter.TeamId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(t => t.Title.Contains(term) || (t.Description != null && t.Description.Contains(term)));
        }

        var tasks = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        return Result<List<TaskDto>>.Success(tasks.Select(MapToDto).ToList());
    }

    public async Task<Result<TaskDto>> GetByIdAsync(Guid id)
    {
        var task = await LoadTaskAsync(id);
        if (task is null)
        {
            return Result<TaskDto>.Failure("Task not found.");
        }

        if (!await CanViewTaskAsync(task))
        {
            return Result<TaskDto>.Failure("You do not have access to this task.");
        }

        return Result<TaskDto>.Success(MapToDto(task));
    }

    public async Task<Result<TaskDto>> CreateAsync(CreateTaskRequest request)
    {
        User? assignee = null;
        if (request.AssignedToId.HasValue)
        {
            assignee = await _db.Users.FindAsync(request.AssignedToId.Value);
            if (assignee is null)
            {
                return Result<TaskDto>.Failure("Assignee not found.");
            }
        }

        var teamId = request.TeamId ?? assignee?.TeamId;

        if (_currentUser.Role == UserRole.Manager)
        {
            var managedTeamIds = await ManagedTeamIdsAsync();
            if (teamId is null || !managedTeamIds.Contains(teamId.Value))
            {
                return Result<TaskDto>.Failure("You can only create tasks for your own team.");
            }
        }

        var task = new TaskItem
        {
            Title = request.Title.Trim(),
            Description = request.Description,
            Priority = request.Priority,
            DueDate = request.DueDate,
            AssignedToId = request.AssignedToId,
            TeamId = teamId,
            CreatedById = _currentUser.UserId,
            Status = DomainTaskStatus.ToDo
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        await _notificationService.NotifyTaskAssignedAsync(task);

        var created = await LoadTaskAsync(task.Id);
        return Result<TaskDto>.Success(MapToDto(created!));
    }

    public async Task<Result<TaskDto>> UpdateAsync(Guid id, UpdateTaskRequest request)
    {
        var task = await LoadTaskAsync(id);
        if (task is null)
        {
            return Result<TaskDto>.Failure("Task not found.");
        }

        if (!await CanManageTaskAsync(task))
        {
            return Result<TaskDto>.Failure("You do not have permission to edit this task.");
        }

        var previousAssignee = task.AssignedToId;

        if (request.AssignedToId.HasValue && request.AssignedToId != task.AssignedToId)
        {
            var assignee = await _db.Users.FindAsync(request.AssignedToId.Value);
            if (assignee is null)
            {
                return Result<TaskDto>.Failure("Assignee not found.");
            }

            if (_currentUser.Role == UserRole.Manager)
            {
                var managedTeamIds = await ManagedTeamIdsAsync();
                if (assignee.TeamId is null || !managedTeamIds.Contains(assignee.TeamId.Value))
                {
                    return Result<TaskDto>.Failure("You can only assign tasks to your own team members.");
                }
            }
        }

        task.Title = request.Title.Trim();
        task.Description = request.Description;
        task.Priority = request.Priority;
        task.DueDate = request.DueDate;
        task.AssignedToId = request.AssignedToId;
        task.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        if (request.AssignedToId.HasValue && request.AssignedToId != previousAssignee)
        {
            await _notificationService.NotifyTaskAssignedAsync(task);
        }

        var updated = await LoadTaskAsync(id);
        return Result<TaskDto>.Success(MapToDto(updated!));
    }

    public async Task<Result<TaskDto>> UpdateStatusAsync(Guid id, UpdateTaskStatusRequest request)
    {
        var task = await LoadTaskAsync(id);
        if (task is null)
        {
            return Result<TaskDto>.Failure("Task not found.");
        }

        if (!await CanUpdateStatusAsync(task))
        {
            return Result<TaskDto>.Failure("You do not have permission to update this task's status.");
        }

        var oldStatus = task.Status;
        if (oldStatus == request.Status)
        {
            return Result<TaskDto>.Success(MapToDto(task));
        }

        task.Status = request.Status;
        task.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _notificationService.NotifyTaskStatusChangedAsync(task, oldStatus);

        var updated = await LoadTaskAsync(id);
        return Result<TaskDto>.Success(MapToDto(updated!));
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var task = await LoadTaskAsync(id);
        if (task is null)
        {
            return Result.Failure("Task not found.");
        }

        if (!await CanManageTaskAsync(task))
        {
            return Result.Failure("You do not have permission to delete this task.");
        }

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    private IQueryable<TaskItem> BaseQuery() =>
        _db.Tasks
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .Include(t => t.Team)
            .Include(t => t.Comments)
            .AsQueryable();

    private Task<TaskItem?> LoadTaskAsync(Guid id) => BaseQuery().FirstOrDefaultAsync(t => t.Id == id);

    private async Task<IQueryable<TaskItem>> ApplyScopeAsync(IQueryable<TaskItem> query)
    {
        if (_currentUser.Role == UserRole.Admin)
        {
            return query;
        }

        if (_currentUser.Role == UserRole.Manager)
        {
            var managedTeamIds = await ManagedTeamIdsAsync();
            return query.Where(t => t.TeamId != null && managedTeamIds.Contains(t.TeamId.Value));
        }

        return query.Where(t => t.AssignedToId == _currentUser.UserId);
    }

    private async Task<bool> CanViewTaskAsync(TaskItem task)
    {
        if (_currentUser.Role == UserRole.Admin) return true;
        if (task.AssignedToId == _currentUser.UserId || task.CreatedById == _currentUser.UserId) return true;

        if (_currentUser.Role == UserRole.Manager)
        {
            var managedTeamIds = await ManagedTeamIdsAsync();
            return task.TeamId != null && managedTeamIds.Contains(task.TeamId.Value);
        }

        return false;
    }

    private async Task<bool> CanManageTaskAsync(TaskItem task)
    {
        if (_currentUser.Role == UserRole.Admin) return true;

        if (_currentUser.Role == UserRole.Manager)
        {
            var managedTeamIds = await ManagedTeamIdsAsync();
            return task.TeamId != null && managedTeamIds.Contains(task.TeamId.Value);
        }

        return false;
    }

    private async Task<bool> CanUpdateStatusAsync(TaskItem task)
    {
        if (await CanManageTaskAsync(task)) return true;
        return task.AssignedToId == _currentUser.UserId;
    }

    private async Task<HashSet<Guid>> ManagedTeamIdsAsync()
    {
        return (await _db.Teams
            .Where(t => t.ManagerId == _currentUser.UserId)
            .Select(t => t.Id)
            .ToListAsync())
            .ToHashSet();
    }

    private static TaskDto MapToDto(TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        Status = task.Status,
        Priority = task.Priority,
        DueDate = task.DueDate,
        AssignedToId = task.AssignedToId,
        AssignedToName = task.AssignedTo?.FullName,
        CreatedById = task.CreatedById,
        CreatedByName = task.CreatedBy?.FullName,
        TeamId = task.TeamId,
        TeamName = task.Team?.Name,
        CommentCount = task.Comments.Count,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt
    };
}
