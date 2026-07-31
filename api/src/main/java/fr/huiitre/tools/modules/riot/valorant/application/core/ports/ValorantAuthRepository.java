package fr.huiitre.tools.modules.riot.valorant.application.core.ports;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Optional;

public interface ValorantAuthRepository {
    long save(long userId, String puuid, String region, String gameName, String tagLine, String label, String encryptedRefreshToken, String iv, LocalDateTime expiresAt);
    Optional<ValorantAuthData> findById(long accountId);
    List<ValorantAccountData> findAllByUserId(long userId);
    boolean existsByIdAndUserId(long accountId, long userId);
    boolean existsByUserIdAndPuuid(long userId, String puuid);
    void deleteById(long accountId);
    List<Long> findAllAccountIds();

    record ValorantAuthData(long userId, String puuid, String region, String encryptedRefreshToken, String iv, LocalDateTime expiresAt) {}

    record ValorantAccountData(long id, String puuid, String region, String gameName, String tagLine, String label) {}
}
