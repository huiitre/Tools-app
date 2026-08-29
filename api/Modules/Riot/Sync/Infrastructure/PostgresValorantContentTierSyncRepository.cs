using Tools.Api.Modules.Riot.Sync.Application;
using Tools.Api.Modules.Riot.Common.Infrastructure;
using Tools.Api.Modules.Riot.Sync.Application.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

namespace Tools.Api.Modules.Riot.Sync.Infrastructure;

public sealed class PostgresValorantContentTierSyncRepository(RiotDatabase database) : IValorantContentTierSyncRepository
{
    public Task<List<ValorantContentTierView>> FindAll() =>
        database.Query<ValorantContentTierView>(
            """
            SELECT id AS Id, asset_id AS AssetId, name AS Name, dev_name AS DevName, rank AS Rank,
                   juice_value AS JuiceValue, juice_cost AS JuiceCost,
                   highlight_color AS HighlightColor, display_icon_url AS DisplayIconUrl
            FROM tools_riot.valorant_content_tiers
            """);

    public Task<long> Save(ValorantContentTierSyncData data) =>
        database.ExecuteScalar<long>(
            """
            INSERT INTO tools_riot.valorant_content_tiers
                (asset_id, name, dev_name, rank, juice_value, juice_cost, highlight_color, display_icon_url)
            VALUES (@AssetId, @Name, @DevName, @Rank, @JuiceValue, @JuiceCost, @HighlightColor, @DisplayIconUrl)
            RETURNING id
            """,
            data);

    public Task Update(long id, ValorantContentTierSyncData data) =>
        database.Execute(
            """
            UPDATE tools_riot.valorant_content_tiers
            SET name = @Name, dev_name = @DevName, rank = @Rank, juice_value = @JuiceValue,
                juice_cost = @JuiceCost, highlight_color = @HighlightColor,
                display_icon_url = @DisplayIconUrl, updated_at = now()
            WHERE id = @Id
            """,
            new { Id = id, data.Name, data.DevName, data.Rank, data.JuiceValue, data.JuiceCost, data.HighlightColor, data.DisplayIconUrl });

    public Task Delete(long id) =>
        database.Execute("DELETE FROM tools_riot.valorant_content_tiers WHERE id = @Id", new { Id = id });
}
