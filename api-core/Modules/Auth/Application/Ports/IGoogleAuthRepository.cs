// Port BDD spécifique au rattachement d'une identité Google à un utilisateur Tools.
public interface IGoogleAuthRepository
{
    Task<AuthUser?> FindByGoogleProviderIdAsync(string providerUserId, CancellationToken cancellationToken);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);
    Task<AuthUser> CreateGoogleUserAsync(GoogleIdentity identity, CancellationToken cancellationToken);
    Task UpdateGoogleAvatarAsync(long userId, string pictureUrl, CancellationToken cancellationToken);
}
