using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Persistence;
using DomainTaskStatus = TaskManagement.Domain.Enums.TaskItemStatus;

namespace TaskManagement.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DashboardService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<DashboardSummaryDto>> GetSummaryAsync()
    {
        IQueryable<TaskItem> query = _db.Tasks
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .Include(t => t.Team)
            .Include(t => t.Comments);

        if (_currentUser.Role == UserRole.Manager)
        {
            var managedTeamIds = await _db.Teams
                .Where(t => t.ManagerId == _currentUser.UserId)
                .Select(t => t.Id)
                .ToListAsync();

            query = query.Where(t => t.TeamId != null && managedTeamIds.Contains(t.TeamId.Value));
        }
        else if (_currentUser.Role == UserRole.User)
        {
            query = query.Where(t => t.AssignedToId == _currentUser.UserId);
        }

        var tasks = await query.ToListAsync();
        var now = DateTime.UtcNow;

        var summary = new DashboardSummaryDto
        {
            TotalCount = tasks.Count,
            ToDoCount = tasks.Count(t => t.Status == DomainTaskStatus.ToDo),
            InProgressCount = tasks.Count(t => t.Status == DomainTaskStatus.InProgress),
            DoneCount = tasks.Count(t => t.Status == DomainTaskStatus.Done),
            OverdueCount = tasks.Count(t => t.DueDate.HasValue && t.DueDate < now && t.Status != DomainTaskStatus.Done),
            HighPriorityCount = tasks.Count(t => t.Priority == TaskPriority.High && t.Status != DomainTaskStatus.Done),
            UpcomingDeadlines = tasks
                .Where(t => t.DueDate.HasValue && t.Status != DomainTaskStatus.Done)
                .OrderBy(t => t.DueDate)
                .Take(5)
                .Select(t => new TaskDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Status = t.Status,
                    Priority = t.Priority,
                    DueDate = t.DueDate,
                    AssignedToId = t.AssignedToId,
                    AssignedToName = t.AssignedTo?.FullName,
                    CreatedById = t.CreatedById,
                    CreatedByName = t.CreatedBy?.FullName,
                    TeamId = t.TeamId,
                    TeamName = t.Team?.Name,
                    CommentCount = t.Comments.Count,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                })
                .ToList()
        };

        return Result<DashboardSummaryDto>.Success(summary);
    }
}
