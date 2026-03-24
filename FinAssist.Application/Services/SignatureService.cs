using FinAssist.Core.DTOs.Signature;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;

namespace FinAssist.Application.Services;

public class SignatureService(
    ISignatureRepository signatureRepo,
    IBesoinsRepository besoinsRepo,
    IHashingService hashingService,
    ISignatureConfig signatureConfig,
    INotificationService notificationService) : ISignatureService
{
    private string SignatureSecret => signatureConfig.Secret;

    public async Task<SignatureDTO> SignerAsync(int documentId, int utilisateurId)
    {
        // Vérifier que le document existe
        var document = await besoinsRepo.GetDocumentByIdAsync(documentId)
            ?? throw new KeyNotFoundException($"Document {documentId} introuvable.");

        return await SignerDocumentAsync(document, utilisateurId);
    }

    public async Task<SignatureDTO> SignerParBesoinAsync(int besoinId, int utilisateurId)
    {
        var documents = await besoinsRepo.GetDocumentsAsync(besoinId);
        var document = documents.FirstOrDefault()
            ?? throw new InvalidOperationException(
                "Ce besoin n'a pas de pièce jointe. Seuls les besoins avec document peuvent être signés.");

        return await SignerDocumentAsync(document, utilisateurId);
    }

    private async Task<SignatureDTO> SignerDocumentAsync(Document document, int utilisateurId)
    {
        var besoin = await besoinsRepo.GetByIdAsync(document.BesoinId)
            ?? throw new KeyNotFoundException("Besoin associé introuvable.");

        // Le besoin doit être dans un statut APPROUVE_ROLE{N} pour être signé
        if (!besoin.Statut.StartsWith("APPROUVE_ROLE", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Seuls les besoins approuvés peuvent être signés. Statut actuel : {besoin.Statut}.");

        // Extraire l'ordre de l'étape depuis le statut (ex: APPROUVE_ROLE2 → 2)
        var ordreStr = besoin.Statut.Replace("APPROUVE_ROLE", "", StringComparison.OrdinalIgnoreCase);
        if (!int.TryParse(ordreStr, out var ordre))
            throw new InvalidOperationException($"Impossible de déterminer l'étape depuis le statut '{besoin.Statut}'.");

        var statutSigne = WorkflowEngine.StatutSigne(ordre);
        // Vérifier qu'une signature valide n'existe pas déjà
        // Vérifier que cet utilisateur n'a pas déjà signé ce document à cette étape
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
            Valide = true
        };

        await signatureRepo.AddAsync(signature);

        besoin.Statut = statutSigne;
        besoin.DateModification = DateTime.UtcNow;
        await besoinsRepo.UpdateAsync(besoin);

        await besoinsRepo.AddHistoriqueAsync(new Historique
        {
            BesoinId = besoin.Id,
            Action = "SIGNATURE",
            Description = $"Document '{document.Nom}' signé électroniquement ({statutSigne}).",
            DateAction = horodatage
        });

        await notificationService.NotifierSignatureAsync(besoin.Id, besoin.UtilisateurId);

        return ToDTO(await signatureRepo.GetByIdAsync(signature.Id) ?? signature);
    }

    public async Task<VerificationDTO> VerifierAsync(int signatureId)
    {
        var signature = await signatureRepo.GetByIdAsync(signatureId)
            ?? throw new KeyNotFoundException($"Signature {signatureId} introuvable.");

        // Recalcul de l'empreinte du document actuel
        var empreinteActuelle = hashingService.ComputeHash(signature.Document.Contenu);

        // Vérification 1 : intégrité du document (empreinte inchangée)
        if (empreinteActuelle != signature.Empreinte)
        {
            signature.Valide = false;
            await signatureRepo.UpdateAsync(signature);
            return new VerificationDTO
            {
                SignatureId = signatureId,
                Authentique = false,
                Message = "Le document a été modifié après la signature. Signature invalide.",
                Horodatage = signature.Horodatage,
                SignataireNom = $"{signature.Utilisateur.Prenom} {signature.Utilisateur.Nom}"
            };
        }

        // Vérification 2 : authenticité de la valeur HMAC
        var payload = $"{signature.Empreinte}|{signature.DocumentId}|{signature.UtilisateurId}|{signature.Horodatage:O}";
        var authentique = hashingService.Verify(payload, signature.Valeur, SignatureSecret);

        return new VerificationDTO
        {
            SignatureId = signatureId,
            Authentique = authentique && signature.Valide,
            Message = authentique && signature.Valide
                ? "Signature authentique et document intègre."
                : "Signature invalide ou révoquée.",
            Horodatage = signature.Horodatage,
            SignataireNom = $"{signature.Utilisateur.Prenom} {signature.Utilisateur.Nom}"
        };
    }

    private static SignatureDTO ToDTO(SignatureElectronique s) => new()
    {
        Id = s.Id,
        Valeur = s.Valeur,
        Empreinte = s.Empreinte,
        Horodatage = s.Horodatage,
        Valide = s.Valide,
        UtilisateurId = s.UtilisateurId,
        SignataireNom = s.Utilisateur is not null
            ? $"{s.Utilisateur.Prenom} {s.Utilisateur.Nom}"
            : string.Empty,
        DocumentId = s.DocumentId,
        DocumentNom = s.Document?.Nom ?? string.Empty
    };
}
