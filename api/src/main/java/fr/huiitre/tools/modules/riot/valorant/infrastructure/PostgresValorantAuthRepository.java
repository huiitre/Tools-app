package fr.huiitre.tools.modules.riot.valorant.infrastructure;

import fr.huiitre.tools.modules.riot.valorant.application.core.ports.ValorantAuthRepository;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.stereotype.Repository;

import java.sql.Timestamp;
import java.time.LocalDateTime;
import java.util.Optional;

@Repository
public class PostgresValorantAuthRepository implements ValorantAuthRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresValorantAuthRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public void save(long userId, String puuid, String region, String encryptedRefreshToken, String iv, LocalDateTime expiresAt) {
        final String sql = """
            INSERT INTO tools_riot.valorant_auth (user_id, puuid, region, encrypted_refresh, encryption_iv, expires_at, updated_at)
            VALUES (?, ?, ?, ?, ?, ?, now())
            ON CONFLICT (user_id) DO UPDATE SET
                puuid = EXCLUDED.puuid,
                region = EXCLUDED.region,
                encrypted_refresh = EXCLUDED.encrypted_refresh,
                encryption_iv = EXCLUDED.encryption_iv,
                expires_at = EXCLUDED.expires_at,
                updated_at = now()
        """;
        jdbcTemplate.update(sql, userId, puuid, region, encryptedRefreshToken, iv, Timestamp.valueOf(expiresAt));
    }

    @Override
    public Optional<ValorantAuthData> findByUserId(long userId) {
        final String sql = "SELECT puuid, region, encrypted_refresh, encryption_iv, expires_at FROM tools_riot.valorant_auth WHERE user_id = ?";
        return jdbcTemplate.query(sql, (rs, rowNum) -> new ValorantAuthData(
                rs.getString("puuid"),
                rs.getString("region"),
                rs.getString("encrypted_refresh"),
                rs.getString("encryption_iv"),
                rs.getTimestamp("expires_at").toLocalDateTime()
        ), userId).stream().findFirst();
    }

    @Override
    public void deleteByUserId(long userId) {
        final String sql = "DELETE FROM tools_riot.valorant_auth WHERE user_id = ?";
        jdbcTemplate.update(sql, userId);
    }

    @Override
    public java.util.List<Long> findAllUserIds() {
        final String sql = "SELECT user_id FROM tools_riot.valorant_auth";
        return jdbcTemplate.queryForList(sql, Long.class);
    }
}
