using FinAssist.Core.DTOs.Auth;
using FinAssist.Core.Interfaces;

namespace FinAssist.Application.Services;

public class AuthService(IAuthRepository authRepo, IPasswordService passwordService, IJwtTokenService jwtService, IPermissionsRepository permissionsRepo) : IAuthService
{
    public async Task<LoginResponseDTO> LoginAsync(LoginRequestDTO request)
    {
        var user = await authRepo.GetByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Email ou mot de passe incorrect.");

        if (!user.Actif)
            throw new UnauthorizedAccessException("Ce compte est désactivé.");

        if (!passwordService.Verify(request.MotDePasse, user.MotDePasse))
            throw new UnauthorizedAccessException("Email ou mot de passe incorrect.");

        var permissions = await permissionsRepo.GetPermissionsEffectivesAsync(user.Id);
        var accessToken = jwtService.GenerateAccessToken(user, permissions);

        return new LoginResponseDTO
        {
            AccessToken = accessToken,
            Expiration = DateTime.UtcNow.AddMinutes(60),
            Utilisateur = new UtilisateurInfoDTO
            {
                Id = user.Id,
                Nom = user.Nom,
                Prenom = user.Prenom,
                Email = user.Email,
                Role = user.Role.Code
            }
        };
    }

    public Task LogoutAsync(int userId) => Task.CompletedTask;
}
