package fr.huiitre.tools.modules.core.user.infrastructure;

import java.util.Optional;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.core.user.application.ports.UserCredentialsRepository;

public class PostgresUserCredentialsRepository implements UserCredentialsRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresUserCredentialsRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public void save(Long userId, String passwordHash) {
        final String sql = """
                    INSERT INTO tools_core.user_credentials (user_id, password_hash)
                    VALUES (?, ?)
                """;

        jdbcTemplate.update(sql, userId, passwordHash);
    }

    @Override
    public Optional<String> findPasswordHashByUserId(Long userId) {
        final String sql = """
                    SELECT password_hash
                    FROM tools_core.user_credentials
                    WHERE user_id = ?
                    LIMIT 1
                """;

        return jdbcTemplate
                .query(sql, rs -> rs.next() ? Optional.of(rs.getString("password_hash")) : Optional.empty(), userId);
    }

    @Override
    public void updatePassword(Long userId, String newPasswordHash) {
        final String sql = """
                    UPDATE tools_core.user_credentials
                    SET password_hash = ?
                    WHERE user_id = ?
                """;

        jdbcTemplate.update(sql, newPasswordHash, userId);
    }
}
