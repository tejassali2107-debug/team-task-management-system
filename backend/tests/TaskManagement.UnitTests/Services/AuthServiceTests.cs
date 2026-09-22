using Microsoft.Extensions.Options;
using TaskManagement.Application.DTOs;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Identity;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Services;
using TaskManagement.UnitTests.TestHelpers;
using Xunit;

namespace TaskManagement.UnitTests.Services;

public class AuthServiceTests
{
    private static AuthService CreateService(AppDbContext db)
    {
        var passwordHasher = new PasswordHasher();
        var jwtService = new JwtTokenService(Options.Create(new JwtSettings
        {
            Key = "test-signing-key-that-is-at-least-32-bytes-long",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = 60
        }));

        return new AuthService(db, passwordHasher, jwtService);
    }

    [Fact]
    public async Task RegisterAsync_WithNewEmail_CreatesUserWithUserRole()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);

        var result = await service.RegisterAsync(new RegisterRequest
        {
            FullName = "New Person",
            Email = "new.person@taskflow.com",
            Password = "Password123"
        });

        Assert.True(result.Succeeded);
        Assert.Equal(UserRole.User, result.Data!.User.Role);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_Fails()
    {
        using var db = TestDbContextFactory.Create();
        var passwordHasher = new PasswordHasher();
        db.Users.Add(new User
        {
            FullName = "Existing",
            Email = "existing@taskflow.com",
            PasswordHash = passwordHasher.Hash("whatever"),
            Role = UserRole.User
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.RegisterAsync(new RegisterRequest
        {
            FullName = "Duplicate",
            Email = "existing@taskflow.com",
            Password = "Password123"
        });

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_Fails()
    {
        using var db = TestDbContextFactory.Create();
        var passwordHasher = new PasswordHasher();
        db.Users.Add(new User
        {
            FullName = "Someone",
            Email = "someone@taskflow.com",
            PasswordHash = passwordHasher.Hash("CorrectPassword"),
            Role = UserRole.User
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = "someone@taskflow.com",
            Password = "WrongPassword"
        });

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task LoginAsync_WithCorrectCredentials_ReturnsToken()
    {
        using var db = TestDbContextFactory.Create();
        var passwordHasher = new PasswordHasher();
        db.Users.Add(new User
        {
            FullName = "Someone",
            Email = "someone2@taskflow.com",
            PasswordHash = passwordHasher.Hash("CorrectPassword"),
            Role = UserRole.Admin
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = "someone2@taskflow.com",
            Password = "CorrectPassword"
        });

        Assert.True(result.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(result.Data!.Token));
    }
}
