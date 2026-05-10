package fr.huiitre.tools.modules.riot.valorant.application.core.ports;

import java.time.LocalDateTime;
import java.util.Optional;

public interface ValorantAuthRepository {
    void save(long userId, String puuid, String region, String encryptedRefreshToken, String iv, LocalDateTime expiresAt);
    Optional<ValorantAuthData> findByUserId(long userId);
    void deleteByUserId(long userId);

    record ValorantAuthData(String puuid, String region, String encryptedRefreshToken, String iv, LocalDateTime expiresAt) {}
}
