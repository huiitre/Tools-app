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
    public Map<LocalDate, List<Long>> findAllRawByAccountId(Long accountId) {
        final String sql = """
                SELECT seen_at, skin_id
                FROM tools_riot.valorant_store_history
                WHERE valorant_account_id = ?
                ORDER BY seen_at DESC
                """;

        Map<LocalDate, List<Long>> history = new LinkedHashMap<>();
        jdbcTemplate.query(sql, rs -> {
            LocalDate date = rs.getDate("seen_at").toLocalDate();
            Long skinId = rs.getLong("skin_id");
            history.computeIfAbsent(date, k -> new ArrayList<>()).add(skinId);
        }, accountId);

        return history;
    }

    @Override
    public Long add(Long accountId, Long skinId, LocalDate seenAt) {
        final String sql = "INSERT INTO tools_riot.valorant_store_history (valorant_account_id, skin_id, seen_at) VALUES (?, ?, ?) RETURNING id";
        return jdbcTemplate.queryForObject(sql, Long.class, accountId, skinId, seenAt);
    }

    @Override
    public boolean existsByAccountIdAndSkinIdAndDate(Long accountId, Long skinId, LocalDate seenAt) {
        final String sql = "SELECT EXISTS (SELECT 1 FROM tools_riot.valorant_store_history WHERE valorant_account_id = ? AND skin_id = ? AND seen_at = ?)";
        return Boolean.TRUE.equals(jdbcTemplate.queryForObject(sql, Boolean.class, accountId, skinId, seenAt));
    }
}
