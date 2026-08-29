namespace Tools.Api.Modules.Riot.Sync.Application;

public sealed record ValorantGlobalSyncReport(
    ValorantSyncReport ContentTiers,
    ValorantSyncReport Weapons,
    ValorantSyncReport Skins,
    ValorantSyncReport Bundles
);
