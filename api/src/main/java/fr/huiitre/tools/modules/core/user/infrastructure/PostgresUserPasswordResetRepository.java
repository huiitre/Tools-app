package fr.huiitre.tools.modules.core.user.infrastructure;

import java.time.LocalDateTime;
import java.util.Optional;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.core.auth.application.ports.UserPasswordResetRepository;

public class PostgresUserPasswordResetRepository implements UserPasswordResetRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresUserPasswordResetRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public void save(Long userId, String token, LocalDateTime expiresAt) {
        final String sql = """
                    INSERT INTO tools_core.user_password_reset (user_id, token, expires_at)
                    VALUES (?, ?, ?)
                """;

        jdbcTemplate.update(
                sql,
                userId,
                token,
                expiresAt);
    }

    @Override
    public Optional<Long> findUserIdByValidToken(String token, LocalDateTime now) {
        final String sql = """
                    SELECT user_id
                    FROM tools_core.user_password_reset
                    WHERE token = ? AND expires_at > ?
                """;

        return jdbcTemplate.query(
                sql,
                rs -> rs.next() ? Optional.of(rs.getLong("user_id")) : Optional.empty(),
                token,
                now);
    }

    @Override
    public void deleteByUserId(Long userId) {
        final String sql = """
                    DELETE FROM tools_core.user_password_reset
                    WHERE user_id = ?
                """;

        jdbcTemplate.update(sql, userId);
    }

    @Override
    public void deleteExpired(LocalDateTime now) {
        final String sql = """
                    DELETE FROM tools_core.user_password_reset
                    WHERE expires_at <= ?
                """;

        jdbcTemplate.update(sql, now);
    }
}
