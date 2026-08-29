namespace Tools.Api.Modules.Riot.Valorant.Application.User.Ports;

public interface IValorantWatchlistRepository
{
    Task<long> Add(long accountId, long skinId);
    Task Remove(long accountId, long skinId);
    Task<bool> ExistsByAccountIdAndSkinId(long accountId, long skinId);
}
