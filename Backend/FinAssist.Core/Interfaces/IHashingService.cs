namespace FinAssist.Core.Interfaces;

public interface IHashingService
{
    /// <summary>Calcule l'empreinte SHA-256 d'un contenu binaire (hex).</summary>
    string ComputeHash(byte[] data);

    /// <summary>Génère une signature HMAC-SHA256 à partir d'une empreinte et d'une clé secrète (base64).</summary>
    string Sign(string empreinte, string secret);

    /// <summary>Vérifie qu'une signature HMAC-SHA256 correspond à l'empreinte et à la clé.</summary>
    bool Verify(string empreinte, string valeurSignature, string secret);
}
