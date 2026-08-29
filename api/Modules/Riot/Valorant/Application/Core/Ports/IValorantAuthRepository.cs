namespace Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;

// Comptes Valorant liés à un compte Tools. Le refresh token n'en sort jamais en clair : le
// chiffré et son vecteur d'initialisation sont stockés tels quels, déchiffrés en dehors.
public interface IValorantAuthRepository
{
    // Upsert sur (user_id, puuid) — la contrainte unique de valorant_account. Appelé aussi à
    // chaque rotation de jeton avec gameName/tagLine/label nuls : l'implémentation ne doit alors
    // pas écraser les valeurs déjà en base.
    Task<long> Save(
        long userId,
        string puuid,
        string region,
        string? gameName,
        string? tagLine,
        string? label,
        string encryptedRefreshToken,
        string iv,
        DateTime expiresAt
    );

    Task<ValorantAuthData?> FindById(long accountId);
    Task<List<ValorantAccountData>> FindAllByUserId(long userId);
    Task<bool> ExistsByIdAndUserId(long accountId, long userId);
    Task<bool> ExistsByUserIdAndPuuid(long userId, string puuid);
    Task UpdateLabel(long accountId, string label);
    Task DeleteById(long accountId);

    // Utilisé par le renouvellement périodique des jetons, qui n'a pas d'utilisateur appelant.
    Task<List<long>> FindAllAccountIds();

    record ValorantAuthData(
        long UserId,
        string Puuid,
        string Region,
        string EncryptedRefreshToken,
        string Iv,
        DateTime ExpiresAt
    );

    record ValorantAccountData(
        long Id,
        string Puuid,
        string Region,
        string? GameName,
        string? TagLine,
        string? Label
    );
}
