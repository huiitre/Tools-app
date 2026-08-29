using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

namespace Tools.Api.Modules.Riot.Valorant.Application.Skin.Views;

// Owned/Watched et leurs dates viennent de jointures sur le compte Valorant courant : les dates
// sont nulles quand le drapeau est faux.
public sealed record ValorantSkinView(
    long Id,
    Guid AssetId,
    string Name,
    string? IconUrl,
    Guid? TierUuid,
    ValorantContentTierView? ContentTier,
    long? WeaponId,
    List<ValorantSkinLevelView> Levels,
    List<ValorantSkinChromaView> Chromas,
    bool Owned,
    bool Watched,
    DateTime? OwnedAt,
    DateTime? WatchedAt
);
