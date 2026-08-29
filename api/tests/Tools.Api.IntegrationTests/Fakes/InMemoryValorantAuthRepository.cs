using Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;

namespace Tools.Api.IntegrationTests.Fakes;

// Comptes Valorant liés en mémoire. Le compte 1 appartient à l'utilisateur 1 : il sert à éprouver
// le contrôle d'appartenance, qu'aucune route ne doit pouvoir contourner.
public sealed class InMemoryValorantAuthRepository : IValorantAuthRepository
{
    public const long OwnedAccountId = 1;
    public const long OwnerUserId = 1;
    public const long ForeignAccountId = 2;

    public Task<long> Save(
        long userId, string puuid, string region, string? gameName, string? tagLine, string? label,
        string encryptedRefreshToken, string iv, DateTime expiresAt) =>
        Task.FromResult(OwnedAccountId);

    public Task<IValorantAuthRepository.ValorantAuthData?> FindById(long accountId) =>
        Task.FromResult<IValorantAuthRepository.ValorantAuthData?>(null);

    public Task<List<IValorantAuthRepository.ValorantAccountData>> FindAllByUserId(long userId) =>
        Task.FromResult<List<IValorantAuthRepository.ValorantAccountData>>(
            userId == OwnerUserId
                ? [new(OwnedAccountId, "puuid-1", "eu", "Huiitre", "EUW", "Principal")]
                : []);

    public Task<bool> ExistsByIdAndUserId(long accountId, long userId) =>
        Task.FromResult(accountId == OwnedAccountId && userId == OwnerUserId);

    public Task<bool> ExistsByUserIdAndPuuid(long userId, string puuid) => Task.FromResult(false);

    public Task UpdateLabel(long accountId, string label) => Task.CompletedTask;

    public Task DeleteById(long accountId) => Task.CompletedTask;

    // Aucun compte à traiter : la passe de fond ne sort donc jamais vers Riot.
    public Task<List<long>> FindAllAccountIds() => Task.FromResult<List<long>>([]);
}
