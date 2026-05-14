package fr.huiitre.tools.modules.riot.valorant.application.user.usecase;

import fr.huiitre.tools.modules.core.notification.application.event.NotificationEvent;
import fr.huiitre.tools.modules.core.notification.domain.entity.NotificationType;
import fr.huiitre.tools.modules.core.user.application.ports.UserRepository;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantStorePort;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.ValorantVersionProvider;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.ValorantStoreOffer;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.ValorantAuthRepository;
import fr.huiitre.tools.modules.riot.valorant.application.core.usecase.ValorantAuthService;
import fr.huiitre.tools.modules.riot.valorant.application.skin.view.ValorantSkinView;
import fr.huiitre.tools.modules.riot.valorant.application.user.ports.ValorantStoreHistoryRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.stereotype.Service;

import java.time.Instant;
import java.time.LocalDate;
import java.time.ZoneOffset;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import java.util.stream.Collectors;

@Service
public class ValorantWatchlistNotifier {

    private static final Logger log = LoggerFactory.getLogger(ValorantWatchlistNotifier.class);

    private final ValorantAuthService valorantAuthService;
    private final ValorantAuthRepository valorantAuthRepository;
    private final ValorantStorePort valorantStorePort;
    private final ValorantVersionProvider versionProvider;
    private final ValorantSkinRepository skinRepository;
    private final ValorantStoreHistoryRepository storeHistoryRepository;
    private final ApplicationEventPublisher eventPublisher;

    public ValorantWatchlistNotifier(ValorantAuthService valorantAuthService,
                                   ValorantAuthRepository valorantAuthRepository,
                                   ValorantStorePort valorantStorePort,
                                   ValorantVersionProvider versionProvider,
                                   ValorantSkinRepository skinRepository,
                                   ValorantStoreHistoryRepository storeHistoryRepository,
                                   ApplicationEventPublisher eventPublisher) {
        this.valorantAuthService = valorantAuthService;
        this.valorantAuthRepository = valorantAuthRepository;
        this.valorantStorePort = valorantStorePort;
        this.versionProvider = versionProvider;
        this.skinRepository = skinRepository;
        this.storeHistoryRepository = storeHistoryRepository;
        this.eventPublisher = eventPublisher;
    }

    public void processAllUsers() {
        // On récupère uniquement ceux qui ont des credentials Valorant persistés
        List<Long> userIds = valorantAuthRepository.findAllUserIds();
        log.info("Starting Valorant Watchlist & History sync for {} users", userIds.size());

        for (Long userId : userIds) {
            try {
                processUser(userId);
            } catch (Exception e) {
                log.error("Error processing Valorant sync for user {}: {}", userId, e.getMessage());
            }
        }
    }

    private void processUser(Long userId) {
        Optional<ValorantAuthRepository.ValorantAuthData> authDataOpt = valorantAuthRepository.findByUserId(userId);
        if (authDataOpt.isEmpty()) return;

        ValorantAuthRepository.ValorantAuthData authData = authDataOpt.get();
        String accessToken = valorantAuthService.getOrRefreshAccessToken(userId);
        String entitlementsToken = valorantStorePort.fetchEntitlementsToken(accessToken);
        String clientVersion = versionProvider.getVersion().get("riotClientVersion").toString();

        ValorantStorePort.RawStorefront raw = valorantStorePort.fetchStorefront(
                authData.puuid(), authData.region(), accessToken, entitlementsToken, clientVersion
        );

        // 1. Archivage automatique dans l'historique
        LocalDate shopDate = calculateShopDate(raw.singleItemOffersRemainingDurationInSeconds());
        List<Long> skinDbIds = new ArrayList<>();

        for (ValorantStorePort.RawOffer offer : raw.singleItemOffers()) {
            skinRepository.findByLevelAssetId(UUID.fromString(offer.itemId()), userId)
                    .ifPresent(skin -> {
                        skinDbIds.add(skin.id());
                        if (!storeHistoryRepository.existsByUserIdAndSkinIdAndDate(userId, skin.id(), shopDate)) {
                            storeHistoryRepository.add(userId, skin.id(), shopDate);
                        }
                    });
        }

        // 2. Vérification Watchlist & Notification
        List<ValorantSkinView> watchlist = skinRepository.findAllWatchedByUserId(userId);
        List<ValorantSkinView> matches = watchlist.stream()
                .filter(w -> skinDbIds.contains(w.id()))
                .collect(Collectors.toList());

        if (!matches.isEmpty()) {
            sendNotification(userId, matches);
        }
    }

    private LocalDate calculateShopDate(long remainingSeconds) {
        // On se cale sur le milieu de la rotation pour stabiliser la date (comme au front)
        // Expiration - 12h
        return Instant.now()
                .plusSeconds(remainingSeconds)
                .minusSeconds(43200) // 12h
                .atZone(ZoneOffset.UTC)
                .toLocalDate();
    }

    private void sendNotification(Long userId, List<ValorantSkinView> matches) {
        String title = "Valorant Shop - Skins disponibles !";
        String body;
        
        if (matches.size() == 1) {
            body = "Le skin \"" + matches.get(0).name() + "\" est disponible dans ta boutique !";
        } else {
            String names = matches.stream()
                    .map(s -> "- " + s.name())
                    .collect(Collectors.joining("\n"));
            body = "Plusieurs skins de ta watchlist sont disponibles :\n" + names;
        }

        eventPublisher.publishEvent(new NotificationEvent(
                title,
                body,
                NotificationType.SUCCESS,
                userId,
                null,
                null,
                null,
                "{\"route\": \"valorant-shop\"}"
        ));
    }
}
