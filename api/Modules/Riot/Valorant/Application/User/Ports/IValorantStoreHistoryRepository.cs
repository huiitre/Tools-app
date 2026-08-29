namespace Tools.Api.Modules.Riot.Valorant.Application.User.Ports;

public interface IValorantStoreHistoryRepository
{
    // Identifiants de skins par date d'apparition : la résolution en vues est faite au-dessus,
    // en une seule requête pour toutes les dates.
    Task<Dictionary<DateOnly, List<long>>> FindAllRawByAccountId(long accountId);

    Task<long> Add(long accountId, long skinId, DateOnly seenAt);
    Task<bool> ExistsByAccountIdAndSkinIdAndDate(long accountId, long skinId, DateOnly seenAt);
}
