using FinAssist.Core.Entities;

namespace FinAssist.Core.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(Utilisateur user, IEnumerable<string> permissions);
}
