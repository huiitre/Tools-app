package fr.huiitre.tools.modules.riot.valorant.infrastructure;

import fr.huiitre.tools.modules.riot.valorant.application.user.ports.ValorantWatchlistRepository;
import fr.huiitre.tools.modules.riot.valorant.application.user.view.ValorantWatchlistEntryView;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import java.util.List;

public class PostgresValorantWatchlistRepository implements ValorantWatchlistRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresValorantWatchlistRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<ValorantWatchlistEntryView> WATCHLIST_ROW_MAPPER = (rs, rowNum) ->
            new ValorantWatchlistEntryView(
                    rs.getLong("id"),
                    rs.getLong("skin_id"),
                    rs.getString("name"),
                    rs.getString("icon_url"),
                    rs.getTimestamp("created_at").toLocalDateTime());

    @Override
    public List<ValorantWatchlistEntryView> findAllByUserId(Long userId) {
        final String sql = """
                    SELECT w.id, w.skin_id, s.name, s.icon_url, w.created_at
                    FROM tools_riot.valorant_skin_watchlist w
                    INNER JOIN tools_riot.valorant_weapon_skins s ON s.id = w.skin_id
                    WHERE w.user_id = ?
                    ORDER BY w.created_at DESC
                """;
        return jdbcTemplate.query(sql, WATCHLIST_ROW_MAPPER, userId);
    }

    @Override
    public Long add(Long userId, Long skinId) {
        final String sql = """
                    INSERT INTO tools_riot.valorant_skin_watchlist (user_id, skin_id)
                    VALUES (?, ?)
                    RETURNING id
                """;
        return jdbcTemplate.queryForObject(sql, Long.class, userId, skinId);
    }

    @Override
    public void remove(Long userId, Long skinId) {
        final String sql = """
                    DELETE FROM tools_riot.valorant_skin_watchlist
                    WHERE user_id = ? AND skin_id = ?
                """;
        int affected = jdbcTemplate.update(sql, userId, skinId);
        if (affected == 0) {
            throw new IllegalArgumentException("WATCHLIST_ENTRY_NOT_FOUND");
        }
    }

    @Override
    public boolean existsByUserIdAndSkinId(Long userId, Long skinId) {
        final String sql = """
                    SELECT COUNT(*)
                    FROM tools_riot.valorant_skin_watchlist
                    WHERE user_id = ? AND skin_id = ?
                """;
        Integer count = jdbcTemplate.queryForObject(sql, Integer.class, userId, skinId);
        return count != null && count > 0;
    }
}
