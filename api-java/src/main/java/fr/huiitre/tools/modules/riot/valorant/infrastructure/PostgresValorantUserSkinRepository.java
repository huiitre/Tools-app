package fr.huiitre.tools.modules.riot.valorant.infrastructure;

import fr.huiitre.tools.modules.riot.valorant.application.user.ports.ValorantUserSkinRepository;
import org.springframework.jdbc.core.JdbcTemplate;

public class PostgresValorantUserSkinRepository implements ValorantUserSkinRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresValorantUserSkinRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public Long add(Long accountId, Long skinId) {
        final String sql = """
                    INSERT INTO tools_riot.valorant_user_skins (valorant_account_id, skin_id)
                    VALUES (?, ?)
                    RETURNING id
                """;
        return jdbcTemplate.queryForObject(sql, Long.class, accountId, skinId);
    }

    @Override
    public void remove(Long accountId, Long skinId) {
        final String sql = """
                    DELETE FROM tools_riot.valorant_user_skins
                    WHERE valorant_account_id = ? AND skin_id = ?
                """;
        int affected = jdbcTemplate.update(sql, accountId, skinId);
        if (affected == 0) {
            throw new IllegalArgumentException("USER_SKIN_NOT_FOUND");
        }
    }

    @Override
    public boolean existsByAccountIdAndSkinId(Long accountId, Long skinId) {
        final String sql = """
                    SELECT COUNT(*)
                    FROM tools_riot.valorant_user_skins
                    WHERE valorant_account_id = ? AND skin_id = ?
                """;
        Integer count = jdbcTemplate.queryForObject(sql, Integer.class, accountId, skinId);
        return count != null && count > 0;
    }
}
