using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(AppDbContext db, ICurrentUserService currentUser, IPasswordHasher passwordHasher)
    {
        _db = db;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<List<UserDto>>> GetAllAsync()
    {
        IQueryable<User> query = _db.Users.Include(u => u.Team);

        if (_currentUser.Role == UserRole.Manager)
        {
            var managedTeamIds = await _db.Teams
                .Where(t => t.ManagerId == _currentUser.UserId)
                .Select(t => t.Id)
                .ToListAsync();

            query = query.Where(u => u.Id == _currentUser.UserId || (u.TeamId != null && managedTeamIds.Contains(u.TeamId.Value)));
        }
        else if (_currentUser.Role == UserRole.User)
        {
            query = query.Where(u => u.Id == _currentUser.UserId);
        }

        var users = await query.OrderBy(u => u.FullName).Select(u => MapToDto(u)).ToListAsync();
        return Result<List<UserDto>>.Success(users);
    }

    public async Task<Result<UserDto>> GetByIdAsync(Guid id)
    {
        var user = await _db.Users.Include(u => u.Team).FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            return Result<UserDto>.Failure("User not found.");
        }

        if (!await CanAccessUserAsync(user))
        {
            return Result<UserDto>.Failure("You do not have access to this user.");
        }

        return Result<UserDto>.Success(MapToDto(user));
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == normalizedEmail))
        {
            return Result<UserDto>.Failure("An account with this email already exists.");
        }

        if (request.TeamId.HasValue && !await _db.Teams.AnyAsync(t => t.Id == request.TeamId.Value))
        {
            return Result<UserDto>.Failure("Team not found.");
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = request.Role,
            TeamId = request.TeamId
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var created = await _db.Users.Include(u => u.Team).FirstAsync(u => u.Id == user.Id);
        return Result<UserDto>.Success(MapToDto(created));
    }

    public async Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _db.Users.Include(u => u.Team).FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            return Result<UserDto>.Failure("User not found.");
        }

        if (request.TeamId.HasValue && !await _db.Teams.AnyAsync(t => t.Id == request.TeamId.Value))
        {
            return Result<UserDto>.Failure("Team not found.");
        }

        user.Role = request.Role;
        user.TeamId = request.TeamId;
        await _db.SaveChangesAsync();

        return Result<UserDto>.Success(MapToDto(user));
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null)
        {
            return Result.Failure("User not found.");
        }

        if (user.Id == _currentUser.UserId)
        {
            return Result.Failure("You cannot delete your own account.");
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    private async Task<bool> CanAccessUserAsync(User user)
    {
        if (_currentUser.Role == UserRole.Admin || user.Id == _currentUser.UserId)
        {
            return true;
        }

        if (_currentUser.Role == UserRole.Manager)
        {
            return await _db.Teams.AnyAsync(t => t.ManagerId == _currentUser.UserId && t.Id == user.TeamId);
        }

        return false;
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role,
        TeamId = user.TeamId,
        TeamName = user.Team?.Name,
        CreatedAt = user.CreatedAt
    };
}
