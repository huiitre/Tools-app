namespace Tools.Api.Modules.Riot.Sync.Application.Ports;

public interface IValorantContentTierDataProvider
{
    Task<List<ValorantContentTierSyncData>> FetchAll();
}
