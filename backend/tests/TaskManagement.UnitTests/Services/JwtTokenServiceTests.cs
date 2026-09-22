using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Identity;
using Xunit;

namespace TaskManagement.UnitTests.Services;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _service;

    public JwtTokenServiceTests()
    {
        var settings = new JwtSettings
        {
            Key = "test-signing-key-that-is-at-least-32-bytes-long",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = 30
        };

        _service = new JwtTokenService(Options.Create(settings));
    }

    [Fact]
    public void GenerateToken_IncludesUserIdEmailAndRoleClaims()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@taskflow.com",
            FullName = "Test User",
            Role = UserRole.Manager
        };

        var (token, expiresAt) = _service.GenerateToken(user);

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(expiresAt > DateTime.UtcNow);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        Assert.Contains(jwt.Claims, c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "Manager");
    }

    [Fact]
    public void GenerateToken_ExpiresAtMatchesConfiguredMinutes()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "a@b.com", FullName = "A", Role = UserRole.User };

        var (_, expiresAt) = _service.GenerateToken(user);

        var expectedExpiry = DateTime.UtcNow.AddMinutes(30);
        Assert.True((expectedExpiry - expiresAt).Duration() < TimeSpan.FromSeconds(5));
    }
}
