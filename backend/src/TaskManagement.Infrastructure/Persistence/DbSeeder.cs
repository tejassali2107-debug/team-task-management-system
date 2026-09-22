using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using DomainTaskStatus = TaskManagement.Domain.Enums.TaskItemStatus;

namespace TaskManagement.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher passwordHasher)
    {
        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync();
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }

        if (await db.Users.AnyAsync())
        {
            return;
        }

        var admin = new User
        {
            FullName = "Ava Admin",
            Email = "admin@taskflow.com",
            PasswordHash = passwordHasher.Hash("Admin@123"),
            Role = UserRole.Admin
        };

        var manager = new User
        {
            FullName = "Mark Manager",
            Email = "manager@taskflow.com",
            PasswordHash = passwordHasher.Hash("Manager@123"),
            Role = UserRole.Manager
        };

        var alice = new User
        {
            FullName = "Alice User",
            Email = "alice@taskflow.com",
            PasswordHash = passwordHasher.Hash("User@123"),
            Role = UserRole.User
        };

        var bob = new User
        {
            FullName = "Bob User",
            Email = "bob@taskflow.com",
            PasswordHash = passwordHasher.Hash("User@123"),
            Role = UserRole.User
        };

        db.Users.AddRange(admin, manager, alice, bob);
        await db.SaveChangesAsync();

        var team = new Team
        {
            Name = "Engineering",
            Description = "Core product engineering team",
            ManagerId = manager.Id
        };
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        manager.TeamId = team.Id;
        alice.TeamId = team.Id;
        bob.TeamId = team.Id;
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;

        db.Tasks.AddRange(
            new TaskItem
            {
                Title = "Set up CI pipeline",
                Description = "Configure GitHub Actions for build and test automation.",
                Status = DomainTaskStatus.InProgress,
                Priority = TaskPriority.High,
                DueDate = now.AddDays(3),
                AssignedTo = alice,
                CreatedBy = manager,
                Team = team
            },
            new TaskItem
            {
                Title = "Design task detail page",
                Description = "Create wireframes for the task detail and comments view.",
                Status = DomainTaskStatus.ToDo,
                Priority = TaskPriority.Medium,
                DueDate = now.AddDays(7),
                AssignedTo = bob,
                CreatedBy = manager,
                Team = team
            },
            new TaskItem
            {
                Title = "Fix login token expiry bug",
                Description = "Investigate reports of premature session expiry.",
                Status = DomainTaskStatus.Done,
                Priority = TaskPriority.High,
                DueDate = now.AddDays(-2),
                UpdatedAt = now.AddDays(-1),
                AssignedTo = alice,
                CreatedBy = manager,
                Team = team
            },
            new TaskItem
            {
                Title = "Write onboarding documentation",
                Description = "Draft the new-hire onboarding guide.",
                Status = DomainTaskStatus.ToDo,
                Priority = TaskPriority.Low,
                DueDate = now.AddDays(14),
                AssignedTo = bob,
                CreatedBy = manager,
                Team = team
            }
        );

        await db.SaveChangesAsync();
    }
}
