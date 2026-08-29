using Tools.Api.Modules.Riot.Common.Infrastructure;
using Tools.Api.Modules.Riot.Valorant.Application.User.Ports;

namespace Tools.Api.Modules.Riot.Valorant.Infrastructure;

public sealed class PostgresValorantWatchlistRepository(RiotDatabase database) : IValorantWatchlistRepository
{
    public Task<long> Add(long accountId, long skinId) =>
        database.ExecuteScalar<long>(
            """
            INSERT INTO tools_riot.valorant_skin_watchlist (valorant_account_id, skin_id)
            VALUES (@AccountId, @SkinId)
            RETURNING id
            """,
            new { AccountId = accountId, SkinId = skinId });

    // Une suppression sans effet n'est pas signalée : le use case veut le skin non suivi, il ne l'est pas.
    public Task Remove(long accountId, long skinId) =>
        database.Execute(
            """
            DELETE FROM tools_riot.valorant_skin_watchlist
            WHERE valorant_account_id = @AccountId AND skin_id = @SkinId
            """,
            new { AccountId = accountId, SkinId = skinId });

    public Task<bool> ExistsByAccountIdAndSkinId(long accountId, long skinId) =>
        database.ExecuteScalar<bool>(
            """
            SELECT EXISTS (
                SELECT 1 FROM tools_riot.valorant_skin_watchlist
                WHERE valorant_account_id = @AccountId AND skin_id = @SkinId
            )
            """,
            new { AccountId = accountId, SkinId = skinId });
}
