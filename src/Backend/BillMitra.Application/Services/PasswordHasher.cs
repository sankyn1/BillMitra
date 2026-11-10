using System.Security.Cryptography;
using BillMitra.Application.Common.Interfaces;

namespace BillMitra.Application.Services;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 128 bit
    private const int KeySize = 32;  // 256 bit
    private const int Iterations = 100000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;
    private const char SegmentDelimiter = ':';

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

        return string.Join(SegmentDelimiter,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash),
            Iterations);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
            return false;

        string[] segments = passwordHash.Split(SegmentDelimiter);
        if (segments.Length != 3)
            return false;

        try
        {
            byte[] salt = Convert.FromBase64String(segments[0]);
            byte[] expectedHash = Convert.FromBase64String(segments[1]);
            int iterations = int.Parse(segments[2]);

            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expectedHash.Length);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch
        {
            return false;
        }
    }
}
