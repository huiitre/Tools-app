namespace Tools.Api.Modules.Auth.Application.Ports.Registration;

// Jetons de confirmation d'adresse (tools_core.user_email_verification).
public interface IEmailVerificationRepository
{
    Task SaveAsync(long userId, string token, DateTime expiresAt);

    // Identifiant associé à un jeton encore valide, ou null s'il est inconnu ou expiré.
    Task<long?> FindUserIdByValidTokenAsync(string token, DateTime now);

    Task DeleteByUserIdAsync(long userId);

    // Supprime les jetons expirés et retourne le nombre de lignes effacées.
    Task<int> DeleteExpiredAsync(DateTime now);

    // Supprime les inscriptions jamais confirmées et sans jeton valide.
    //
    // Le critère est email_verified_at IS NULL, jamais is_active : un compte suspendu par un
    // administrateur ne doit pas disparaître parce qu'un vieux jeton a expiré.
    Task<int> DeleteAbandonedRegistrationsAsync(DateTime now);
}
