using TaskManagement.Application.DTOs;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Services;
using TaskManagement.UnitTests.TestHelpers;
using Xunit;
using DomainTaskStatus = TaskManagement.Domain.Enums.TaskItemStatus;

namespace TaskManagement.UnitTests.Services;

public class TaskServiceTests
{
    private static async Task<(AppDbContext Db, Team TeamA, Team TeamB, User ManagerA, User ManagerB, User UserA1, User UserB1)> SeedAsync()
    {
        var db = TestDbContextFactory.Create();

        var managerA = new User { FullName = "Manager A", Email = "managera@x.com", PasswordHash = "x", Role = UserRole.Manager };
        var managerB = new User { FullName = "Manager B", Email = "managerb@x.com", PasswordHash = "x", Role = UserRole.Manager };
        var userA1 = new User { FullName = "User A1", Email = "usera1@x.com", PasswordHash = "x", Role = UserRole.User };
        var userB1 = new User { FullName = "User B1", Email = "userb1@x.com", PasswordHash = "x", Role = UserRole.User };
        db.Users.AddRange(managerA, managerB, userA1, userB1);
        await db.SaveChangesAsync();

        var teamA = new Team { Name = "Team A", ManagerId = managerA.Id };
        var teamB = new Team { Name = "Team B", ManagerId = managerB.Id };
        db.Teams.AddRange(teamA, teamB);
        await db.SaveChangesAsync();

        managerA.TeamId = teamA.Id;
        userA1.TeamId = teamA.Id;
        managerB.TeamId = teamB.Id;
        userB1.TeamId = teamB.Id;
        await db.SaveChangesAsync();

        return (db, teamA, teamB, managerA, managerB, userA1, userB1);
    }

    private static TaskService BuildService(AppDbContext db, FakeCurrentUserService currentUser)
    {
        var emailSender = new FakeEmailSender();
        var notificationService = new NotificationService(db, currentUser, emailSender);
        return new TaskService(db, currentUser, notificationService);
    }

    [Fact]
    public async Task CreateAsync_ManagerCreatingForOwnTeam_Succeeds()
    {
        var (db, teamA, _, managerA, _, userA1, _) = await SeedAsync();
        var currentUser = new FakeCurrentUserService { UserId = managerA.Id, Role = UserRole.Manager, TeamId = teamA.Id };
        var service = BuildService(db, currentUser);

        var result = await service.CreateAsync(new CreateTaskRequest
        {
            Title = "Team A task",
            AssignedToId = userA1.Id,
            TeamId = teamA.Id
        });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task CreateAsync_ManagerAssigningToOtherTeam_Fails()
    {
        var (db, _, teamB, managerA, _, _, userB1) = await SeedAsync();
        var currentUser = new FakeCurrentUserService { UserId = managerA.Id, Role = UserRole.Manager };
        var service = BuildService(db, currentUser);

        var result = await service.CreateAsync(new CreateTaskRequest
        {
            Title = "Cross-team task",
            AssignedToId = userB1.Id,
            TeamId = teamB.Id
        });

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task GetAllAsync_ManagerOnlySeesOwnTeamTasks()
    {
        var (db, teamA, teamB, managerA, managerB, userA1, userB1) = await SeedAsync();

        db.Tasks.Add(new TaskItem { Title = "A task", TeamId = teamA.Id, AssignedToId = userA1.Id, CreatedById = managerA.Id });
        db.Tasks.Add(new TaskItem { Title = "B task", TeamId = teamB.Id, AssignedToId = userB1.Id, CreatedById = managerB.Id });
        await db.SaveChangesAsync();

        var currentUser = new FakeCurrentUserService { UserId = managerA.Id, Role = UserRole.Manager };
        var service = BuildService(db, currentUser);

        var result = await service.GetAllAsync(new TaskFilterRequest());

        Assert.True(result.Succeeded);
        Assert.Single(result.Data!);
        Assert.Equal("A task", result.Data![0].Title);
    }

    [Fact]
    public async Task GetAllAsync_UserOnlySeesAssignedTasks()
    {
        var (db, teamA, _, managerA, _, userA1, _) = await SeedAsync();

        var other = new User { FullName = "Other", Email = "other@x.com", PasswordHash = "x", Role = UserRole.User, TeamId = teamA.Id };
        db.Users.Add(other);
        await db.SaveChangesAsync();

        db.Tasks.Add(new TaskItem { Title = "Assigned to A1", TeamId = teamA.Id, AssignedToId = userA1.Id, CreatedById = managerA.Id });
        db.Tasks.Add(new TaskItem { Title = "Assigned to Other", TeamId = teamA.Id, AssignedToId = other.Id, CreatedById = managerA.Id });
        await db.SaveChangesAsync();

        var currentUser = new FakeCurrentUserService { UserId = userA1.Id, Role = UserRole.User };
        var service = BuildService(db, currentUser);

        var result = await service.GetAllAsync(new TaskFilterRequest());

        Assert.True(result.Succeeded);
        Assert.Single(result.Data!);
        Assert.Equal("Assigned to A1", result.Data![0].Title);
    }

    [Fact]
    public async Task UpdateStatusAsync_AssignedUser_CanUpdateOwnTask()
    {
        var (db, teamA, _, managerA, _, userA1, _) = await SeedAsync();
        var task = new TaskItem { Title = "My task", TeamId = teamA.Id, AssignedToId = userA1.Id, CreatedById = managerA.Id };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        var currentUser = new FakeCurrentUserService { UserId = userA1.Id, Role = UserRole.User };
        var service = BuildService(db, currentUser);

        var result = await service.UpdateStatusAsync(task.Id, new UpdateTaskStatusRequest { Status = DomainTaskStatus.InProgress });

        Assert.True(result.Succeeded);
        Assert.Equal(DomainTaskStatus.InProgress, result.Data!.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_UnrelatedUser_Fails()
    {
        var (db, teamA, _, managerA, _, userA1, _) = await SeedAsync();
        var other = new User { FullName = "Other", Email = "other2@x.com", PasswordHash = "x", Role = UserRole.User, TeamId = teamA.Id };
        db.Users.Add(other);
        var task = new TaskItem { Title = "My task", TeamId = teamA.Id, AssignedToId = userA1.Id, CreatedById = managerA.Id };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        var currentUser = new FakeCurrentUserService { UserId = other.Id, Role = UserRole.User };
        var service = BuildService(db, currentUser);

        var result = await service.UpdateStatusAsync(task.Id, new UpdateTaskStatusRequest { Status = DomainTaskStatus.Done });

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task GetAllAsync_Admin_SeesTasksAcrossAllTeams()
    {
        var (db, teamA, teamB, managerA, managerB, userA1, userB1) = await SeedAsync();

        db.Tasks.Add(new TaskItem { Title = "A task", TeamId = teamA.Id, AssignedToId = userA1.Id, CreatedById = managerA.Id });
        db.Tasks.Add(new TaskItem { Title = "B task", TeamId = teamB.Id, AssignedToId = userB1.Id, CreatedById = managerB.Id });
        await db.SaveChangesAsync();

        var currentUser = new FakeCurrentUserService { UserId = Guid.NewGuid(), Role = UserRole.Admin };
        var service = BuildService(db, currentUser);

        var result = await service.GetAllAsync(new TaskFilterRequest());

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Data!.Count);
    }
}
