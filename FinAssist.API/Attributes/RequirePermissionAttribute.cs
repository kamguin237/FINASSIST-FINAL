using FinAssist.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FinAssist.API.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequirePermissionAttribute(string permissionCode) : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Cherche le claim "sub" (userId) avec le une double securité
        var userIdClaim = context.HttpContext.User.FindFirst("sub")
                       ?? context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        
        // Si pas de token ou id invalide --> 401 Unauthorized
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Non authentifié." });
            return;
        }
        
        // Récupère le service qui gère les permissions
        var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        // Est-ce que cet utilisateur a cette permission ?
        var hasPermission = await permissionService.HasPermissionAsync(userId, permissionCode);

        if (!hasPermission)
            context.Result = new ObjectResult(new { message = $"Permission '{permissionCode}' requise." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
    }
}

// NB: En gros ce code veut dire: Avant d’exécuter une action API, je vérifie que l’utilisateur est connecté et possède la permission requise. Sinon, je bloque l’accès.
