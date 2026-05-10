package fr.huiitre.tools.modules.riot.valorant.application.core.ports;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Optional;

public interface ValorantAuthRepository {
    void save(long userId, String puuid, String region, String encryptedRefreshToken, String iv, LocalDateTime expiresAt);
    Optional<ValorantAuthData> findByUserId(long userId);
    void deleteByUserId(long userId);
    List<Long> findAllUserIds();

    record ValorantAuthData(String puuid, String region, String encryptedRefreshToken, String iv, LocalDateTime expiresAt) {}
}
