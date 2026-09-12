namespace Users.API.Services;

using System.Security.Cryptography;
using System.Text;


public static class Password
{
    public static bool IsValid(string? value) => !string.IsNullOrWhiteSpace(value);

    // Temporary stand-in until we decide the real hasher. Never store the raw password.
    public static string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public static bool Matches(string password, string passwordHash)
        => Hash(password) == passwordHash;
}
