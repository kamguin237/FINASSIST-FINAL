using FinAssist.Core.Interfaces;
using System.Security.Claims;

namespace FinAssist.API.Middleware;

/// <summary>
/// Intercepte toutes les requêtes mutantes (POST, PUT, PATCH, DELETE)
/// et enregistre un log d'audit après exécution.
/// </summary>
public class AuditMiddleware(RequestDelegate next)
{
    // Méthodes HTTP qui modifient des données
    private static readonly HashSet<string> MethodesMutantes =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    public async Task InvokeAsync(HttpContext context, ILogService logService)
    {
        if (!MethodesMutantes.Contains(context.Request.Method))
        {
            await next(context);
            return;
        }

        await next(context);

        // On ne logue que les réponses réussies (2xx)
        if (context.Response.StatusCode is >= 200 and < 300)
        {
            var utilisateurId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) is { } uid
                && int.TryParse(uid, out var id) ? (int?)id : null;

            var path = context.Request.Path.Value ?? string.Empty;
            var methode = context.Request.Method.ToUpperInvariant();

            // Déduire l'entité depuis le path  ex: /api/besoins/5 → Besoin
            var entiteType = DeduireEntite(path);
            var entiteId = DeduireId(path);
            var action = $"{methode} {path}";

            await logService.LoggerAsync(
                action: action,
                entiteType: entiteType,
                entiteId: entiteId,
                utilisateurId: utilisateurId);
        }
    }

    private static string DeduireEntite(string path)
    {
        var segments = path.Trim('/').Split('/');
        // /api/{entite}/... → segments[1]
        if (segments.Length >= 2)
        {
            return segments[1] switch
            {
                "besoins" => "Besoin",
                "users" => "Utilisateur",
                "roles" => "Role",
                "permissions" => "Permission",
                "workflow" => "Workflow",
                "signatures" => "Signature",
                "notifications" => "Notification",
                "categories" => "Categorie",
                "reporting" => "Reporting",
                "logs" => "Log",
                _ => segments[1]
            };
        }
        return "Inconnu";
    }

    private static int? DeduireId(string path)
    {
        var segments = path.Trim('/').Split('/');
        // /api/{entite}/{id}/... → segments[2]
        if (segments.Length >= 3 && int.TryParse(segments[2], out var id))
            return id;
        return null;
    }
}
