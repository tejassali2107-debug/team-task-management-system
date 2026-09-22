using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.Services;

public class TeamService : ITeamService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notificationService;

    public TeamService(AppDbContext db, ICurrentUserService currentUser, INotificationService notificationService)
    {
        _db = db;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task<Result<List<TeamDto>>> GetAllAsync()
    {
        IQueryable<Team> query = _db.Teams.Include(t => t.Manager).Include(t => t.Members);

        if (_currentUser.Role == UserRole.Manager)
        {
            query = query.Where(t => t.ManagerId == _currentUser.UserId);
        }
        else if (_currentUser.Role == UserRole.User)
        {
            query = query.Where(t => t.Id == _currentUser.TeamId);
        }

        var teams = await query.OrderBy(t => t.Name).ToListAsync();
        return Result<List<TeamDto>>.Success(teams.Select(MapToDto).ToList());
    }

    public async Task<Result<TeamDto>> GetByIdAsync(Guid id)
    {
        var team = await LoadTeamAsync(id);
        if (team is null)
        {
            return Result<TeamDto>.Failure("Team not found.");
        }

        if (!CanAccessTeam(team))
        {
            return Result<TeamDto>.Failure("You do not have access to this team.");
        }

        return Result<TeamDto>.Success(MapToDto(team));
    }

    public async Task<Result<TeamDto>> CreateAsync(CreateTeamRequest request)
    {
        if (request.ManagerId.HasValue)
        {
            var validation = await ValidateManagerAsync(request.ManagerId.Value);
            if (validation is not null)
            {
                return Result<TeamDto>.Failure(validation);
            }
        }

        var team = new Team
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            ManagerId = request.ManagerId
        };

        _db.Teams.Add(team);
        await _db.SaveChangesAsync();

        if (request.ManagerId.HasValue)
        {
            var manager = await _db.Users.FindAsync(request.ManagerId.Value);
            if (manager is not null)
            {
                manager.TeamId = team.Id;
                await _db.SaveChangesAsync();
            }
        }

        var created = await LoadTeamAsync(team.Id);
        return Result<TeamDto>.Success(MapToDto(created!));
    }

    public async Task<Result<TeamDto>> UpdateAsync(Guid id, UpdateTeamRequest request)
    {
        var team = await LoadTeamAsync(id);
        if (team is null)
        {
            return Result<TeamDto>.Failure("Team not found.");
        }

        if (request.ManagerId.HasValue && request.ManagerId != team.ManagerId)
        {
            var validation = await ValidateManagerAsync(request.ManagerId.Value);
            if (validation is not null)
            {
                return Result<TeamDto>.Failure(validation);
            }

            var manager = await _db.Users.FindAsync(request.ManagerId.Value);
            if (manager is not null)
            {
                manager.TeamId = team.Id;
            }
        }

        team.Name = request.Name.Trim();
        team.Description = request.Description;
        team.ManagerId = request.ManagerId;

        await _db.SaveChangesAsync();

        var updated = await LoadTeamAsync(id);
        return Result<TeamDto>.Success(MapToDto(updated!));
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var team = await _db.Teams.FindAsync(id);
        if (team is null)
        {
            return Result.Failure("Team not found.");
        }

        _db.Teams.Remove(team);
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<TeamDto>> AddMemberAsync(Guid teamId, AddTeamMemberRequest request)
    {
        var team = await LoadTeamAsync(teamId);
        if (team is null)
        {
            return Result<TeamDto>.Failure("Team not found.");
        }

        if (!CanManageTeamMembership(team))
        {
            return Result<TeamDto>.Failure("You do not have permission to modify this team's membership.");
        }

        var user = await _db.Users.FindAsync(request.UserId);
        if (user is null)
        {
            return Result<TeamDto>.Failure("User not found.");
        }

        user.TeamId = team.Id;
        await _db.SaveChangesAsync();

        await _notificationService.NotifyAddedToTeamAsync(user, team);

        var updated = await LoadTeamAsync(teamId);
        return Result<TeamDto>.Success(MapToDto(updated!));
    }

    public async Task<Result<TeamDto>> RemoveMemberAsync(Guid teamId, Guid userId)
    {
        var team = await LoadTeamAsync(teamId);
        if (team is null)
        {
            return Result<TeamDto>.Failure("Team not found.");
        }

        if (!CanManageTeamMembership(team))
        {
            return Result<TeamDto>.Failure("You do not have permission to modify this team's membership.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TeamId == teamId);
        if (user is null)
        {
            return Result<TeamDto>.Failure("User is not a member of this team.");
        }

        user.TeamId = null;
        await _db.SaveChangesAsync();

        var updated = await LoadTeamAsync(teamId);
        return Result<TeamDto>.Success(MapToDto(updated!));
    }

    private async Task<string?> ValidateManagerAsync(Guid managerId)
    {
        var manager = await _db.Users.FindAsync(managerId);
        if (manager is null)
        {
            return "Manager not found.";
        }

        if (manager.Role != UserRole.Manager)
        {
            return "Assigned manager must have the Manager role.";
        }

        return null;
    }

    private bool CanAccessTeam(Team team)
    {
        return _currentUser.Role switch
        {
            UserRole.Admin => true,
            UserRole.Manager => team.ManagerId == _currentUser.UserId,
            _ => team.Id == _currentUser.TeamId
        };
    }

    private bool CanManageTeamMembership(Team team)
    {
        return _currentUser.Role == UserRole.Admin ||
               (_currentUser.Role == UserRole.Manager && team.ManagerId == _currentUser.UserId);
    }

    private Task<Team?> LoadTeamAsync(Guid id) =>
        _db.Teams.Include(t => t.Manager).Include(t => t.Members).FirstOrDefaultAsync(t => t.Id == id);

    private static TeamDto MapToDto(Team team) => new()
    {
        Id = team.Id,
        Name = team.Name,
        Description = team.Description,
        ManagerId = team.ManagerId,
        ManagerName = team.Manager?.FullName,
        MemberCount = team.Members.Count,
        CreatedAt = team.CreatedAt,
        Members = team.Members.Select(m => new UserDto
        {
            Id = m.Id,
            FullName = m.FullName,
            Email = m.Email,
            Role = m.Role,
            TeamId = m.TeamId,
            CreatedAt = m.CreatedAt
        }).ToList()
    };
}
