namespace Tools.Api.Modules.Riot.Sync.Application.Ports;

// Les niveaux sont entièrement réécrits à chaque synchronisation, jamais comparés un à un.
public interface IValorantSkinLevelSyncRepository
{
    Task DeleteAll();
    Task Save(long skinId, ValorantSkinLevelSyncData data);
}
