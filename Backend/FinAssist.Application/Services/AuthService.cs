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
            DoitChangerMotDePasse = user.DoitChangerMotDePasse,
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

    public async Task ChangerMotDePasseAsync(int userId, ChangePasswordDTO dto)
    {
        var user = await authRepo.GetByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("Utilisateur introuvable.");

        if (!passwordService.Verify(dto.AncienMotDePasse, user.MotDePasse))
            throw new UnauthorizedAccessException("Mot de passe actuel incorrect.");

        if (dto.NouveauMotDePasse.Length < 8)
            throw new ArgumentException("Le nouveau mot de passe doit contenir au moins 8 caractères.");

        user.MotDePasse = passwordService.Hash(dto.NouveauMotDePasse);
        user.DoitChangerMotDePasse = false;
        user.DateModification = DateTime.UtcNow;
        await authRepo.UpdateAsync(user);
    }
}
