using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;
using TravelPlanner.Application.Exceptions;
using TravelPlanner.Infrastructure.Security;
using Xunit;

namespace TravelPlanner.Api.Tests.Application.Security;

public sealed class Argon2PasswordHasherTests
{
    [Fact]
    public void Hash_creates_a_verifiable_Argon2id_hash()
    {
        var hasher = new Argon2PasswordHasher();

        var hash = hasher.Hash("correct horse battery staple");

        Assert.True(hasher.Verify("correct horse battery staple", hash));
        Assert.False(hasher.Verify("incorrect password", hash));
    }

    [Fact]
    public void Hash_uses_a_unique_salt_for_each_hash()
    {
        var hasher = new Argon2PasswordHasher();

        var firstHash = hasher.Hash("correct horse battery staple");
        var secondHash = hasher.Hash("correct horse battery staple");

        Assert.NotEqual(firstHash, secondHash);
    }
}
