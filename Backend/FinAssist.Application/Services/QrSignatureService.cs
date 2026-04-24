using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FinAssist.Core.DTOs.Signatures;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FinAssist.Application.Services;

public class QrSignatureService(
    ISignatureUtilisateurRepository repo,
    IConfiguration config)
{
    private string Secret => config["Signature:Secret"] ?? "finassist_qr_secret_key_32chars!";

    public async Task<QrSignatureResponseDTO> GenerateAsync(int utilisateurId, string nom, string prenom, string role)
    {
        var now        = DateTime.UtcNow;
        var expiration = now.AddYears(1);

        // Payload JSON
        var payload = new
        {
            userId    = utilisateurId,
            nom, prenom, role,
            iat       = now.ToString("o"),
            exp       = expiration.ToString("o")
        };
        var payloadJson  = JsonSerializer.Serialize(payload);
        var payloadB64   = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson));

        // HMAC-SHA256
        var hmac      = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadB64)));
        var sigShort  = signature.Replace("+", "").Replace("/", "").Replace("=", "")[..8].ToUpperInvariant();

        // Token lisible : FA-{année}-{initiales}-{hash8}
        var initiales = $"{prenom[0]}{nom[0]}".ToUpperInvariant();
        var token     = $"FA-{now.Year}-{initiales}-{sigShort}";
        var fullToken = $"{payloadB64}.{Convert.ToBase64String(Encoding.UTF8.GetBytes(signature))}";

        // Stocker en base (réutilise SignatureUtilisateur)
        var sig = new SignatureUtilisateur
        {
            UtilisateurId   = utilisateurId,
            Type            = "qrcode",
            ImageBase64     = fullToken,   // on stocke le token complet
            Police          = token,       // token court lisible
            DateCreation    = now,
            DateModification = now
        };
        await repo.SaveAsync(sig);

        var baseUrl = config["App:BaseUrl"] ?? "http://localhost:8080";
        return new QrSignatureResponseDTO
        {
            Token          = token,
            UrlVerification = $"{baseUrl}/api/signatures/qr/verify/{Uri.EscapeDataString(fullToken)}",
            Nom            = nom,
            Prenom         = prenom,
            Role           = role,
            GenereLe       = now,
            Expiration     = expiration,
            Valide         = true
        };
    }

    public async Task<QrVerificationResultDTO> VerifyAsync(int utilisateurId)
    {
        var sig = await repo.GetByUtilisateurIdAsync(utilisateurId);
        if (sig is null || sig.Type != "qrcode")
            return new QrVerificationResultDTO { Valide = false, Statut = "invalide", Message = "Aucune signature QR trouvée." };

        return DecodeToken(sig.ImageBase64);
    }

    public QrVerificationResultDTO VerifyToken(string fullToken)
        => DecodeToken(fullToken);

    private QrVerificationResultDTO DecodeToken(string fullToken)
    {
        try
        {
            var parts = fullToken.Split('.');
            if (parts.Length != 2)
                return Invalid("Format de token invalide.");

            var payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
            var doc         = JsonDocument.Parse(payloadJson);

            var exp = DateTime.Parse(doc.RootElement.GetProperty("exp").GetString()!).ToUniversalTime();
            if (exp < DateTime.UtcNow)
                return new QrVerificationResultDTO { Valide = false, Statut = "expire", Message = "Token expiré." };

            // Vérifier HMAC
            var storedSig = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
            var hmac      = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
            var expected  = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(parts[0])));
            if (storedSig != expected)
                return Invalid("Signature invalide.");

            return new QrVerificationResultDTO
            {
                Valide     = true,
                Statut     = "valide",
                Nom        = doc.RootElement.GetProperty("nom").GetString(),
                Prenom     = doc.RootElement.GetProperty("prenom").GetString(),
                Role       = doc.RootElement.GetProperty("role").GetString(),
                GenereLe   = DateTime.Parse(doc.RootElement.GetProperty("iat").GetString()!),
                Expiration = exp,
                Message    = "Signature valide."
            };
        }
        catch { return Invalid("Erreur de décodage."); }
    }

    private static QrVerificationResultDTO Invalid(string msg)
        => new() { Valide = false, Statut = "invalide", Message = msg };
}
