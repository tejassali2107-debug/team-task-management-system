using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Services;
using TaskManagement.UnitTests.TestHelpers;
using Xunit;

namespace TaskManagement.UnitTests.Services;

public class NotificationServiceTests
{
    [Fact]
    public async Task NotifyTaskAssignedAsync_CreatesNotificationAndSendsMockEmail()
    {
        using var db = TestDbContextFactory.Create();

        var assignee = new User { FullName = "Assignee", Email = "assignee@x.com", PasswordHash = "x", Role = UserRole.User };
        var creator = new User { FullName = "Creator", Email = "creator@x.com", PasswordHash = "x", Role = UserRole.Manager };
        db.Users.AddRange(assignee, creator);
        await db.SaveChangesAsync();

        var task = new TaskItem { Title = "New Task", AssignedToId = assignee.Id, CreatedById = creator.Id };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        var emailSender = new FakeEmailSender();
        var currentUser = new FakeCurrentUserService { UserId = creator.Id, Role = UserRole.Manager };
        var service = new NotificationService(db, currentUser, emailSender);

        await service.NotifyTaskAssignedAsync(task);

        Assert.Single(db.Notifications);
        Assert.Equal(assignee.Id, db.Notifications.First().UserId);
        Assert.Equal(NotificationType.TaskAssigned, db.Notifications.First().Type);
        Assert.Single(emailSender.SentEmails);
        Assert.Equal(assignee.Email, emailSender.SentEmails[0].To);
    }

    [Fact]
    public async Task NotifyTaskStatusChangedAsync_DoesNotNotifyTheActorThemselves()
    {
        using var db = TestDbContextFactory.Create();

        var assignee = new User { FullName = "Assignee", Email = "assignee2@x.com", PasswordHash = "x", Role = UserRole.User };
        db.Users.Add(assignee);
        await db.SaveChangesAsync();

        var task = new TaskItem
        {
            Title = "Task",
            AssignedToId = assignee.Id,
            CreatedById = assignee.Id,
            Status = TaskItemStatus.InProgress
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        var emailSender = new FakeEmailSender();
        var currentUser = new FakeCurrentUserService { UserId = assignee.Id, Role = UserRole.User };
        var service = new NotificationService(db, currentUser, emailSender);

        await service.NotifyTaskStatusChangedAsync(task, TaskItemStatus.ToDo);

        Assert.Empty(db.Notifications);
        Assert.Empty(emailSender.SentEmails);
    }
}
