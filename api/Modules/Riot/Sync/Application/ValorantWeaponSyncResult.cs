namespace Tools.Api.Modules.Riot.Sync.Application;

// Les armes sont synchronisées avant les skins, qui ont besoin de l'identifiant en base de leur
// arme parente : la correspondance est donc rendue avec le rapport, pas relue ensuite.
public sealed record ValorantWeaponSyncResult(
    ValorantSyncReport Report,
    Dictionary<Guid, long> WeaponAssetIdToDbId
);
