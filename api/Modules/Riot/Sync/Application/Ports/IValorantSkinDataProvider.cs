namespace Tools.Api.Modules.Riot.Sync.Application.Ports;

public interface IValorantSkinDataProvider
{
    Task<List<ValorantSkinSyncData>> FetchAll();
}
