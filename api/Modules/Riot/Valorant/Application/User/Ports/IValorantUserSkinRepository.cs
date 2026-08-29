namespace Tools.Api.Modules.Riot.Valorant.Application.User.Ports;

public interface IValorantUserSkinRepository
{
    Task<long> Add(long accountId, long skinId);
    Task Remove(long accountId, long skinId);
    Task<bool> ExistsByAccountIdAndSkinId(long accountId, long skinId);
}
