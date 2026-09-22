using System.Net;
using System.Net.Http.Json;
using TaskManagement.Application.DTOs;
using TaskManagement.Domain.Enums;
using TaskManagement.IntegrationTests.Fixtures;
using Xunit;

namespace TaskManagement.IntegrationTests.Controllers;

public class TasksControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TasksControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task User_CannotCreateTask()
    {
        var client = await TestAuthHelper.LoginAsAsync(_factory.CreateClient(), "alice@taskflow.com", "User@123");

        var response = await client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest { Title = "Should be forbidden" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Manager_CanCreateTaskForOwnTeam()
    {
        var client = await TestAuthHelper.LoginAsAsync(_factory.CreateClient(), "manager@taskflow.com", "Manager@123");

        var teams = await client.GetFromJsonAsync<List<TeamDto>>("/api/teams", TestJsonOptions.Default);
        var team = Assert.Single(teams!);
        var alice = team.Members.First(m => m.Email == "alice@taskflow.com");

        var response = await client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest
        {
            Title = "New integration task",
            TeamId = team.Id,
            AssignedToId = alice.Id
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TaskDto>(TestJsonOptions.Default);
        Assert.Equal("New integration task", created!.Title);
        Assert.Equal(TaskItemStatus.ToDo, created.Status);
    }

    [Fact]
    public async Task User_CanUpdateStatus_OfOwnAssignedTask()
    {
        var adminClient = await TestAuthHelper.LoginAsAsync(_factory.CreateClient(), "admin@taskflow.com", "Admin@123");
        var aliceId = await GetUserIdAsync(adminClient, "alice@taskflow.com");
        var aliceTasks = await adminClient.GetFromJsonAsync<List<TaskDto>>("/api/tasks?assignedToId=" + aliceId, TestJsonOptions.Default);
        var task = aliceTasks!.First(t => t.Status != TaskItemStatus.Done);

        var aliceClient = await TestAuthHelper.LoginAsAsync(_factory.CreateClient(), "alice@taskflow.com", "User@123");

        var response = await aliceClient.PatchAsJsonAsync($"/api/tasks/{task.Id}/status", new UpdateTaskStatusRequest { Status = TaskItemStatus.InProgress });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task User_CannotUpdateStatus_OfTaskAssignedToSomeoneElse()
    {
        var adminClient = await TestAuthHelper.LoginAsAsync(_factory.CreateClient(), "admin@taskflow.com", "Admin@123");
        var bobId = await GetUserIdAsync(adminClient, "bob@taskflow.com");
        var bobTasks = await adminClient.GetFromJsonAsync<List<TaskDto>>("/api/tasks?assignedToId=" + bobId, TestJsonOptions.Default);
        var task = bobTasks!.First();

        var aliceClient = await TestAuthHelper.LoginAsAsync(_factory.CreateClient(), "alice@taskflow.com", "User@123");

        var response = await aliceClient.PatchAsJsonAsync($"/api/tasks/{task.Id}/status", new UpdateTaskStatusRequest { Status = TaskItemStatus.Done });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetTasks_FilteredByStatus_ReturnsOnlyMatchingTasks()
    {
        var client = await TestAuthHelper.LoginAsAsync(_factory.CreateClient(), "admin@taskflow.com", "Admin@123");

        var tasks = await client.GetFromJsonAsync<List<TaskDto>>("/api/tasks?status=Done", TestJsonOptions.Default);

        Assert.NotNull(tasks);
        Assert.All(tasks!, t => Assert.Equal(TaskItemStatus.Done, t.Status));
    }

    private static async Task<Guid> GetUserIdAsync(HttpClient adminClient, string email)
    {
        var users = await adminClient.GetFromJsonAsync<List<UserDto>>("/api/users", TestJsonOptions.Default);
        return users!.First(u => u.Email == email).Id;
    }
}
