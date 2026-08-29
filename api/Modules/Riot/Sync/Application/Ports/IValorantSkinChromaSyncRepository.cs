namespace Tools.Api.Modules.Riot.Sync.Application.Ports;

// Même politique que les niveaux : purge complète puis réinsertion.
public interface IValorantSkinChromaSyncRepository
{
    Task DeleteAll();
    Task Save(long skinId, ValorantSkinChromaSyncData data);
}
