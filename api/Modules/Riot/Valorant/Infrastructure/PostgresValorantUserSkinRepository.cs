using Tools.Api.Modules.Riot.Common.Infrastructure;
using Tools.Api.Modules.Riot.Valorant.Application.User.Ports;

namespace Tools.Api.Modules.Riot.Valorant.Infrastructure;

public sealed class PostgresValorantUserSkinRepository(RiotDatabase database) : IValorantUserSkinRepository
{
    public Task<long> Add(long accountId, long skinId) =>
        database.ExecuteScalar<long>(
            """
            INSERT INTO tools_riot.valorant_user_skins (valorant_account_id, skin_id)
            VALUES (@AccountId, @SkinId)
            RETURNING id
            """,
            new { AccountId = accountId, SkinId = skinId });

    // Une suppression sans effet n'est pas signalée : le use case veut le skin absent, il l'est.
    public Task Remove(long accountId, long skinId) =>
        database.Execute(
            """
            DELETE FROM tools_riot.valorant_user_skins
            WHERE valorant_account_id = @AccountId AND skin_id = @SkinId
            """,
            new { AccountId = accountId, SkinId = skinId });

    public Task<bool> ExistsByAccountIdAndSkinId(long accountId, long skinId) =>
        database.ExecuteScalar<bool>(
            """
            SELECT EXISTS (
                SELECT 1 FROM tools_riot.valorant_user_skins
                WHERE valorant_account_id = @AccountId AND skin_id = @SkinId
            )
            """,
            new { AccountId = accountId, SkinId = skinId });
}
