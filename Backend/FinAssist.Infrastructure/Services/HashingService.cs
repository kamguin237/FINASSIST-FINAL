using System.Security.Cryptography;
using System.Text;
using FinAssist.Core.Interfaces;

namespace FinAssist.Infrastructure.Services;

public class HashingService : IHashingService
{
    public string ComputeHash(byte[] data)
        => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

    public string Sign(string empreinte, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var dataBytes = Encoding.UTF8.GetBytes(empreinte);
        var hmac = HMACSHA256.HashData(keyBytes, dataBytes);
        return Convert.ToBase64String(hmac);
    }

    public bool Verify(string empreinte, string valeurSignature, string secret)
    {
        var expected = Sign(empreinte, secret);
        // Comparaison à temps constant pour éviter les timing attacks
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(expected),
            Convert.FromBase64String(valeurSignature));
    }
}
