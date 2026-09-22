using System.Net;
using System.Net.Http.Json;
using TaskManagement.Application.DTOs;
using TaskManagement.IntegrationTests.Fixtures;
using Xunit;

namespace TaskManagement.IntegrationTests.Controllers;

public class AuthFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthFlowTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ThenLogin_Succeeds()
    {
        var register = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = "Integration Test",
            Email = "integration.test@taskflow.com",
            Password = "Password123"
        });

        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "integration.test@taskflow.com",
            Password = "Password123"
        });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var body = await login.Content.ReadFromJsonAsync<AuthResponse>(TestJsonOptions.Default);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "admin@taskflow.com",
            Password = "WrongPassword"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
