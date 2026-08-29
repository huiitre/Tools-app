using Tools.Api.Modules.Riot.Common.Infrastructure;
using Tools.Api.Modules.Riot.Valorant.Application.User.Ports;

namespace Tools.Api.Modules.Riot.Valorant.Infrastructure;

public sealed class PostgresValorantStoreHistoryRepository(RiotDatabase database) : IValorantStoreHistoryRepository
{
    private sealed record HistoryRow(DateOnly SeenAt, long SkinId);

    public async Task<Dictionary<DateOnly, List<long>>> FindAllRawByAccountId(long accountId)
    {
        var rows = await database.Query<HistoryRow>(
            """
            SELECT seen_at AS SeenAt, skin_id AS SkinId
            FROM tools_riot.valorant_store_history
            WHERE valorant_account_id = @AccountId
            ORDER BY seen_at DESC
            """,
            new { AccountId = accountId });

        // Le tri est porté par la requête : le regroupement conserve l'ordre des lignes, donc les
        // journées sortent de la plus récente à la plus ancienne.
        return rows
            .GroupBy(row => row.SeenAt)
            .ToDictionary(group => group.Key, group => group.Select(row => row.SkinId).ToList());
    }

    public Task<long> Add(long accountId, long skinId, DateOnly seenAt) =>
        database.ExecuteScalar<long>(
            """
            INSERT INTO tools_riot.valorant_store_history (valorant_account_id, skin_id, seen_at)
            VALUES (@AccountId, @SkinId, @SeenAt)
            RETURNING id
            """,
            new { AccountId = accountId, SkinId = skinId, SeenAt = seenAt });

    public Task<bool> ExistsByAccountIdAndSkinIdAndDate(long accountId, long skinId, DateOnly seenAt) =>
        database.ExecuteScalar<bool>(
            """
            SELECT EXISTS (
                SELECT 1 FROM tools_riot.valorant_store_history
                WHERE valorant_account_id = @AccountId AND skin_id = @SkinId AND seen_at = @SeenAt
            )
            """,
            new { AccountId = accountId, SkinId = skinId, SeenAt = seenAt });
}
