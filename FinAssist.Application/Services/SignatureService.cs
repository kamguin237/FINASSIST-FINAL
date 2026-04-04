using FinAssist.Core.DTOs.Signature;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;

namespace FinAssist.Application.Services;

public class SignatureService(
    ISignatureRepository signatureRepo,
    IBesoinsRepository besoinsRepo,
    IHashingService hashingService,
    ISignatureConfig signatureConfig,
    INotificationService notificationService,
    IPdfSignatureService pdfSignatureService) : ISignatureService
{
    private string SignatureSecret => signatureConfig.Secret;

    public async Task<SignatureDTO> SignerAsync(int documentId, int utilisateurId)
    {
        var document = await besoinsRepo.GetDocumentByIdAsync(documentId)
            ?? throw new KeyNotFoundException($"Document {documentId} introuvable.");
        return await SignerDocumentAsync(document, utilisateurId);
    }

    public async Task<SignatureDTO> SignerParBesoinAsync(int besoinId, int utilisateurId, SignerBesoinDTO? dto = null)
    {
        var documents = await besoinsRepo.GetDocumentsAsync(besoinId);
        var document = documents.FirstOrDefault()
            ?? throw new InvalidOperationException("Ce besoin n'a pas de pièce jointe.");
        return await SignerDocumentAsync(document, utilisateurId, dto);
    }

    private async Task<SignatureDTO> SignerDocumentAsync(Document document, int utilisateurId, SignerBesoinDTO? dto = null)
    {
        var besoin = await besoinsRepo.GetByIdAsync(document.BesoinId)
            ?? throw new KeyNotFoundException("Besoin associé introuvable.");

        if (!besoin.Statut.StartsWith("APPROUVE_ROLE", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Seuls les besoins approuvés peuvent être signés. Statut actuel : {besoin.Statut}.");

        var ordreStr = besoin.Statut.Replace("APPROUVE_ROLE", "", StringComparison.OrdinalIgnoreCase);
        if (!int.TryParse(ordreStr, out var ordre))
            throw new InvalidOperationException($"Impossible de déterminer l'étape depuis le statut '{besoin.Statut}'.");

        var existante = await signatureRepo.GetByDocumentAndUtilisateurAsync(document.Id, utilisateurId);
        if (existante is not null && existante.Valide)
            throw new InvalidOperationException("Vous avez déjà signé ce document.");

        var empreinte = hashingService.ComputeHash(document.Contenu);
        var horodatage = DateTime.UtcNow;
        var payload = $"{empreinte}|{document.Id}|{utilisateurId}|{horodatage:O}";
        var valeur = hashingService.Sign(payload, SignatureSecret);

        var signature = new SignatureElectronique
        {
            DocumentId = document.Id,
            UtilisateurId = utilisateurId,
            Empreinte = empreinte,
            Valeur = valeur,
            Horodatage = horodatage,
            Valide = true,
            SignatureBase64 = dto?.SignatureBase64,
            PositionX = dto?.PositionX,
            PositionY = dto?.PositionY,
            Largeur = dto?.Largeur,
            Hauteur = dto?.Hauteur,
            // PDF déjà signé côté frontend (pdf-lib) — stocké directement
            PdfSigne = dto?.PdfSigneBase64 is not null
                ? Convert.FromBase64String(dto.PdfSigneBase64)
                : null
        };

        await signatureRepo.AddAsync(signature);

        // Déterminer le statut après signature :
        // si c'est la dernière étape du circuit → TERMINE directement
        // sinon → SIGNE_ROLE{N} (en attente de transmission manuelle)
        var etapeCourante = besoin.Categorie?.WorkflowCircuit?.Etapes
            ?.FirstOrDefault(e => e.Ordre == ordre);

        besoin.Statut = (etapeCourante?.EstDerniereEtape == true)
            ? WorkflowEngine.TERMINE
            : WorkflowEngine.StatutSigne(ordre);
        besoin.DateModification = DateTime.UtcNow;
        await besoinsRepo.UpdateAsync(besoin);

        await besoinsRepo.AddHistoriqueAsync(new Historique
        {
            BesoinId = besoin.Id,
            Action = "SIGNATURE",
            Description = $"Document '{document.Nom}' signé électroniquement ({besoin.Statut}).",
            DateAction = horodatage
        });

        await notificationService.NotifierSignatureAsync(
            besoin.Id,
            besoin.Titre,
            besoin.UtilisateurId,
            besoin.Categorie?.WorkflowCircuit?.Etapes
                ?.FirstOrDefault(e => e.Ordre == ordre)?.RoleRequis ?? "Responsable"
        );
        return ToDTO(await signatureRepo.GetByIdAsync(signature.Id) ?? signature);
    }

    public async Task<VerificationDTO> VerifierAsync(int signatureId)
    {
        var signature = await signatureRepo.GetByIdAsync(signatureId)
            ?? throw new KeyNotFoundException($"Signature {signatureId} introuvable.");

        var empreinteActuelle = hashingService.ComputeHash(signature.Document.Contenu);

        if (empreinteActuelle != signature.Empreinte)
        {
            signature.Valide = false;
            await signatureRepo.UpdateAsync(signature);
            return new VerificationDTO
            {
                SignatureId = signatureId, Authentique = false,
                Message = "Le document a été modifié après la signature. Signature invalide.",
                Horodatage = signature.Horodatage,
                SignataireNom = $"{signature.Utilisateur.Prenom} {signature.Utilisateur.Nom}"
            };
        }

        var payload = $"{signature.Empreinte}|{signature.DocumentId}|{signature.UtilisateurId}|{signature.Horodatage:O}";
        var authentique = hashingService.Verify(payload, signature.Valeur, SignatureSecret);

        return new VerificationDTO
        {
            SignatureId = signatureId,
            Authentique = authentique && signature.Valide,
            Message = authentique && signature.Valide ? "Signature authentique et document intègre." : "Signature invalide ou révoquée.",
            Horodatage = signature.Horodatage,
            SignataireNom = $"{signature.Utilisateur.Prenom} {signature.Utilisateur.Nom}"
        };
    }

    public async Task<SignatureApercuDTO> GetApercuAsync(int besoinId)
    {
        var sig = await signatureRepo.GetByBesoinIdAsync(besoinId)
            ?? throw new KeyNotFoundException($"Aucune signature trouvée pour le besoin {besoinId}.");

        return new SignatureApercuDTO
        {
            Id = sig.Id,
            SignatureBase64 = sig.SignatureBase64,
            PositionX = sig.PositionX,
            PositionY = sig.PositionY,
            Largeur = sig.Largeur,
            Hauteur = sig.Hauteur,
            Horodatage = sig.Horodatage,
            Empreinte = sig.Empreinte,
            Valide = sig.Valide,
            Signataire = new SignataireInfoDTO
            {
                Nom = sig.Utilisateur?.Nom ?? string.Empty,
                Prenom = sig.Utilisateur?.Prenom ?? string.Empty,
                Role = sig.Utilisateur?.Role?.Code ?? string.Empty
            }
        };
    }

    public async Task<(byte[] pdfBytes, string nomFichier)> GenererDocumentSigneAsync(int signatureId)
    {
        var sig = await signatureRepo.GetByIdAsync(signatureId)
            ?? throw new KeyNotFoundException($"Signature {signatureId} introuvable.");

        if (sig.Document?.Contenu is null)
            throw new InvalidOperationException("Document introuvable.");

        // PDF déjà signé côté frontend — retour direct sans incrustation backend
        if (sig.PdfSigne is { Length: > 0 })
        {
            Console.WriteLine($"[FINASSIST][SignatureService] Retour PDF pré-signé: signatureId={signatureId}, bytes={sig.PdfSigne.Length}");
            return (sig.PdfSigne, $"document_signe_{signatureId}.pdf");
        }

        // Fallback : incrustation backend (ancienne méthode)
        if (string.IsNullOrEmpty(sig.SignatureBase64))
        {
            Console.WriteLine($"[FINASSIST][SignatureService] Aucune signature à incrusters, retour document original: signatureId={signatureId}");
            return (sig.Document.Contenu, $"document_signe_{signatureId}.pdf");
        }

        try
        {
            var pdfBytes = pdfSignatureService.IncrusterSignature(
                sig.Document.Contenu,
                sig.SignatureBase64,
                sig.PositionX ?? 10,
                sig.PositionY ?? 80,
                sig.Largeur ?? 200,
                sig.Hauteur ?? 80,
                $"{sig.Utilisateur?.Prenom} {sig.Utilisateur?.Nom}",
                sig.Utilisateur?.Role?.Code ?? string.Empty,
                sig.Horodatage
            );
            return (pdfBytes, $"document_signe_{signatureId}.pdf");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erreur lors de l'incrustation: {ex.Message}");
        }
    }

    private static SignatureDTO ToDTO(SignatureElectronique s) => new()
    {
        Id = s.Id,
        Valeur = s.Valeur,
        Empreinte = s.Empreinte,
        Horodatage = s.Horodatage,
        Valide = s.Valide,
        UtilisateurId = s.UtilisateurId,
        SignataireNom = s.Utilisateur is not null ? $"{s.Utilisateur.Prenom} {s.Utilisateur.Nom}" : string.Empty,
        DocumentId = s.DocumentId,
        DocumentNom = s.Document?.Nom ?? string.Empty
    };
}
