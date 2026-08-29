using Tools.Api.Modules.Core.Notifications.Application;
using Tools.Api.Modules.Core.Notifications.Application.Services;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Services;
using Tools.Api.Modules.Riot.Valorant.Application.Skin.Views;
using Tools.Api.Modules.Riot.Valorant.Application.User.Ports;

namespace Tools.Api.Modules.Riot.Valorant.Application.User.Services;

// Passe sur tous les comptes liés : archive la boutique du jour et prévient l'utilisateur quand
// un skin de sa liste de suivi y est en vente.
//
// **Ce n'est pas un use case sécurisé, et ça ne doit jamais le devenir** : le planificateur
// l'appelle sur un thread de fond, où aucun utilisateur n'est identifié — hériter de
// SecuredUseCase ferait échouer l'autorisation dès la construction. Le déclenchement manuel passe
// par TriggerValorantWatchlistSyncUseCase, qui porte le contrôle d'accès.
public sealed class ValorantWatchlistNotifier(
    ValorantAuthService valorantAuthService,
    IValorantAuthRepository valorantAuthRepository,
    IValorantStorePort valorantStorePort,
    IValorantVersionProvider versionProvider,
    IValorantSkinRepository skinRepository,
    IValorantStoreHistoryRepository storeHistoryRepository,
    NotificationService notificationService,
    ILogger<ValorantWatchlistNotifier> logger)
{
    private const string NotificationMetadata = """{"route": "valorant-shop"}""";

    // La rotation dure 24 h : on date l'archive de son milieu, pour que la même journée de
    // boutique porte la même date d'un bout à l'autre. Le front applique le même calcul.
    private static readonly TimeSpan HalfRotation = TimeSpan.FromHours(12);

    public async Task ProcessAllAccounts()
    {
        var accountIds = await valorantAuthRepository.FindAllAccountIds();

        logger.LogInformation(
            "Synchronisation Valorant (historique + liste de suivi) pour {AccountCount} compte(s)",
            accountIds.Count);

        foreach (var accountId in accountIds)
        {
            try
            {
                await ProcessAccount(accountId);
            }
            catch (Exception exception)
            {
                // Un compte en échec — jeton périmé, Riot injoignable — ne doit pas priver les
                // autres de leur passe.
                logger.LogError(
                    exception,
                    "Échec de la synchronisation Valorant du compte {AccountId}",
                    accountId);
            }
        }
    }

    private async Task ProcessAccount(long accountId)
    {
        if (await valorantAuthRepository.FindById(accountId) is not { } authData)
        {
            return;
        }

        var accessToken = await valorantAuthService.GetOrRefreshAccessToken(accountId);
        var entitlementsToken = await valorantStorePort.FetchEntitlementsToken(accessToken);
        var clientVersion = await versionProvider.GetRiotClientVersion();

        var raw = await valorantStorePort.FetchStorefront(
            authData.Puuid, authData.Region, accessToken, entitlementsToken, clientVersion);

        var shopDate = ShopDateOf(raw.SingleItemOffersRemainingDurationInSeconds);
        var skinIdsInShop = new List<long>();

        foreach (var offer in raw.SingleItemOffers)
        {
            if (!Guid.TryParse(offer.ItemId, out var levelAssetId))
            {
                continue;
            }

            if (await skinRepository.FindByLevelAssetId(levelAssetId, accountId) is not { } skin)
            {
                continue;
            }

            skinIdsInShop.Add(skin.Id);

            if (!await storeHistoryRepository.ExistsByAccountIdAndSkinIdAndDate(accountId, skin.Id, shopDate))
            {
                await storeHistoryRepository.Add(accountId, skin.Id, shopDate);
            }
        }

        var watched = await skinRepository.FindAllWatchedByAccountId(accountId);
        var matches = watched.Where(skin => skinIdsInShop.Contains(skin.Id)).ToList();

        if (matches.Count > 0)
        {
            await Notify(authData.UserId, accountId, matches);
        }
    }

    private static DateOnly ShopDateOf(long remainingSeconds)
    {
        return DateOnly.FromDateTime(
            DateTime.UtcNow.AddSeconds(remainingSeconds).Subtract(HalfRotation));
    }

    private async Task Notify(long userId, long accountId, List<ValorantSkinView> matches)
    {
        var accountLabel = await AccountLabelOf(userId, accountId);
        var suffix = accountLabel is null ? string.Empty : $" ({accountLabel})";

        var body = matches.Count == 1
            ? $"Le skin \"{matches[0].Name}\" est disponible dans ta boutique !"
            : "Plusieurs skins de ta liste de suivi sont disponibles :\n"
              + string.Join("\n", matches.Select(skin => $"- {skin.Name}"));

        await notificationService.Send(new SendNotificationCommand(
            $"Valorant Shop - Skins disponibles !{suffix}",
            body,
            NotificationType.Success,
            TargetUserId: userId,
            Metadata: NotificationMetadata));
    }

    // Le libellé donné par l'utilisateur, sinon son Riot ID : avec plusieurs comptes liés, une
    // notification sans nom ne dit pas lequel est concerné.
    private async Task<string?> AccountLabelOf(long userId, long accountId)
    {
        var accounts = await valorantAuthRepository.FindAllByUserId(userId);

        if (accounts.FirstOrDefault(account => account.Id == accountId) is not { } account)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(account.Label))
        {
            return account.Label;
        }

        return account.GameName is null ? null : $"{account.GameName}#{account.TagLine}";
    }
}
