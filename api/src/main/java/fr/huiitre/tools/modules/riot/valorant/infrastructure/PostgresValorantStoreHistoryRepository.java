package fr.huiitre.tools.modules.riot.valorant.infrastructure;

import fr.huiitre.tools.modules.riot.valorant.application.user.ports.ValorantStoreHistoryRepository;
import org.springframework.jdbc.core.JdbcTemplate;

import java.time.LocalDate;
import java.util.*;

public class PostgresValorantStoreHistoryRepository implements ValorantStoreHistoryRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresValorantStoreHistoryRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public Map<LocalDate, List<Long>> findAllRawByUserId(Long userId) {
        final String sql = """
                SELECT seen_at, skin_id
                FROM tools_riot.valorant_store_history
                WHERE user_id = ?
                ORDER BY seen_at DESC
                """;
        
        Map<LocalDate, List<Long>> history = new LinkedHashMap<>();
        jdbcTemplate.query(sql, rs -> {
            LocalDate date = rs.getDate("seen_at").toLocalDate();
            Long skinId = rs.getLong("skin_id");
            history.computeIfAbsent(date, k -> new ArrayList<>()).add(skinId);
        }, userId);
        
        return history;
    }

    @Override
    public Long add(Long userId, Long skinId, LocalDate seenAt) {
        final String sql = "INSERT INTO tools_riot.valorant_store_history (user_id, skin_id, seen_at) VALUES (?, ?, ?) RETURNING id";
        return jdbcTemplate.queryForObject(sql, Long.class, userId, skinId, seenAt);
    }

    @Override
    public boolean existsByUserIdAndSkinIdAndDate(Long userId, Long skinId, LocalDate seenAt) {
        final String sql = "SELECT EXISTS (SELECT 1 FROM tools_riot.valorant_store_history WHERE user_id = ? AND skin_id = ? AND seen_at = ?)";
        return Boolean.TRUE.equals(jdbcTemplate.queryForObject(sql, Boolean.class, userId, skinId, seenAt));
    }
}
