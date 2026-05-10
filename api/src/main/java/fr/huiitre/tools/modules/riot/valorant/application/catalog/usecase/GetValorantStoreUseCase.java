package fr.huiitre.tools.modules.riot.valorant.application.catalog.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.CurrentUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.security.infrastructure.EncryptionService;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantBundleRepository;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantStorePort;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.*;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.RiotAuthPort;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.ValorantAuthRepository;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.ValorantVersionProvider;
import fr.huiitre.tools.modules.riot.valorant.infrastructure.ValorantTokenParser;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

@Service
public class GetValorantStoreUseCase implements SecuredUseCase {

    private final ValorantAuthRepository valorantAuthRepository;
    private final EncryptionService encryptionService;
    private final RiotAuthPort riotAuthPort;
    private final ValorantStorePort valorantStorePort;
    private final ValorantVersionProvider versionProvider;
    private final ValorantSkinRepository skinRepository;
    private final ValorantBundleRepository bundleRepository;
    private final CurrentUserProvider currentUserProvider;
    private final ValorantTokenParser tokenParser;

    public GetValorantStoreUseCase(ValorantAuthRepository valorantAuthRepository,
                                   EncryptionService encryptionService,
                                   RiotAuthPort riotAuthPort,
                                   ValorantStorePort valorantStorePort,
                                   ValorantVersionProvider versionProvider,
                                   ValorantSkinRepository skinRepository,
                                   ValorantBundleRepository bundleRepository,
                                   CurrentUserProvider currentUserProvider,
                                   ValorantTokenParser tokenParser) {
        this.valorantAuthRepository = valorantAuthRepository;
        this.encryptionService = encryptionService;
        this.riotAuthPort = riotAuthPort;
        this.valorantStorePort = valorantStorePort;
        this.versionProvider = versionProvider;
        this.skinRepository = skinRepository;
        this.bundleRepository = bundleRepository;
        this.currentUserProvider = currentUserProvider;
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

    public ValorantStoreView execute(String providedAccessToken, String providedRegion) {
        long userId = Long.parseLong(currentUserProvider.getCurrentUserId());

        Optional<ValorantAuthRepository.ValorantAuthData> authDataOpt = valorantAuthRepository.findByUserId(userId);

        String accessToken;
        String puuid;
        String region;

        if (providedAccessToken != null && !providedAccessToken.isBlank()) {
            // Mode Manuel (Access Token fourni par le front)
            accessToken = providedAccessToken;
            puuid = tokenParser.extractPuuid(accessToken);
            region = (providedRegion != null && !providedRegion.isBlank()) 
                    ? providedRegion 
                    : authDataOpt.map(ValorantAuthRepository.ValorantAuthData::region).orElse("eu");
        } else {
            // Mode Persistant (On utilise ce qu'il y a en base)
            ValorantAuthRepository.ValorantAuthData authData = authDataOpt
                    .orElseThrow(() -> new IllegalArgumentException("RIOT_AUTH_NOT_FOUND"));
            
            accessToken = refreshAccessToken(userId, authData);
            puuid = authData.puuid();
            region = authData.region();
        }

        try {
            return fetchAndMapStore(userId, puuid, region, accessToken);
        } catch (IllegalArgumentException e) {
            // Si l'access token est expiré (401), on tente un refresh automatique si on a les infos en base
            if (("RIOT_ACCESS_TOKEN_INVALID".equals(e.getMessage()) || "RIOT_STOREFRONT_FETCH_FAILED".equals(e.getMessage())) 
                && authDataOpt.isPresent()) {
                accessToken = refreshAccessToken(userId, authDataOpt.get());
                return fetchAndMapStore(userId, puuid, region, accessToken);
            }
            throw e;
        }
    }

    private String refreshAccessToken(long userId, ValorantAuthRepository.ValorantAuthData authData) {
        String refreshToken = encryptionService.decrypt(authData.encryptedRefreshToken(), authData.iv());
        RiotAuthPort.ValorantAuthResponse riotResponse = riotAuthPort.refresh(refreshToken);

        // Rotation du refresh token
        String newIv = encryptionService.generateIv();
        String newEncryptedRefresh = encryptionService.encrypt(riotResponse.refreshToken(), newIv);
        valorantAuthRepository.save(userId, riotResponse.puuid(), authData.region(), newEncryptedRefresh, newIv, riotResponse.refreshTokenExpiresAt());

        return riotResponse.accessToken();
    }

    private ValorantStoreView fetchAndMapStore(long userId, String puuid, String region, String accessToken) {
        String entitlementsToken = valorantStorePort.fetchEntitlementsToken(accessToken);
        String clientVersion = versionProvider.getVersion().get("riotClientVersion").toString();

        ValorantStorePort.RawStorefront raw = valorantStorePort.fetchStorefront(puuid, region, accessToken, entitlementsToken, clientVersion);

        // Mapping des offres quotidiennes
        List<ValorantStoreOffer> offers = new ArrayList<>();
        for (ValorantStorePort.RawOffer o : raw.singleItemOffers()) {
            skinRepository.findByLevelAssetId(UUID.fromString(o.itemId()), userId)
                    .ifPresent(skin -> offers.add(new ValorantStoreOffer(skin, o.cost())));
        }

        // Mapping des bundles
        List<ValorantStoreBundle> bundles = new ArrayList<>();
        for (ValorantStorePort.RawBundle b : raw.featuredBundles()) {
            Optional<ValorantBundleView> bundleMeta = bundleRepository.findByAssetId(UUID.fromString(b.assetId()));
            
            List<ValorantStoreOffer> bundleItems = new ArrayList<>();
            for (ValorantStorePort.RawOffer i : b.items()) {
                skinRepository.findByLevelAssetId(UUID.fromString(i.itemId()), userId)
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
                skinRepository.findByLevelAssetId(UUID.fromString(o.itemId()), userId)
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
