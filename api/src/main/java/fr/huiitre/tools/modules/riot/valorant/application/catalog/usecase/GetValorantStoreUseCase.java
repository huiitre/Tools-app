package fr.huiitre.tools.modules.riot.valorant.application.catalog.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantBundleRepository;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantStorePort;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.*;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.ValorantAuthRepository;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.ValorantVersionProvider;
import fr.huiitre.tools.modules.riot.valorant.application.core.usecase.ValorantAuthService;
import fr.huiitre.tools.modules.riot.valorant.infrastructure.ValorantTokenParser;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

@Service
public class GetValorantStoreUseCase implements SecuredUseCase {

    private final ValorantAuthService valorantAuthService;
    private final ValorantAuthRepository valorantAuthRepository;
    private final ValorantStorePort valorantStorePort;
    private final ValorantVersionProvider versionProvider;
    private final ValorantSkinRepository skinRepository;
    private final ValorantBundleRepository bundleRepository;
    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final ValorantTokenParser tokenParser;

    public GetValorantStoreUseCase(ValorantAuthService valorantAuthService,
                                   ValorantAuthRepository valorantAuthRepository,
                                   ValorantStorePort valorantStorePort,
                                   ValorantVersionProvider versionProvider,
                                   ValorantSkinRepository skinRepository,
                                   ValorantBundleRepository bundleRepository,
                                   AuthenticatedUserProvider authenticatedUserProvider,
                                   ValorantTokenParser tokenParser) {
        this.valorantAuthService = valorantAuthService;
        this.valorantAuthRepository = valorantAuthRepository;
        this.valorantStorePort = valorantStorePort;
        this.versionProvider = versionProvider;
        this.skinRepository = skinRepository;
        this.bundleRepository = bundleRepository;
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.tokenParser = tokenParser;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.RIOT);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public ValorantStoreView execute(Long accountId, String providedAccessToken, String providedRegion) {
        String accessToken;
        String puuid;
        String region;

        if (providedAccessToken != null && !providedAccessToken.isBlank()) {
            // Mode Manuel (Access Token fourni par le front, sans compte lié)
            accessToken = providedAccessToken;
            puuid = tokenParser.extractPuuid(accessToken);
            region = (providedRegion != null && !providedRegion.isBlank()) ? providedRegion : "eu";
            return fetchAndMapStore(null, puuid, region, accessToken);
        }

        // Mode Persistant (compte Valorant lié)
        if (accountId == null) {
            throw new IllegalArgumentException("VALORANT_ACCOUNT_REQUIRED");
        }
        if (!valorantAuthRepository.existsByIdAndUserId(accountId, authenticatedUserProvider.getUserId())) {
            throw new IllegalArgumentException("VALORANT_ACCOUNT_NOT_FOUND");
        }

        ValorantAuthRepository.ValorantAuthData authData = valorantAuthRepository.findById(accountId)
                .orElseThrow(() -> new IllegalArgumentException("RIOT_AUTH_NOT_FOUND"));

        puuid = authData.puuid();
        region = authData.region();
        accessToken = valorantAuthService.getOrRefreshAccessToken(accountId);

        try {
            return fetchAndMapStore(accountId, puuid, region, accessToken);
        } catch (IllegalArgumentException e) {
            // Si l'access token est expiré (401), on tente un refresh automatique
            if ("RIOT_ACCESS_TOKEN_INVALID".equals(e.getMessage()) || "RIOT_STOREFRONT_FETCH_FAILED".equals(e.getMessage())) {
                accessToken = valorantAuthService.getOrRefreshAccessToken(accountId);
                return fetchAndMapStore(accountId, puuid, region, accessToken);
            }
            throw e;
        }
    }

    private ValorantStoreView fetchAndMapStore(Long accountId, String puuid, String region, String accessToken) {
        String entitlementsToken = valorantStorePort.fetchEntitlementsToken(accessToken);
        String clientVersion = versionProvider.getVersion().get("riotClientVersion").toString();

        ValorantStorePort.RawStorefront raw = valorantStorePort.fetchStorefront(puuid, region, accessToken, entitlementsToken, clientVersion);

        // Mapping des offres quotidiennes
        List<ValorantStoreOffer> offers = new ArrayList<>();
        for (ValorantStorePort.RawOffer o : raw.singleItemOffers()) {
            skinRepository.findByLevelAssetId(UUID.fromString(o.itemId()), accountId)
                    .ifPresent(skin -> offers.add(new ValorantStoreOffer(skin, o.cost())));
        }

        // Mapping des bundles
        List<ValorantStoreBundle> bundles = new ArrayList<>();
        for (ValorantStorePort.RawBundle b : raw.featuredBundles()) {
            Optional<ValorantBundleView> bundleMeta = bundleRepository.findByAssetId(UUID.fromString(b.assetId()));

            List<ValorantStoreOffer> bundleItems = new ArrayList<>();
            for (ValorantStorePort.RawOffer i : b.items()) {
                skinRepository.findByLevelAssetId(UUID.fromString(i.itemId()), accountId)
                        .ifPresent(skin -> bundleItems.add(new ValorantStoreOffer(skin, i.cost())));
            }

            bundles.add(new ValorantStoreBundle(
                    b.assetId(),
                    bundleMeta.map(ValorantBundleView::name).orElse("Pack inconnu"),
                    bundleMeta.map(ValorantBundleView::bannerUrl).orElse(""),
                    bundleItems,
                    b.totalBaseCost(),
                    b.totalDiscountedCost(),
                    b.discountPercent(),
                    b.remainingSeconds()
            ));
        }

        // Mapping Night Market
        ValorantNightMarket nightMarket = null;
        if (raw.nightMarket() != null) {
            List<ValorantNightMarketOffer> nmOffers = new ArrayList<>();
            for (ValorantStorePort.RawNightMarketOffer o : raw.nightMarket().offers()) {
                skinRepository.findByLevelAssetId(UUID.fromString(o.itemId()), accountId)
                        .ifPresent(skin -> nmOffers.add(new ValorantNightMarketOffer(
                                o.offerId(),
                                skin,
                                o.originalCost(),
                                o.discountedCost(),
                                o.discountPercent(),
                                o.isSeen()
                        )));
            }
            nightMarket = new ValorantNightMarket(nmOffers, raw.nightMarket().remainingSeconds());
        }

        return new ValorantStoreView(offers, raw.singleItemOffersRemainingDurationInSeconds(), bundles, nightMarket);
    }
}
