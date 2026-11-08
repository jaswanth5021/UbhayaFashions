using System.Security.Cryptography;
using System.Text;

namespace backend.Services;

public static class AuthenticationVersion
{
    public static string Create(string? passwordHash, string key) =>
        Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(passwordHash ?? "external")));
    public static bool Matches(string? supplied, string? passwordHash, string key) =>
        supplied is not null && CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(supplied), Encoding.UTF8.GetBytes(Create(passwordHash, key)));
}
