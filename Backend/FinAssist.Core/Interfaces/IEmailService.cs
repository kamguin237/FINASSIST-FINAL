using FinAssist.Core.Entities;

namespace FinAssist.Core.Interfaces;

public interface IEmailService
{
    Task<bool> SendAsync(string to, string subject, string htmlBody);
    Task EnvoyerRappelDelaiAsync(Utilisateur validateur, Besoin besoin);
}
