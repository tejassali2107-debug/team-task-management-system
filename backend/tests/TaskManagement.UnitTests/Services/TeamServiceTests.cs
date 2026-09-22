using TaskManagement.Application.DTOs;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Services;
using TaskManagement.UnitTests.TestHelpers;
using Xunit;

namespace TaskManagement.UnitTests.Services;

public class TeamServiceTests
{
    private static TeamService BuildService(AppDbContext db, FakeCurrentUserService currentUser)
    {
        var emailSender = new FakeEmailSender();
        var notificationService = new NotificationService(db, currentUser, emailSender);
        return new TeamService(db, currentUser, notificationService);
    }

    [Fact]
    public async Task AddMemberAsync_ManagerAddingToOwnTeam_Succeeds()
    {
        using var db = TestDbContextFactory.Create();

        var manager = new User { FullName = "Manager", Email = "manager@x.com", PasswordHash = "x", Role = UserRole.Manager };
        var newUser = new User { FullName = "New User", Email = "newuser@x.com", PasswordHash = "x", Role = UserRole.User };
        db.Users.AddRange(manager, newUser);
        await db.SaveChangesAsync();

        var team = new Team { Name = "Team A", ManagerId = manager.Id };
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        var currentUser = new FakeCurrentUserService { UserId = manager.Id, Role = UserRole.Manager };
        var service = BuildService(db, currentUser);

        var result = await service.AddMemberAsync(team.Id, new AddTeamMemberRequest { UserId = newUser.Id });

        Assert.True(result.Succeeded);
        Assert.Contains(result.Data!.Members, m => m.Id == newUser.Id);
    }

    [Fact]
    public async Task AddMemberAsync_ManagerAddingToOtherManagersTeam_Fails()
    {
        using var db = TestDbContextFactory.Create();

        var managerA = new User { FullName = "Manager A", Email = "ma@x.com", PasswordHash = "x", Role = UserRole.Manager };
        var managerB = new User { FullName = "Manager B", Email = "mb@x.com", PasswordHash = "x", Role = UserRole.Manager };
        var newUser = new User { FullName = "New User", Email = "newuser2@x.com", PasswordHash = "x", Role = UserRole.User };
        db.Users.AddRange(managerA, managerB, newUser);
        await db.SaveChangesAsync();

        var teamB = new Team { Name = "Team B", ManagerId = managerB.Id };
        db.Teams.Add(teamB);
        await db.SaveChangesAsync();

        var currentUser = new FakeCurrentUserService { UserId = managerA.Id, Role = UserRole.Manager };
        var service = BuildService(db, currentUser);

        var result = await service.AddMemberAsync(teamB.Id, new AddTeamMemberRequest { UserId = newUser.Id });

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task CreateAsync_WithNonManagerAsManagerId_Fails()
    {
        using var db = TestDbContextFactory.Create();

        var regularUser = new User { FullName = "Regular", Email = "regular@x.com", PasswordHash = "x", Role = UserRole.User };
        db.Users.Add(regularUser);
        await db.SaveChangesAsync();

        var currentUser = new FakeCurrentUserService { Role = UserRole.Admin };
        var service = BuildService(db, currentUser);

        var result = await service.CreateAsync(new CreateTeamRequest { Name = "New Team", ManagerId = regularUser.Id });

        Assert.False(result.Succeeded);
    }
}
