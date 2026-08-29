using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Ports;

namespace Tools.Api.Modules.Riot.Valorant.Infrastructure;

// Boutique et pseudo, lus sur les points d'entrée du client de jeu.
//
// Riot n'expose pas ces routes publiquement : elles attendent les en-têtes que le client envoie,
// dont une empreinte de plateforme et la version exacte du client. Une version périmée fait
// répondre 400, d'où la lecture de version.json à chaque appel.
public sealed class ValorantStoreHttpAdapter(HttpClient httpClient) : IValorantStorePort
{
    private const string EntitlementsUrl = "https://entitlements.auth.riotgames.com/api/token/v1";
    private const string StorefrontUrlTemplate = "https://pd.{0}.a.pvp.net/store/v3/storefront/{1}";
    private const string NameServiceUrlTemplate = "https://pd.{0}.a.pvp.net/name-service/v2/players";

    // Valorant Points. Les autres devises du panier (Radianite, Kingdom Credits) ne nous servent pas.
    private const string VpCurrencyId = "85ad13f7-3d1b-5128-9eb2-7cd8ee0b5741";

    // ItemTypeID « EquippableSkinLevel » : le seul type d'objet que le catalogue local sait résoudre.
    private const string SkinTypeId = "e7c63390-eda7-46e0-bb7a-a6abdacd2433";

    // Empreinte de plateforme attendue par Riot, encodée en Base64 (PC / Windows 10).
    private const string ClientPlatform =
        "ew0KCSJwbGF0Zm9ybVR5cGUiOiAiUEMiLA0KCSJwbGF0Zm9ybU9TIjogIldpbmRvd3MiLA0KCSJwbGF0Zm9ybU9TVmVyc2lvbiI6ICIxMC4wLjE5MDQyLjEuMjU2LjY0Yml0IiwNCgkicGxhdGZvcm1DaGlwc2V0IjogIlVua25vd24iDQp9";

    public async Task<string> FetchEntitlementsToken(string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, EntitlementsUrl)
        {
            Content = EmptyJsonBody()
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var payload = await Send(request, "RIOT_ACCESS_TOKEN_INVALID");

        return payload.TryGetProperty("entitlements_token", out var token) && token.GetString() is { Length: > 0 } value
            ? value
            : throw AppException.Unavailable(
                "RIOT_ENTITLEMENTS_EMPTY_RESPONSE",
                "Riot n'a pas renvoyé de jeton d'entitlements.");
    }

    public async Task<IValorantStorePort.RawStorefront> FetchStorefront(
        string puuid,
        string region,
        string accessToken,
        string entitlementsToken,
        string clientVersion)
    {
        var url = string.Format(StorefrontUrlTemplate, region, puuid);

        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = EmptyJsonBody() };
        AddGameClientHeaders(request, accessToken, entitlementsToken, clientVersion);

        return ParseStorefront(await Send(request, "RIOT_STOREFRONT_FETCH_FAILED"));
    }

    // Le pseudo n'est qu'un confort d'affichage : un échec rend un Riot ID vide plutôt qu'une erreur.
    public async Task<IValorantStorePort.RiotId> FetchRiotId(
        string puuid,
        string region,
        string accessToken,
        string entitlementsToken,
        string clientVersion)
    {
        var url = string.Format(NameServiceUrlTemplate, region);

        using var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(new[] { puuid })
        };
        AddGameClientHeaders(request, accessToken, entitlementsToken, clientVersion);

        try
        {
            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return new IValorantStorePort.RiotId(null, null);
            }

            var players = await response.Content.ReadFromJsonAsync<JsonElement>();

            if (players.ValueKind != JsonValueKind.Array || players.GetArrayLength() == 0)
            {
                return new IValorantStorePort.RiotId(null, null);
            }

            var player = players[0];

            return new IValorantStorePort.RiotId(
                ReadOptionalString(player, "GameName"),
                ReadOptionalString(player, "TagLine"));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new IValorantStorePort.RiotId(null, null);
        }
    }

    private static StringContent EmptyJsonBody() => new("{}", Encoding.UTF8, "application/json");

    private static void AddGameClientHeaders(
        HttpRequestMessage request,
        string accessToken,
        string entitlementsToken,
        string clientVersion)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("X-Riot-Entitlements-JWT", entitlementsToken);
        request.Headers.Add("X-Riot-ClientPlatform", ClientPlatform);
        request.Headers.Add("X-Riot-ClientVersion", clientVersion);
    }

    // clientErrorCode est celui que GetValorantStoreUseCase reconnaît pour retenter une fois avec
    // un access token neuf : un 4xx ici veut presque toujours dire « jeton expiré ».
    private async Task<JsonElement> Send(HttpRequestMessage request, string clientErrorCode)
    {
        HttpResponseMessage response;

        try
        {
            response = await httpClient.SendAsync(request);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            throw AppException.Unavailable("RIOT_STORE_UNAVAILABLE", $"Riot est injoignable : {exception.Message}");
        }

        if ((int)response.StatusCode is >= 400 and < 500)
        {
            throw AppException.Validation(clientErrorCode, "Riot a refusé la requête de boutique.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw AppException.Unavailable(
                "RIOT_STORE_UNAVAILABLE",
                $"Riot a répondu {(int)response.StatusCode}.");
        }

        try
        {
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }
        catch (JsonException exception)
        {
            throw AppException.Unavailable("RIOT_STORE_UNAVAILABLE", $"Réponse Riot illisible : {exception.Message}");
        }
    }

    private static IValorantStorePort.RawStorefront ParseStorefront(JsonElement data)
    {
        var offers = new List<IValorantStorePort.RawOffer>();
        long remainingSeconds = 0;

        if (data.TryGetProperty("SkinsPanelLayout", out var skinsPanel))
        {
            remainingSeconds = ReadInt64(skinsPanel, "SingleItemOffersRemainingDurationInSeconds");

            foreach (var offer in Array(skinsPanel, "SingleItemStoreOffers"))
            {
                if (Array(offer, "Rewards").FirstOrDefault() is { ValueKind: JsonValueKind.Object } reward
                    && ReadOptionalString(reward, "ItemID") is { } itemId)
                {
                    offers.Add(new IValorantStorePort.RawOffer(itemId, ReadCurrency(offer, "Cost")));
                }
            }
        }

        return new IValorantStorePort.RawStorefront(
            offers,
            remainingSeconds,
            ParseBundles(data),
            ParseNightMarket(data));
    }

    private static List<IValorantStorePort.RawBundle> ParseBundles(JsonElement data)
    {
        var bundles = new List<IValorantStorePort.RawBundle>();

        if (!data.TryGetProperty("FeaturedBundle", out var featured))
        {
            return bundles;
        }

        // Riot expose « Bundles » au pluriel, et parfois un unique « Bundle » : les deux formes
        // existent selon la rotation.
        var rawBundles = featured.TryGetProperty("Bundles", out var many) && many.ValueKind == JsonValueKind.Array
            ? many.EnumerateArray().ToList()
            : featured.TryGetProperty("Bundle", out var single) && single.ValueKind == JsonValueKind.Object
                ? [single]
                : [];

        foreach (var bundle in rawBundles)
        {
            var items = new List<IValorantStorePort.RawOffer>();

            foreach (var item in Array(bundle, "Items"))
            {
                if (!item.TryGetProperty("Item", out var meta)
                    || ReadOptionalString(meta, "ItemTypeID") != SkinTypeId
                    || ReadOptionalString(meta, "ItemID") is not { } itemId)
                {
                    continue;
                }

                // Le prix remisé prime ; sans lui, le prix de base. Un skin offert vaut 0.
                var price = item.TryGetProperty("DiscountedPrice", out var discounted)
                    ? ToInt32(discounted)
                    : ReadInt32(item, "BasePrice");

                items.Add(new IValorantStorePort.RawOffer(itemId, price));
            }

            var baseCost = ReadCurrency(bundle, "TotalBaseCost");

            bundles.Add(new IValorantStorePort.RawBundle(
                ReadOptionalString(bundle, "DataAssetID") ?? string.Empty,
                items,
                baseCost,
                bundle.TryGetProperty("TotalDiscountedCost", out _) ? ReadCurrency(bundle, "TotalDiscountedCost") : baseCost,
                ReadInt32(bundle, "TotalDiscountPercent"),
                bundle.TryGetProperty("DurationRemainingInSeconds", out var bundleRemaining)
                    ? ToInt64(bundleRemaining)
                    : ReadInt64(featured, "BundleRemainingDurationInSeconds")));
        }

        return bundles;
    }

    private static IValorantStorePort.RawNightMarket? ParseNightMarket(JsonElement data)
    {
        if (!data.TryGetProperty("BonusStore", out var bonusStore))
        {
            return null;
        }

        var nightMarketOffers = new List<IValorantStorePort.RawNightMarketOffer>();

        foreach (var bonusOffer in Array(bonusStore, "BonusStoreOffers"))
        {
            if (!bonusOffer.TryGetProperty("Offer", out var offer))
            {
                continue;
            }

            var itemId = Array(offer, "Rewards").FirstOrDefault() is { ValueKind: JsonValueKind.Object } reward
                ? ReadOptionalString(reward, "ItemID") ?? string.Empty
                : string.Empty;

            nightMarketOffers.Add(new IValorantStorePort.RawNightMarketOffer(
                ReadOptionalString(bonusOffer, "BonusOfferID") ?? string.Empty,
                itemId,
                ReadCurrency(offer, "Cost"),
                ReadCurrency(bonusOffer, "DiscountCosts"),
                ReadInt32(bonusOffer, "DiscountPercent"),
                bonusOffer.TryGetProperty("IsSeen", out var seen) && seen.ValueKind == JsonValueKind.True));
        }

        return new IValorantStorePort.RawNightMarket(
            nightMarketOffers,
            ReadInt64(bonusStore, "BonusStoreRemainingDurationInSeconds"));
    }

    // Les prix sont des dictionnaires indexés par devise : seul le montant en VP nous intéresse.
    private static int ReadCurrency(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var costs)
        && costs.ValueKind == JsonValueKind.Object
        && costs.TryGetProperty(VpCurrencyId, out var amount)
            ? ToInt32(amount)
            : 0;

    private static IEnumerable<JsonElement> Array(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var array) && array.ValueKind == JsonValueKind.Array
            ? array.EnumerateArray()
            : [];

    private static string? ReadOptionalString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) ? property.GetString() : null;

    private static int ReadInt32(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) ? ToInt32(property) : 0;

    private static long ReadInt64(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) ? ToInt64(property) : 0;

    // **Riot ne renvoie pas que des entiers.** TotalDiscountPercent porte une partie décimale, et
    // `GetInt32()` lève une FormatException dès que le JSON n'est pas un entier — là où le Java
    // passait par `Number.intValue()`, qui tronque sans rien dire. On tronque donc pareil, sur
    // tous les nombres : une valeur inattendue ne doit pas faire tomber toute la boutique.
    private static int ToInt32(JsonElement element) =>
        element.ValueKind == JsonValueKind.Number
            ? element.TryGetInt32(out var integer) ? integer : (int)element.GetDouble()
            : 0;

    private static long ToInt64(JsonElement element) =>
        element.ValueKind == JsonValueKind.Number
            ? element.TryGetInt64(out var integer) ? integer : (long)element.GetDouble()
            : 0;
}
