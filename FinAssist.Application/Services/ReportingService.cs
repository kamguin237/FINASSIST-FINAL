using FinAssist.Core.DTOs.Reporting;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;

namespace FinAssist.Application.Services;

public class ReportingService(
    IReportingRepository reportingRepo,
    IExportService exportService) : IReportingService
{
    public async Task<StatistiquesDTO> GetStatistiquesAsync()
    {
        var besoins = await reportingRepo.GetAllBesoinsAsync();
        var utilisateurs = await reportingRepo.GetAllUtilisateursAsync();
        var signatures = await reportingRepo.CountSignaturesAsync();
        var notifications = await reportingRepo.CountNotificationsAsync();

        return new StatistiquesDTO
        {
            TotalBesoins = besoins.Count(),
            TotalUtilisateurs = utilisateurs.Count(),
            TotalActifs = utilisateurs.Count(u => u.Actif),
            BesoinsByStatut = besoins
                .GroupBy(b => b.Statut.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),
            BesoinsByCategorie = besoins
                .GroupBy(b => b.Categorie?.Nom ?? "Sans catégorie")
                .ToDictionary(g => g.Key, g => g.Count()),
            SignaturesApposees = signatures,
            NotificationsEnvoyees = notifications
        };
    }

    public async Task<RapportDTO> GetRapportBesoinsAsync(FiltreRapportDTO? filtres, int generateurId)
    {
        var besoins = await reportingRepo.GetBesoinsFiltrésAsync(filtres);

        var contenu = besoins.Select(b => new
        {
            b.Id,
            b.Titre,
            Statut = b.Statut.ToString(),
            b.NiveauImportance,
            Categorie = b.Categorie?.Nom ?? string.Empty,
            Createur = b.Utilisateur is not null ? $"{b.Utilisateur.Prenom} {b.Utilisateur.Nom}" : string.Empty,
            b.DateCreation,
            b.DateModification
        });

        var parametres = new Dictionary<string, string>();
        if (filtres?.DateDebut is not null) parametres["dateDebut"] = filtres.DateDebut.Value.ToString("yyyy-MM-dd");
        if (filtres?.DateFin is not null) parametres["dateFin"] = filtres.DateFin.Value.ToString("yyyy-MM-dd");
        if (filtres?.Statut is not null) parametres["statut"] = filtres.Statut;

        return new RapportDTO
        {
            Type = "RAPPORT_BESOINS",
            DateGeneration = DateTime.UtcNow,
            Contenu = contenu,
            GenerateurId = generateurId,
            Parametres = parametres
        };
    }

    public async Task<DashboardDTO> GetDashboardAsync(int utilisateurId, string roleCode)
    {
        var besoins = roleCode is "Administrateur" or "Direction" or "Responsable"
            ? await reportingRepo.GetAllBesoinsAsync()
            : await reportingRepo.GetBesoinsParUtilisateurAsync(utilisateurId);

        var nonLues = await reportingRepo.CountNotificationsNonLuesAsync(utilisateurId);

        return new DashboardDTO
        {
            BesoinsEnAttente = besoins.Count(b =>
                b.Statut == WorkflowEngine.EN_ATTENTE || b.Statut == WorkflowEngine.TRANSMIS),
            BesoinsSoumis = besoins.Count(b => b.Statut == WorkflowEngine.EN_ATTENTE),
            BesoinsApprouves = besoins.Count(b => b.Statut.StartsWith("APPROUVE_ROLE")),
            BesoinsRejetes = besoins.Count(b => b.Statut.StartsWith("REJETE_ROLE")),
            BesoinsSignes = besoins.Count(b => b.Statut.StartsWith("SIGNE_ROLE")),
            NotificationsNonLues = nonLues,
            DerniersBesoins = besoins
                .OrderByDescending(b => b.DateModification)
                .Take(5)
                .Select(b => new BesoinRecent
                {
                    Id = b.Id,
                    Titre = b.Titre,
                    Statut = b.Statut,
                    DateModification = b.DateModification
                })
        };
    }

    public async Task<(byte[] contenu, string contentType, string nomFichier)> ExporterAsync(
        ExportRequestDTO request, int generateurId)
    {
        var rapport = await GetRapportBesoinsAsync(request.Filtres, generateurId);
        var besoins = await reportingRepo.GetBesoinsFiltrésAsync(request.Filtres);

        return request.Format.ToUpperInvariant() switch
        {
            "PDF" => (
                exportService.ExporterPdf(besoins),
                "application/pdf",
                $"rapport_besoins_{DateTime.UtcNow:yyyyMMdd}.pdf"),
            _ => (
                exportService.ExporterExcel(besoins),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"rapport_besoins_{DateTime.UtcNow:yyyyMMdd}.xlsx")
        };
    }
}
