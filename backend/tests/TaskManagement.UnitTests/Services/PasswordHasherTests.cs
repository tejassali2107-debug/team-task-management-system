using TaskManagement.Infrastructure.Identity;
using Xunit;

namespace TaskManagement.UnitTests.Services;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ThenVerify_WithCorrectPassword_Succeeds()
    {
        var hash = _hasher.Hash("Sup3rSecret!");

        var result = _hasher.Verify(hash, "Sup3rSecret!");

        Assert.True(result);
    }

    [Fact]
    public void Verify_WithWrongPassword_Fails()
    {
        var hash = _hasher.Hash("Sup3rSecret!");

        var result = _hasher.Verify(hash, "WrongPassword");

        Assert.False(result);
    }

    [Fact]
    public void Hash_ProducesDifferentOutputForSamePassword()
    {
        var hash1 = _hasher.Hash("Sup3rSecret!");
        var hash2 = _hasher.Hash("Sup3rSecret!");

        Assert.NotEqual(hash1, hash2);
    }
}
