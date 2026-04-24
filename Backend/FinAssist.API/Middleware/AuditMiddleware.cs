using FinAssist.Core.Interfaces;
using System.Security.Claims;

namespace FinAssist.API.Middleware;

public class AuditMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> MethodesMutantes =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    public async Task InvokeAsync(
        HttpContext context,
        ILogService logService,
        IUserAgentParser uaParser,
        IGeoIpService geoIpService)
    {
        if (!MethodesMutantes.Contains(context.Request.Method))
        {
            await next(context);
            return;
        }

        await next(context);

        if (context.Response.StatusCode is >= 200 and < 300)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // Le login est loggué directement dans AuthController avec l'ID utilisateur
            if (path.Contains("/auth/login", StringComparison.OrdinalIgnoreCase))
                return;

            var utilisateurId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) is { } uid
                && int.TryParse(uid, out var id) ? (int?)id : null;

            var methode = context.Request.Method.ToUpperInvariant();

            // ── IP ────────────────────────────────────────────────────────
            var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "Inconnu";
            if (ip.Contains(',')) ip = ip.Split(',')[0].Trim();

            // ── User-Agent ────────────────────────────────────────────────
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            var (os, navigateur) = uaParser.Parse(userAgent);

            // Priorité au header X-Client-OS envoyé par le frontend (plus précis pour Windows 11)
            var clientOs = context.Request.Headers["X-Client-OS"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(clientOs))
                os = clientOs;

            // ── Géolocalisation (timeout 2s) ──────────────────────────────
            var (pays, ville) = ("Inconnu", "Inconnu");
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                (pays, ville) = await geoIpService.GetLocalisationAsync(ip);
            }
            catch { /* silencieux */ }

            await logService.LoggerAsync(
                action:              $"{methode} {path}",
                entiteType:          DeduireEntite(path),
                entiteId:            DeduireId(path),
                utilisateurId:       utilisateurId,
                adresseIp:           ip,
                systemeExploitation: os,
                navigateur:          navigateur,
                pays:                pays,
                ville:               ville);
        }
    }

    private static string DeduireEntite(string path)
    {
        var segments = path.Trim('/').Split('/');
        if (segments.Length >= 2)
        {
            return segments[1] switch
            {
                "besoins"       => "Besoin",
                "users"         => "Utilisateur",
                "roles"         => "Role",
                "permissions"   => "Permission",
                "workflow"      => "Workflow",
                "signatures"    => "Signature",
                "notifications" => "Notification",
                "categories"    => "Categorie",
                "reporting"     => "Reporting",
                "logs"          => "Log",
                _               => segments[1]
            };
        }
        return "Inconnu";
    }

    private static int? DeduireId(string path)
    {
        var segments = path.Trim('/').Split('/');
        if (segments.Length >= 3 && int.TryParse(segments[2], out var id))
            return id;
        return null;
    }
}
