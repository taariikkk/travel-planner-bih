using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;
using TravelPlanner.Application.Exceptions;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace TravelPlanner.Infrastructure.Security;

public sealed class Argon2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int MemorySize = 65_536;
    private const int Iterations = 3;
    private const int Parallelism = 4;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = DeriveKey(password, salt, MemorySize, Iterations, Parallelism);

        return $"argon2id$v=1$m={MemorySize},t={Iterations},p={Parallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        if (!TryParse(passwordHash, out var salt, out var hash, out var memorySize, out var iterations, out var parallelism))
        {
            return false;
        }

        var candidateHash = DeriveKey(password, salt, memorySize, iterations, parallelism);
        return CryptographicOperations.FixedTimeEquals(candidateHash, hash);
    }

    private static byte[] DeriveKey(string password, byte[] salt, int memorySize, int iterations, int parallelism)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memorySize,
            Iterations = iterations,
            DegreeOfParallelism = parallelism
        };

        return argon2.GetBytes(HashSize);
    }

    private static bool TryParse(
        string passwordHash,
        out byte[] salt,
        out byte[] hash,
        out int memorySize,
        out int iterations,
        out int parallelism)
    {
        salt = [];
        hash = [];
        memorySize = 0;
        iterations = 0;
        parallelism = 0;

        var parts = passwordHash.Split('$');
        if (parts.Length != 5 || parts[0] != "argon2id" || parts[1] != "v=1")
        {
            return false;
        }

        var parameters = parts[2].Split(',');
        if (parameters.Length != 3 ||
            !TryReadParameter(parameters[0], "m", out memorySize) ||
            !TryReadParameter(parameters[1], "t", out iterations) ||
            !TryReadParameter(parameters[2], "p", out parallelism) ||
            memorySize <= 0 || iterations <= 0 || parallelism <= 0)
        {
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(parts[3]);
            hash = Convert.FromBase64String(parts[4]);
            return salt.Length == SaltSize && hash.Length == HashSize;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryReadParameter(string parameter, string name, out int value)
    {
        value = 0;
        var prefix = $"{name}=";
        return parameter.StartsWith(prefix, StringComparison.Ordinal) &&
            int.TryParse(parameter[prefix.Length..], out value);
    }
}
