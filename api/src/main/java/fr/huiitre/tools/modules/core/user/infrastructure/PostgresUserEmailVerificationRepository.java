package fr.huiitre.tools.modules.core.user.infrastructure;

import java.time.LocalDateTime;
import java.util.Optional;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.core.auth.application.ports.UserEmailVerificationRepository;

public class PostgresUserEmailVerificationRepository implements UserEmailVerificationRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresUserEmailVerificationRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public void deleteByUserId(Long userId) {
        final String sql = """
                    DELETE FROM tools_core.user_email_verification
                    WHERE user_id = ?
                """;

        jdbcTemplate.update(sql, userId);
    }

    @Override
    public void save(Long userId, String token, LocalDateTime expiresAt) {
        final String sql = """
                    INSERT INTO tools_core.user_email_verification (user_id, token, expires_at)
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
                    FROM tools_core.user_email_verification
                    WHERE token = ?
                      AND expires_at > ?
                    LIMIT 1
                """;

        return jdbcTemplate.query(
                sql,
                rs -> rs.next() ? Optional.of(rs.getLong("user_id")) : Optional.empty(),
                token,
                now);
    }

    @Override
    public void deleteExpired(LocalDateTime now) {
        final String sql = """
                    DELETE FROM tools_core.user_email_verification
                    WHERE expires_at <= ?
                """;

        jdbcTemplate.update(sql, now);
    }

    @Override
    public Optional<LocalDateTime> findLastCreatedAtByUserId(Long userId) {
        final String sql = """
                    SELECT created_at
                    FROM tools_core.user_email_verification
                    WHERE user_id = ?
                    ORDER BY created_at DESC
                    LIMIT 1
                """;

        return jdbcTemplate.query(
                sql,
                rs -> rs.next() ? Optional.of(rs.getTimestamp("created_at").toLocalDateTime()) : Optional.empty(),
                userId);
    }
}
