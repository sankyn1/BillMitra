using BillMitra.Application.Services;
using FluentAssertions;
using Xunit;

namespace BillMitra.UnitTests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_GeneratesSaltedHash()
    {
        var password = "SecureAdminPassword!123";
        var hash = _hasher.HashPassword(password);

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().Contain(":");
        var parts = hash.Split(':');
        parts.Length.Should().Be(3);
        parts[2].Should().Be("100000"); // Iterations
    }

    [Fact]
    public void VerifyPassword_MatchesValidPassword()
    {
        var password = "SecureAdminPassword!123";
        var hash = _hasher.HashPassword(password);

        var isValid = _hasher.VerifyPassword(password, hash);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_RejectsIncorrectPassword()
    {
        var hash = _hasher.HashPassword("CorrectPassword123");

        var isValid = _hasher.VerifyPassword("WrongPassword456", hash);
        isValid.Should().BeFalse();
    }
}

