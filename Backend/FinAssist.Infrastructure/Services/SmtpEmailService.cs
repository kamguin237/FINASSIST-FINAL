using System.Net;
using System.Net.Mail;
using FinAssist.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinAssist.Infrastructure.Services;

public class SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task<bool> SendAsync(string to, string subject, string htmlBody)
    {
        try
        {
            var host     = config["Email:Host"]!;
            var port     = int.Parse(config["Email:Port"] ?? "587");
            var user     = config["Email:Username"]!;
            var password = config["Email:Password"]!;
            var from     = config["Email:From"] ?? user;

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(user, password),
                EnableSsl   = true
            };

            using var message = new MailMessage(from, to, subject, htmlBody)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(message);
            return true;
        }
        catch (Exception ex)
        {
            // On log l'erreur sans exposer le mot de passe
            logger.LogError("Échec envoi email à {To} — sujet: {Subject} — erreur: {Error}",
                to, subject, ex.Message);
            return false;
        }
    }

    public async Task EnvoyerRappelDelaiAsync(FinAssist.Core.Entities.Utilisateur validateur, FinAssist.Core.Entities.Besoin besoin)
    {
        var baseUrl = config["App:BaseUrl"] ?? "http://localhost:4200";
        var html = $"""
            <div style="font-family:Segoe UI,sans-serif;max-width:560px;margin:auto;padding:32px;background:#f9f9f9;border-radius:12px;">
              <h2 style="color:#7c3aed;">⏰ Délai de validation atteint</h2>
              <p>Bonjour <strong>{validateur.Prenom} {validateur.Nom}</strong>,</p>
              <p>Le délai de validation pour le besoin suivant a été atteint :</p>
              <div style="background:#fff;border:1px solid #e0e0e0;border-radius:8px;padding:16px 24px;margin:20px 0;">
                <p style="margin:4px 0;"><strong>Besoin :</strong> {besoin.Titre}</p>
                <p style="margin:4px 0;"><strong>Soumis le :</strong> {besoin.DateEntreeEnAttente:dd/MM/yyyy HH:mm}</p>
                <p style="margin:4px 0;color:#dc2626;"><strong>Délai dépassé</strong></p>
              </div>
              <a href="{baseUrl}/besoins/{besoin.Id}" style="display:inline-block;background:#7c3aed;color:#fff;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:600;">✅ Valider maintenant</a>
              <hr style="border:none;border-top:1px solid #eee;margin:24px 0;" />
              <p style="font-size:12px;color:#999;">Cet email a été généré automatiquement par FinAssist.</p>
            </div>
            """;

        await SendAsync(validateur.Email, $"[FinAssist] Délai de validation atteint — {besoin.Titre}", html);
    }
}
