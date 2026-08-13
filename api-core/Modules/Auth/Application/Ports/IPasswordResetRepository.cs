// Port des jetons de réinitialisation de mot de passe (tools_core.user_password_reset).
public interface IPasswordResetRepository
{
    Task SaveAsync(long userId, string token, DateTime expiresAt, CancellationToken cancellationToken);

    // Retourne l'utilisateur d'un jeton encore valide, ou null s'il est inconnu ou expiré.
    Task<long?> FindUserIdByValidTokenAsync(string token, DateTime now, CancellationToken cancellationToken);

    // Une seule demande active par utilisateur : l'ancienne est supprimée avant d'en créer une.
    Task DeleteByUserIdAsync(long userId, CancellationToken cancellationToken);

    // Utilisé par le nettoyage planifié.
    Task<int> DeleteExpiredAsync(DateTime now, CancellationToken cancellationToken);
}
