package fr.huiitre.tools.modules.core.auth.application.ports;

import java.time.LocalDateTime;
import java.util.Optional;

public interface UserPasswordResetRepository {

    void save(Long userId, String token, LocalDateTime expiresAt);

    Optional<Long> findUserIdByValidToken(String token, LocalDateTime now);

    void deleteByUserId(Long userId);

    void deleteExpired(LocalDateTime now);
}
