using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FinAssist.Application.Services;

public class ValidationDeadlineService(
    IBesoinsRepository besoinsRepo,
    INotificationService notifService,
    IEmailService emailService,
    ILogService logService,
    ILogger<ValidationDeadlineService> logger)
{
    public async Task ProcessDeadlinesAsync()
    {
        var tousBesoins = (await besoinsRepo.GetAllAsync()).ToList();

        var besoinsEnAttente = tousBesoins.Where(b =>
            WorkflowEngine.EstEnAttente(b.Statut) &&
            !b.RejeteAutomatiquement
        ).ToList();

        logger.LogInformation("[Deadline] Traitement de {Count} besoins en attente", besoinsEnAttente.Count);

        foreach (var besoin in besoinsEnAttente)
        {
            try { await TraiterBesoin(besoin); }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Deadline] Erreur sur besoin {Id}", besoin.Id);
            }
        }
    }

    private async Task TraiterBesoin(Besoin besoin)
    {
        // Récupérer le délai de l'étape courante (en minutes)
        var etape = besoin.Categorie?.WorkflowCircuit?.Etapes
            ?.FirstOrDefault(e => e.Ordre == besoin.EtapeCouranteOrdre);

        if (etape is null) return;

        var delaiMinutes = etape.DelaiMaxJours; // stocké en minutes
        if (delaiMinutes <= 0) return;

        var dateRef = besoin.DateEntreeEnAttente ?? besoin.DateModification;
        var elapsed = (DateTime.UtcNow - dateRef).TotalMinutes;
        var pct = (elapsed / delaiMinutes) * 100;

        // Récupérer les validateurs du rôle requis
        var validateurIds = (await besoinsRepo.GetUtilisateurIdsByRoleAsync(etape.RoleRequis)).ToList();

        // ── 50% → Rappel 1 ────────────────────────────────────────────────────
        if (pct >= 50 && !besoin.Rappel1Envoye)
        {
            var restantMin = delaiMinutes - elapsed;
            var msg = $"Rappel : le besoin « {besoin.Titre} » doit être validé. Il reste {FormatDuree(restantMin)}.";
            foreach (var id in validateurIds)
                await notifService.EnvoyerRappelAsync(id, besoin.Id, besoin.Titre, 1, msg);

            besoin.Rappel1Envoye = true;
            await besoinsRepo.UpdateAsync(besoin);
            logger.LogInformation("[Deadline] Rappel 1 envoyé pour besoin {Id}", besoin.Id);
        }

        // ── 80% → Rappel 2 ────────────────────────────────────────────────────
        if (pct >= 80 && !besoin.Rappel2Envoye)
        {
            var restantMin = delaiMinutes - elapsed;
            var msg = $"Urgent : le besoin « {besoin.Titre} » expire dans {FormatDuree(restantMin)} !";
            foreach (var id in validateurIds)
                await notifService.EnvoyerRappelAsync(id, besoin.Id, besoin.Titre, 2, msg);

            besoin.Rappel2Envoye = true;
            await besoinsRepo.UpdateAsync(besoin);
            logger.LogInformation("[Deadline] Rappel 2 envoyé pour besoin {Id}", besoin.Id);
        }

        // ── 100% → Email ──────────────────────────────────────────────────────
        if (pct >= 100 && !besoin.EmailRappelEnvoye)
        {
            foreach (var id in validateurIds)
            {
                var validateur = await besoinsRepo.GetUtilisateurByIdAsync(id);
                if (validateur is not null)
                    await emailService.EnvoyerRappelDelaiAsync(validateur, besoin);
            }
            besoin.EmailRappelEnvoye = true;
            await besoinsRepo.UpdateAsync(besoin);
            logger.LogInformation("[Deadline] Email rappel envoyé pour besoin {Id}", besoin.Id);
        }

        // ── 100% + 60 min → Rejet automatique ────────────────────────────────
        if (pct >= 100 && elapsed >= delaiMinutes + 60 && !besoin.RejeteAutomatiquement)
        {
            await RejeterAutomatiquement(besoin, etape);
        }
    }

    private async Task RejeterAutomatiquement(Besoin besoin, EtapeCircuit etape)
    {
        var roleExtrait = besoin.Statut.Replace("EN_ATTENTE_", "");
        var nouveauStatut = $"REJETE_PAR_{roleExtrait}";
        var motif = $"Rejet automatique : délai de validation dépassé le {DateTime.UtcNow:dd/MM/yyyy HH:mm}";

        besoin.Statut = nouveauStatut;
        besoin.RejeteAutomatiquement = true;
        besoin.DateModification = DateTime.UtcNow;
        await besoinsRepo.UpdateAsync(besoin);

        // Historique
        await besoinsRepo.AddHistoriqueAsync(new Historique
        {
            BesoinId = besoin.Id,
            Action = "REJET_AUTOMATIQUE",
            Description = motif,
            DateAction = DateTime.UtcNow
        });

        // Validation système
        await besoinsRepo.AddValidationAsync(new Validation
        {
            BesoinId = besoin.Id,
            ValidateurId = null,
            Niveau = etape.Ordre,
            EtapeOrdre = etape.Ordre,
            Decision = DecisionValidation.REJETE,
            Motif = motif,
            StatutApres = nouveauStatut,
            DateDecision = DateTime.UtcNow
        });

        // Notification au créateur
        var msgCreateur = $"Votre besoin « {besoin.Titre} » a été automatiquement rejeté faute de validation dans les délais.";
        await notifService.NotifierRejetAsync(besoin.Id, besoin.Titre, besoin.UtilisateurId, etape.RoleRequis);

        // Log
        await logService.LoggerAsync(
            action: "REJET_AUTOMATIQUE_SYSTEME",
            entiteType: "Besoin",
            entiteId: besoin.Id,
            nouvelleValeur: nouveauStatut);

        logger.LogInformation("[Deadline] Rejet automatique besoin {Id} → {Statut}", besoin.Id, nouveauStatut);
    }

    private static string FormatDuree(double minutes)
    {
        if (minutes <= 0) return "0 min";
        if (minutes < 60) return $"{(int)minutes} min";
        var h = (int)(minutes / 60);
        var m = (int)(minutes % 60);
        return m > 0 ? $"{h}h{m:D2}" : $"{h}h";
    }
}
