package fr.huiitre.tools.modules.riot.valorant.infrastructure;

import fr.huiitre.tools.modules.riot.valorant.application.user.ports.ValorantStoreHistoryRepository;
import fr.huiitre.tools.modules.riot.valorant.application.user.view.ValorantStoreHistoryView;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import java.util.List;

public class PostgresValorantStoreHistoryRepository implements ValorantStoreHistoryRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresValorantStoreHistoryRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<ValorantStoreHistoryView> STORE_HISTORY_ROW_MAPPER = (rs, rowNum) ->
            new ValorantStoreHistoryView(
                    rs.getLong("id"),
                    rs.getLong("skin_id"),
                    rs.getString("skin_name"),
                    rs.getString("skin_icon_url"),
                    rs.getDate("seen_at").toLocalDate()
            );

    @Override
    public List<ValorantStoreHistoryView> findAllByUserId(Long userId) {
        String sql = "SELECT sh.id, sh.skin_id, s.name as skin_name, s.icon_url as skin_icon_url, sh.seen_at FROM tools_riot.valorant_store_history sh JOIN tools_riot.valorant_weapon_skins s ON sh.skin_id = s.id WHERE sh.user_id = ? ORDER BY sh.seen_at DESC, sh.id DESC";
        return jdbcTemplate.query(sql, STORE_HISTORY_ROW_MAPPER, userId);
    }

    @Override
    public Long add(Long userId, Long skinId) {
        String sql = "INSERT INTO tools_riot.valorant_store_history (user_id, skin_id) VALUES (?, ?) RETURNING id";
        return jdbcTemplate.queryForObject(sql, Long.class, userId, skinId);
    }

    @Override
    public boolean existsByUserIdAndSkinIdAndDate(Long userId, Long skinId) {
        String sql = "SELECT EXISTS (SELECT 1 FROM tools_riot.valorant_store_history WHERE user_id = ? AND skin_id = ? AND seen_at = CURRENT_DATE)";
        return Boolean.TRUE.equals(jdbcTemplate.queryForObject(sql, Boolean.class, userId, skinId));
    }
}
