package fr.huiitre.tools.modules.riot.valorant.infrastructure;

import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantStorePort;
import org.springframework.core.ParameterizedTypeReference;
import org.springframework.http.*;
import org.springframework.web.client.HttpClientErrorException;
import org.springframework.web.client.RestTemplate;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;

public class ValorantStoreHttpAdapter implements ValorantStorePort {

    private static final String ENTITLEMENTS_URL = "https://entitlements.auth.riotgames.com/api/token/v1";
    private static final String STOREFRONT_URL_TEMPLATE = "https://pd.%s.a.pvp.net/store/v3/storefront/%s";
    
    private static final String VP_CURRENCY_ID = "85ad13f7-3d1b-5128-9eb2-7cd8ee0b5741";
    private static final String SKIN_TYPE_ID = "e7c63390-eda7-46e0-bb7a-a6abdacd2433";
    private static final String CLIENT_PLATFORM = "ew0KCSJwbGF0Zm9ybVR5cGUiOiAiUEMiLA0KCSJwbGF0Zm9ybU9TIjogIldpbmRvd3MiLA0KCSJwbGF0Zm9ybU9TVmVyc2lvbiI6ICIxMC4wLjE5MDQyLjEuMjU2LjY0Yml0IiwNCgkicGxhdGZvcm1DaGlwc2V0IjogIlVua25vd24iDQp9";

    private final RestTemplate restTemplate;

    public ValorantStoreHttpAdapter(RestTemplate restTemplate) {
        this.restTemplate = restTemplate;
    }

    @Override
    public String fetchEntitlementsToken(String accessToken) {
        HttpHeaders headers = new HttpHeaders();
        headers.setBearerAuth(accessToken);
        headers.setContentType(MediaType.APPLICATION_JSON);

        HttpEntity<String> request = new HttpEntity<>("{}", headers);

        try {
            ResponseEntity<Map<String, Object>> response = restTemplate.exchange(
                    ENTITLEMENTS_URL, HttpMethod.POST, request,
                    new ParameterizedTypeReference<>() {});

            Map<String, Object> body = response.getBody();
            if (body == null || !body.containsKey("entitlements_token")) {
                throw new IllegalArgumentException("RIOT_ENTITLEMENTS_EMPTY_RESPONSE");
            }

            return (String) body.get("entitlements_token");
        } catch (HttpClientErrorException e) {
            throw new IllegalArgumentException("RIOT_ACCESS_TOKEN_INVALID");
        }
    }

    @Override
    public RawStorefront fetchStorefront(String puuid, String region, String accessToken, String entitlementsToken, String clientVersion) {
        String url = String.format(STOREFRONT_URL_TEMPLATE, region, puuid);
        
        HttpHeaders headers = new HttpHeaders();
        headers.setBearerAuth(accessToken);
        headers.set("X-Riot-Entitlements-JWT", entitlementsToken);
        headers.set("X-Riot-ClientPlatform", CLIENT_PLATFORM);
        headers.set("X-Riot-ClientVersion", clientVersion);
        headers.setContentType(MediaType.APPLICATION_JSON);

        HttpEntity<String> request = new HttpEntity<>("{}", headers);

        try {
            ResponseEntity<Map<String, Object>> response = restTemplate.exchange(
                    url, HttpMethod.POST, request,
                    new ParameterizedTypeReference<>() {});

            Map<String, Object> body = response.getBody();
            if (body == null) throw new IllegalArgumentException("RIOT_STOREFRONT_EMPTY_RESPONSE");

            return parseStorefront(body);
        } catch (HttpClientErrorException e) {
            throw new IllegalArgumentException("RIOT_STOREFRONT_FETCH_FAILED");
        }
    }

    private RawStorefront parseStorefront(Map<String, Object> data) {
        // 1. Single Item Offers
        List<RawOffer> offers = new ArrayList<>();
        Map<String, Object> skinsPanel = (Map<String, Object>) data.get("SkinsPanelLayout");
        if (skinsPanel != null) {
            List<Map<String, Object>> singleOffers = (List<Map<String, Object>>) skinsPanel.get("SingleItemStoreOffers");
            if (singleOffers != null) {
                for (Map<String, Object> o : singleOffers) {
                    List<Map<String, Object>> rewards = (List<Map<String, Object>>) o.get("Rewards");
                    Map<String, Integer> cost = (Map<String, Integer>) o.get("Cost");
                    if (rewards != null && !rewards.isEmpty()) {
                        String itemId = (String) rewards.get(0).get("ItemID");
                        int price = cost != null ? cost.getOrDefault(VP_CURRENCY_ID, 0) : 0;
                        offers.add(new RawOffer(itemId, price));
                    }
                }
            }
        }
        long remainingOffers = skinsPanel != null ? ((Number) skinsPanel.getOrDefault("SingleItemOffersRemainingDurationInSeconds", 0L)).longValue() : 0;

        // 2. Bundles
        List<RawBundle> bundles = new ArrayList<>();
        Map<String, Object> featured = (Map<String, Object>) data.get("FeaturedBundle");
        if (featured != null) {
            List<Map<String, Object>> rawBundles = (List<Map<String, Object>>) featured.get("Bundles");
            if (rawBundles == null && featured.containsKey("Bundle")) {
                rawBundles = List.of((Map<String, Object>) featured.get("Bundle"));
            }
            
            if (rawBundles != null) {
                for (Map<String, Object> b : rawBundles) {
                    long bundleRemaining = ((Number) b.getOrDefault("DurationRemainingInSeconds", 
                            featured.getOrDefault("BundleRemainingDurationInSeconds", 0L))).longValue();
                    
                    List<RawOffer> bundleItems = new ArrayList<>();
                    List<Map<String, Object>> items = (List<Map<String, Object>>) b.get("Items");
                    if (items != null) {
                        for (Map<String, Object> i : items) {
                            Map<String, Object> itemMeta = (Map<String, Object>) i.get("Item");
                            if (itemMeta != null && SKIN_TYPE_ID.equals(itemMeta.get("ItemTypeID"))) {
                                String itemId = (String) itemMeta.get("ItemID");
                                int price = ((Number) i.getOrDefault("DiscountedPrice", i.getOrDefault("BasePrice", 0))).intValue();
                                bundleItems.add(new RawOffer(itemId, price));
                            }
                        }
                    }

                    Map<String, Integer> totalBaseCost = (Map<String, Integer>) b.get("TotalBaseCost");
                    Map<String, Integer> totalDiscountedCost = (Map<String, Integer>) b.get("TotalDiscountedCost");
                    
                    int base = totalBaseCost != null ? totalBaseCost.getOrDefault(VP_CURRENCY_ID, 0) : 0;
                    int disc = totalDiscountedCost != null ? totalDiscountedCost.getOrDefault(VP_CURRENCY_ID, base) : base;

                    bundles.add(new RawBundle(
                            (String) b.get("DataAssetID"),
                            bundleItems,
                            base,
                            disc,
                            ((Number) b.getOrDefault("TotalDiscountPercent", 0)).intValue(),
                            bundleRemaining
                    ));
                }
            }
        }

        // 3. Night Market
        RawNightMarket nightMarket = null;
        Map<String, Object> bonusStore = (Map<String, Object>) data.get("BonusStore");
        if (bonusStore != null) {
            List<RawNightMarketOffer> nmOffers = new ArrayList<>();
            List<Map<String, Object>> rawNmOffers = (List<Map<String, Object>>) bonusStore.get("BonusStoreOffers");
            if (rawNmOffers != null) {
                for (Map<String, Object> o : rawNmOffers) {
                    Map<String, Object> offer = (Map<String, Object>) o.get("Offer");
                    List<Map<String, Object>> rewards = (List<Map<String, Object>>) offer.get("Rewards");
                    Map<String, Integer> discCosts = (Map<String, Integer>) o.get("DiscountCosts");
                    Map<String, Integer> origCosts = (Map<String, Integer>) offer.get("Cost");
                    
                    String itemId = rewards != null && !rewards.isEmpty() ? (String) rewards.get(0).get("ItemID") : "";
                    int discPrice = discCosts != null ? discCosts.getOrDefault(VP_CURRENCY_ID, 0) : 0;
                    int origPrice = origCosts != null ? origCosts.getOrDefault(VP_CURRENCY_ID, 0) : 0;

                    nmOffers.add(new RawNightMarketOffer(
                            (String) o.get("BonusOfferID"),
                            itemId,
                            origPrice,
                            discPrice,
                            ((Number) o.getOrDefault("DiscountPercent", 0)).intValue(),
                            (Boolean) o.getOrDefault("IsSeen", false)
                    ));
                }
            }
            nightMarket = new RawNightMarket(nmOffers, ((Number) bonusStore.getOrDefault("BonusStoreRemainingDurationInSeconds", 0L)).longValue());
        }

        return new RawStorefront(offers, remainingOffers, bundles, nightMarket);
    }
}
