using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Services;
using Tools.Api.Modules.Riot.Valorant.Application.Skin.Views;

namespace Tools.Api.Modules.Riot.Valorant.Application.Catalog.Usecases;

// Boutique du jour, lue chez Riot puis résolue contre le catalogue local.
//
// Deux entrées possibles : un compte lié, dont l'API détient le refresh token, ou un access token
// que l'appelant fournit lui-même — c'est le mode dépannage du front, sans rien persister.
public sealed class GetValorantStoreUseCase(
    UseCaseAuthorizer authorizer,
    ValorantAuthService valorantAuthService,
    IValorantAuthRepository valorantAuthRepository,
    IValorantStorePort valorantStorePort,
    IValorantVersionProvider versionProvider,
    IValorantSkinRepository skinRepository,
    IValorantBundleRepository bundleRepository,
    IValorantTokenParser tokenParser
) : SecuredUseCase(authorizer)
{
    // Le Java exigeait USER sur la route et READ_ONLY dans le use case : le plus strict des deux
    // s'appliquait, il est repris ici.
    protected override RoleCode RequiredRole => RoleCode.User;
    protected override ModuleCode? RequiredModule => ModuleCode.Riot;

    private const string DefaultRegion = "eu";

    // Codes que l'adaptateur Riot doit poser sur son AppException pour qu'un access token périmé
    // déclenche une seule reprise au lieu de remonter en erreur.
    private static readonly string[] RetryableCodes =
        ["RIOT_ACCESS_TOKEN_INVALID", "RIOT_STOREFRONT_FETCH_FAILED"];

    public async Task<ValorantStoreView> Execute(long? accountId, string? providedAccessToken, string? providedRegion)
    {
        if (!string.IsNullOrWhiteSpace(providedAccessToken))
        {
            // Jeton fourni : aucun compte n'est consulté, donc rien n'est marqué possédé ni suivi.
            var puuid = tokenParser.ExtractPuuid(providedAccessToken);
            var region = string.IsNullOrWhiteSpace(providedRegion) ? DefaultRegion : providedRegion;

            return await FetchAndMapStore(null, puuid, region, providedAccessToken);
        }

        if (accountId is not { } linkedAccountId)
        {
            throw AppException.Validation(
                "VALORANT_ACCOUNT_REQUIRED",
                "Un compte Valorant lié ou un access token est nécessaire.");
        }

        if (!await valorantAuthRepository.ExistsByIdAndUserId(linkedAccountId, CurrentUser.UserId))
        {
            throw AppException.NotFound(
                "VALORANT_ACCOUNT_NOT_FOUND",
                "Ce compte Valorant est introuvable.");
        }

        var authData = await valorantAuthRepository.FindById(linkedAccountId)
            ?? throw AppException.NotFound(
                "RIOT_AUTH_NOT_FOUND",
                "Aucun jeton n'est enregistré pour ce compte Valorant.");

        var accessToken = await valorantAuthService.GetOrRefreshAccessToken(linkedAccountId);

        try
        {
            return await FetchAndMapStore(linkedAccountId, authData.Puuid, authData.Region, accessToken);
        }
        catch (AppException exception) when (RetryableCodes.Contains(exception.Code))
        {
            // Une seule reprise, avec un jeton neuf : si elle échoue à son tour, l'erreur remonte.
            var renewedToken = await valorantAuthService.GetOrRefreshAccessToken(linkedAccountId);

            return await FetchAndMapStore(linkedAccountId, authData.Puuid, authData.Region, renewedToken);
        }
    }

    private async Task<ValorantStoreView> FetchAndMapStore(
        long? accountId,
        string puuid,
        string region,
        string accessToken)
    {
        var entitlementsToken = await valorantStorePort.FetchEntitlementsToken(accessToken);
        var clientVersion = await versionProvider.GetRiotClientVersion();

        var raw = await valorantStorePort.FetchStorefront(
            puuid, region, accessToken, entitlementsToken, clientVersion);

        var offers = await ResolveOffers(raw.SingleItemOffers, accountId);
        var bundles = new List<ValorantStoreBundle>();

        foreach (var rawBundle in raw.FeaturedBundles)
        {
            // Riot ne donne que l'identifiant du pack : nom et bannière viennent du catalogue local.
            var meta = Guid.TryParse(rawBundle.AssetId, out var bundleAssetId)
                ? await bundleRepository.FindByAssetId(bundleAssetId)
                : null;

            bundles.Add(new ValorantStoreBundle(
                rawBundle.AssetId,
                meta?.Name ?? "Pack inconnu",
                meta?.BannerUrl ?? string.Empty,
                await ResolveOffers(rawBundle.Items, accountId),
                rawBundle.TotalBaseCost,
                rawBundle.TotalDiscountedCost,
                rawBundle.DiscountPercent,
                rawBundle.RemainingSeconds));
        }

        ValorantNightMarket? nightMarket = null;
        if (raw.NightMarket is { } rawNightMarket)
        {
            var nightMarketOffers = new List<ValorantNightMarketOffer>();

            foreach (var rawOffer in rawNightMarket.Offers)
            {
                if (await ResolveSkin(rawOffer.ItemId, accountId) is not { } skin)
                {
                    continue;
                }

                nightMarketOffers.Add(new ValorantNightMarketOffer(
                    rawOffer.OfferId,
                    skin,
                    rawOffer.OriginalCost,
                    rawOffer.DiscountedCost,
                    rawOffer.DiscountPercent,
                    rawOffer.IsSeen));
            }

            nightMarket = new ValorantNightMarket(nightMarketOffers, rawNightMarket.RemainingSeconds);
        }

        return new ValorantStoreView(
            offers,
            raw.SingleItemOffersRemainingDurationInSeconds,
            bundles,
            nightMarket);
    }

    private async Task<List<ValorantStoreOffer>> ResolveOffers(
        List<IValorantStorePort.RawOffer> rawOffers,
        long? accountId)
    {
        var offers = new List<ValorantStoreOffer>();

        foreach (var rawOffer in rawOffers)
        {
            if (await ResolveSkin(rawOffer.ItemId, accountId) is { } skin)
            {
                offers.Add(new ValorantStoreOffer(skin, rawOffer.Cost));
            }
        }

        return offers;
    }

    // Riot désigne les skins par l'UUID de leur *level*, jamais par celui du skin racine. Un
    // identifiant absent du catalogue local est ignoré : la boutique reste affichable.
    private async Task<ValorantSkinView?> ResolveSkin(string itemId, long? accountId)
    {
        return Guid.TryParse(itemId, out var levelAssetId)
            ? await skinRepository.FindByLevelAssetId(levelAssetId, accountId)
            : null;
    }
}
