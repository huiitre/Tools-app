package fr.huiitre.tools.modules.riot.valorant.infrastructure;

import fr.huiitre.tools.modules.riot.valorant.application.user.ports.ValorantUserSkinRepository;
import org.springframework.jdbc.core.JdbcTemplate;

public class PostgresValorantUserSkinRepository implements ValorantUserSkinRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresValorantUserSkinRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public Long add(Long userId, Long skinId) {
        final String sql = """
                    INSERT INTO tools_riot.valorant_user_skins (user_id, skin_id)
                    VALUES (?, ?)
                    RETURNING id
                """;
        return jdbcTemplate.queryForObject(sql, Long.class, userId, skinId);
    }

    @Override
    public void remove(Long userId, Long skinId) {
        final String sql = """
                    DELETE FROM tools_riot.valorant_user_skins
                    WHERE user_id = ? AND skin_id = ?
                """;
        int affected = jdbcTemplate.update(sql, userId, skinId);
        if (affected == 0) {
            throw new IllegalArgumentException("USER_SKIN_NOT_FOUND");
        }
    }

    @Override
    public boolean existsByUserIdAndSkinId(Long userId, Long skinId) {
        final String sql = """
                    SELECT COUNT(*)
                    FROM tools_riot.valorant_user_skins
                    WHERE user_id = ? AND skin_id = ?
                """;
        Integer count = jdbcTemplate.queryForObject(sql, Integer.class, userId, skinId);
        return count != null && count > 0;
    }
}
