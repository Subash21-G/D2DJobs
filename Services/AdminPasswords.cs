using System.Security.Cryptography;
using System.Text;
using JobForFresher.Models;
using Microsoft.AspNetCore.Identity;
namespace JobForFresher.Services;
public static class AdminPasswords
{
    private static readonly PasswordHasher<AdminUser> Hasher = new();
    public static string Hash(AdminUser user, string password) => Hasher.HashPassword(user, password);
    public static PasswordVerificationResult Verify(AdminUser user, string password)
    {
        try
        {
            var bytes = Convert.FromBase64String(user.PasswordHash);
            // Existing installations used a 32-byte SHA-256 hash. Upgrade only after a valid login.
            if (bytes.Length == 32)
                return CryptographicOperations.FixedTimeEquals(bytes, SHA256.HashData(Encoding.UTF8.GetBytes(password)))
                    ? PasswordVerificationResult.SuccessRehashNeeded : PasswordVerificationResult.Failed;
            return Hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        }
        catch (FormatException) { return PasswordVerificationResult.Failed; }
    }
}