using Tools.Api.Modules.Riot.Sync.Application;
using Tools.Api.Modules.Riot.Common.Infrastructure;
using Tools.Api.Modules.Riot.Sync.Application.Ports;

namespace Tools.Api.Modules.Riot.Sync.Infrastructure;

public sealed class PostgresValorantSkinChromaSyncRepository(RiotDatabase database) : IValorantSkinChromaSyncRepository
{
    public Task DeleteAll() =>
        database.Execute("DELETE FROM tools_riot.valorant_skin_chromas");

    public Task Save(long skinId, ValorantSkinChromaSyncData data) =>
        database.Execute(
            """
            INSERT INTO tools_riot.valorant_skin_chromas
                (skin_id, asset_id, chroma_index, name, display_icon_url, full_render_url, swatch_url, streamed_video_url)
            VALUES (@SkinId, @AssetId, @ChromaIndex, @Name, @DisplayIconUrl, @FullRenderUrl, @SwatchUrl, @StreamedVideoUrl)
            """,
            new { SkinId = skinId, data.AssetId, data.ChromaIndex, data.Name, data.DisplayIconUrl, data.FullRenderUrl, data.SwatchUrl, data.StreamedVideoUrl });
}
