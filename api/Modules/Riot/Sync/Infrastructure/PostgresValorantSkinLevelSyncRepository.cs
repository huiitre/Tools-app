using Tools.Api.Modules.Riot.Sync.Application;
using Tools.Api.Modules.Riot.Common.Infrastructure;
using Tools.Api.Modules.Riot.Sync.Application.Ports;

namespace Tools.Api.Modules.Riot.Sync.Infrastructure;

public sealed class PostgresValorantSkinLevelSyncRepository(RiotDatabase database) : IValorantSkinLevelSyncRepository
{
    public Task DeleteAll() =>
        database.Execute("DELETE FROM tools_riot.valorant_skin_levels");

    public Task Save(long skinId, ValorantSkinLevelSyncData data) =>
        database.Execute(
            """
            INSERT INTO tools_riot.valorant_skin_levels
                (skin_id, asset_id, level_index, name, level_item, display_icon_url, streamed_video_url)
            VALUES (@SkinId, @AssetId, @LevelIndex, @Name, @LevelItem, @DisplayIconUrl, @StreamedVideoUrl)
            """,
            new { SkinId = skinId, data.AssetId, data.LevelIndex, data.Name, data.LevelItem, data.DisplayIconUrl, data.StreamedVideoUrl });
}
