using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskManagement.Application.DTOs;

namespace TaskManagement.IntegrationTests.Fixtures;

public static class TestAuthHelper
{
    public static async Task<HttpClient> LoginAsAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password }, TestJsonOptions.Default);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(TestJsonOptions.Default);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }
}
