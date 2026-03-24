using Microsoft.AspNetCore.Authorization;

namespace FinAssist.API.Attributes;

/// <summary>
/// Raccourci pour [Authorize(Roles = "...")] avec les rôles FINASSIST.
/// Usage : [RoleAuthorization("Administrateur", "Direction")]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RoleAuthorizationAttribute : AuthorizeAttribute
{
    public RoleAuthorizationAttribute(params string[] roles)
    {
        Roles = string.Join(",", roles);
    }
}

/// <summary>Constantes des rôles du système.</summary>
public static class Roles
{
    public const string Administrateur = "Administrateur";
    public const string Direction = "Direction";
    public const string Responsable = "Responsable";
    public const string Agent = "Agent";
}
