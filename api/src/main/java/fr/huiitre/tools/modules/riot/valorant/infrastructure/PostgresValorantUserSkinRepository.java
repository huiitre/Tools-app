package fr.huiitre.tools.modules.riot.valorant.infrastructure;

import fr.huiitre.tools.modules.riot.valorant.application.ports.ValorantUserSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.view.ValorantUserSkinView;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import java.util.List;

public class PostgresValorantUserSkinRepository implements ValorantUserSkinRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresValorantUserSkinRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<ValorantUserSkinView> USER_SKIN_ROW_MAPPER = (rs, rowNum) ->
            new ValorantUserSkinView(
                    rs.getLong("id"),
                    rs.getLong("skin_id"),
                    rs.getString("name"),
                    rs.getString("icon_url"),
                    rs.getTimestamp("created_at").toLocalDateTime());

    @Override
    public List<ValorantUserSkinView> findAllByUserId(Long userId) {
        final String sql = """
                    SELECT us.id, us.skin_id, s.name, s.icon_url, us.created_at
                    FROM tools_riot.valorant_user_skins us
                    INNER JOIN tools_riot.valorant_weapon_skins s ON s.id = us.skin_id
                    WHERE us.user_id = ?
                    ORDER BY s.name ASC
                """;
        return jdbcTemplate.query(sql, USER_SKIN_ROW_MAPPER, userId);
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
