using Tools.Api.Modules.Riot.Common.Infrastructure;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;
using Tools.Api.Modules.Riot.Valorant.Application.Skin.Views;

namespace Tools.Api.Modules.Riot.Valorant.Infrastructure;

// Un skin porte ses niveaux, ses chromas et sa rareté. Les niveaux arrivent par jointure — une
// ligne par niveau, regroupée ici — et les chromas par une seconde requête sur les identifiants
// déjà connus : les joindre tous les deux d'un coup multiplierait les lignes entre eux.
public sealed class PostgresValorantSkinRepository(RiotDatabase database) : IValorantSkinRepository
{
    // accountId est comparé dans les jointures : nul, elles ne ramènent rien et les deux
    // drapeaux tombent à faux, sans exclure le skin.
    private const string SelectWithLevels = """
        SELECT s.id AS Id, s.asset_id AS AssetId, s.name AS Name, s.icon_url AS IconUrl,
               s.tier_uuid AS TierUuid, s.weapon_id AS WeaponId,
               (us.id IS NOT NULL) AS Owned, us.created_at AS OwnedAt,
               (w.id IS NOT NULL) AS Watched, w.created_at AS WatchedAt,
               l.asset_id AS LevelAssetId, l.level_index AS LevelIndex, l.name AS LevelName,
               l.level_item AS LevelItem, l.display_icon_url AS LevelDisplayIconUrl,
               l.streamed_video_url AS LevelStreamedVideoUrl,
               ct.id AS CtId, ct.asset_id AS CtAssetId, ct.name AS CtName, ct.dev_name AS CtDevName,
               ct.rank AS CtRank, ct.juice_value AS CtJuiceValue, ct.juice_cost AS CtJuiceCost,
               ct.highlight_color AS CtHighlightColor, ct.display_icon_url AS CtDisplayIconUrl
        FROM tools_riot.valorant_weapon_skins s
        LEFT JOIN tools_riot.valorant_skin_levels l ON l.skin_id = s.id
        LEFT JOIN tools_riot.valorant_user_skins us ON us.skin_id = s.id AND us.valorant_account_id = @AccountId
        LEFT JOIN tools_riot.valorant_skin_watchlist w ON w.skin_id = s.id AND w.valorant_account_id = @AccountId
        LEFT JOIN tools_riot.valorant_content_tiers ct ON ct.asset_id = s.content_tier_uuid
        """;

    private sealed record SkinRow(
        long Id,
        Guid AssetId,
        string Name,
        string? IconUrl,
        Guid? TierUuid,
        long? WeaponId,
        bool Owned,
        DateTime? OwnedAt,
        bool Watched,
        DateTime? WatchedAt,
        Guid? LevelAssetId,
        int? LevelIndex,
        string? LevelName,
        string? LevelItem,
        string? LevelDisplayIconUrl,
        string? LevelStreamedVideoUrl,
        long? CtId,
        Guid? CtAssetId,
        string? CtName,
        string? CtDevName,
        int? CtRank,
        int? CtJuiceValue,
        int? CtJuiceCost,
        string? CtHighlightColor,
        string? CtDisplayIconUrl);

    private sealed record ChromaRow(
        long SkinId,
        Guid AssetId,
        int ChromaIndex,
        string Name,
        string? DisplayIconUrl,
        string? FullRenderUrl,
        string? SwatchUrl,
        string? StreamedVideoUrl);

    public Task<List<ValorantSkinView>> FindAll(long? accountId) =>
        FindMany($"{SelectWithLevels} ORDER BY s.name, l.level_index", new { AccountId = accountId });

    public Task<ValorantSkinView?> FindById(long id, long? accountId) =>
        FindOne($"{SelectWithLevels} WHERE s.id = @Id ORDER BY l.level_index", new { AccountId = accountId, Id = id });

    public Task<ValorantSkinView?> FindByAssetId(Guid assetId, long? accountId) =>
        FindOne($"{SelectWithLevels} WHERE s.asset_id = @AssetId ORDER BY l.level_index", new { AccountId = accountId, AssetId = assetId });

    // Riot ne connaît que les UUID de niveaux : on remonte au skin parent avant de tout charger.
    public Task<ValorantSkinView?> FindByLevelAssetId(Guid levelAssetId, long? accountId) =>
        FindOne(
            $"""
             {SelectWithLevels}
             WHERE s.id = (SELECT skin_id FROM tools_riot.valorant_skin_levels WHERE asset_id = @LevelAssetId)
             ORDER BY l.level_index
             """,
            new { AccountId = accountId, LevelAssetId = levelAssetId });

    public Task<List<ValorantSkinView>> FindAllByWeaponId(long weaponId, long? accountId) =>
        FindMany($"{SelectWithLevels} WHERE s.weapon_id = @WeaponId ORDER BY s.name, l.level_index",
            new { AccountId = accountId, WeaponId = weaponId });

    public Task<List<ValorantSkinView>> FindAllByTierUuid(Guid tierUuid, long? accountId) =>
        FindMany($"{SelectWithLevels} WHERE s.tier_uuid = @TierUuid ORDER BY s.name, l.level_index",
            new { AccountId = accountId, TierUuid = tierUuid });

    public Task<List<ValorantSkinView>> FindAllOwnedByAccountId(long accountId) =>
        FindMany($"{SelectWithLevels} WHERE us.id IS NOT NULL ORDER BY s.name, l.level_index",
            new { AccountId = accountId });

    public Task<List<ValorantSkinView>> FindAllWatchedByAccountId(long accountId) =>
        FindMany($"{SelectWithLevels} WHERE w.id IS NOT NULL ORDER BY s.name, l.level_index",
            new { AccountId = accountId });

    private async Task<ValorantSkinView?> FindOne(string sql, object parameters) =>
        (await FindMany(sql, parameters)).FirstOrDefault();

    private async Task<List<ValorantSkinView>> FindMany(string sql, object parameters)
    {
        var rows = await database.Query<SkinRow>(sql, parameters);

        if (rows.Count == 0)
        {
            return [];
        }

        var chromasBySkinId = await FindChromas(rows.Select(row => row.Id).Distinct().ToArray());

        return rows
            .GroupBy(row => row.Id)
            .Select(group => ToView(group.First(), group, chromasBySkinId))
            .ToList();
    }

    private static ValorantSkinView ToView(
        SkinRow first,
        IEnumerable<SkinRow> rows,
        Dictionary<long, List<ValorantSkinChromaView>> chromasBySkinId)
    {
        // Un skin sans niveau produit tout de même une ligne, avec ses colonnes de niveau nulles.
        var levels = rows
            .Where(row => row.LevelAssetId is not null)
            .Select(row => new ValorantSkinLevelView(
                row.LevelAssetId!.Value,
                row.LevelIndex ?? 0,
                row.LevelName ?? string.Empty,
                row.LevelItem,
                row.LevelDisplayIconUrl,
                row.LevelStreamedVideoUrl))
            .ToList();

        return new ValorantSkinView(
            first.Id,
            first.AssetId,
            first.Name,
            first.IconUrl,
            first.TierUuid,
            ToContentTier(first),
            first.WeaponId,
            levels,
            chromasBySkinId.GetValueOrDefault(first.Id, []),
            first.Owned,
            first.Watched,
            first.OwnedAt,
            first.WatchedAt);
    }

    private static ValorantContentTierView? ToContentTier(SkinRow row) =>
        row.CtId is { } tierId && row.CtAssetId is { } tierAssetId
            ? new ValorantContentTierView(
                tierId,
                tierAssetId,
                row.CtName ?? string.Empty,
                row.CtDevName ?? string.Empty,
                row.CtRank ?? 0,
                row.CtJuiceValue ?? 0,
                row.CtJuiceCost ?? 0,
                row.CtHighlightColor,
                row.CtDisplayIconUrl)
            : null;

    private async Task<Dictionary<long, List<ValorantSkinChromaView>>> FindChromas(long[] skinIds)
    {
        var rows = await database.Query<ChromaRow>(
            """
            SELECT skin_id AS SkinId, asset_id AS AssetId, chroma_index AS ChromaIndex, name AS Name,
                   display_icon_url AS DisplayIconUrl, full_render_url AS FullRenderUrl,
                   swatch_url AS SwatchUrl, streamed_video_url AS StreamedVideoUrl
            FROM tools_riot.valorant_skin_chromas
            WHERE skin_id = ANY(@SkinIds)
            ORDER BY skin_id, chroma_index
            """,
            new { SkinIds = skinIds });

        return rows
            .GroupBy(row => row.SkinId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(row => new ValorantSkinChromaView(
                    row.AssetId,
                    row.ChromaIndex,
                    row.Name,
                    row.DisplayIconUrl,
                    row.FullRenderUrl,
                    row.SwatchUrl,
                    row.StreamedVideoUrl)).ToList());
    }
}
