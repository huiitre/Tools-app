package fr.huiitre.tools.modules.riot.valorant.infrastructure;

import fr.huiitre.tools.modules.riot.valorant.application.core.ports.ValorantAuthRepository;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.stereotype.Repository;

import java.sql.Timestamp;
import java.time.LocalDateTime;
import java.util.List;
import java.util.Optional;

@Repository
public class PostgresValorantAuthRepository implements ValorantAuthRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresValorantAuthRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public long save(long userId, String puuid, String region, String gameName, String tagLine, String label, String encryptedRefreshToken, String iv, LocalDateTime expiresAt) {
        final String sql = """
            INSERT INTO tools_riot.valorant_account (user_id, puuid, region, game_name, tag_line, label, encrypted_refresh, encryption_iv, expires_at, updated_at)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, now())
            ON CONFLICT (user_id, puuid) DO UPDATE SET
                region = EXCLUDED.region,
                game_name = COALESCE(EXCLUDED.game_name, tools_riot.valorant_account.game_name),
                tag_line = COALESCE(EXCLUDED.tag_line, tools_riot.valorant_account.tag_line),
                label = COALESCE(EXCLUDED.label, tools_riot.valorant_account.label),
                encrypted_refresh = EXCLUDED.encrypted_refresh,
                encryption_iv = EXCLUDED.encryption_iv,
                expires_at = EXCLUDED.expires_at,
                updated_at = now()
            RETURNING id
        """;
        return jdbcTemplate.queryForObject(sql, Long.class, userId, puuid, region, gameName, tagLine, label, encryptedRefreshToken, iv, Timestamp.valueOf(expiresAt));
    }

    @Override
    public Optional<ValorantAuthData> findById(long accountId) {
        final String sql = "SELECT user_id, puuid, region, encrypted_refresh, encryption_iv, expires_at FROM tools_riot.valorant_account WHERE id = ?";
        return jdbcTemplate.query(sql, (rs, rowNum) -> new ValorantAuthData(
                rs.getLong("user_id"),
                rs.getString("puuid"),
                rs.getString("region"),
                rs.getString("encrypted_refresh"),
                rs.getString("encryption_iv"),
                rs.getTimestamp("expires_at").toLocalDateTime()
        ), accountId).stream().findFirst();
    }

    @Override
    public List<ValorantAccountData> findAllByUserId(long userId) {
        final String sql = "SELECT id, puuid, region, game_name, tag_line, label FROM tools_riot.valorant_account WHERE user_id = ? ORDER BY created_at";
        return jdbcTemplate.query(sql, (rs, rowNum) -> new ValorantAccountData(
                rs.getLong("id"),
                rs.getString("puuid"),
                rs.getString("region"),
                rs.getString("game_name"),
                rs.getString("tag_line"),
                rs.getString("label")
        ), userId);
    }

    @Override
    public boolean existsByIdAndUserId(long accountId, long userId) {
        final String sql = "SELECT EXISTS (SELECT 1 FROM tools_riot.valorant_account WHERE id = ? AND user_id = ?)";
        return Boolean.TRUE.equals(jdbcTemplate.queryForObject(sql, Boolean.class, accountId, userId));
    }

    @Override
    public boolean existsByUserIdAndPuuid(long userId, String puuid) {
        final String sql = "SELECT EXISTS (SELECT 1 FROM tools_riot.valorant_account WHERE user_id = ? AND puuid = ?)";
        return Boolean.TRUE.equals(jdbcTemplate.queryForObject(sql, Boolean.class, userId, puuid));
    }

    @Override
    public void deleteById(long accountId) {
        final String sql = "DELETE FROM tools_riot.valorant_account WHERE id = ?";
        jdbcTemplate.update(sql, accountId);
    }

    @Override
    public List<Long> findAllAccountIds() {
        final String sql = "SELECT id FROM tools_riot.valorant_account";
        return jdbcTemplate.queryForList(sql, Long.class);
    }
}
