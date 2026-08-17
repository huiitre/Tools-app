package fr.huiitre.tools.modules.core.feedback.infrastructure;

import java.util.List;
import java.util.Map;

import org.springframework.jdbc.core.RowMapper;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;

import fr.huiitre.tools.modules.core.feedback.application.port.FeedbackRepository;

public class PostgresFeedbackRepository implements FeedbackRepository {

    private final NamedParameterJdbcTemplate jdbcTemplate;

    public PostgresFeedbackRepository(NamedParameterJdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<FeedbackEntity> ROW_MAPPER = (rs, rowNum) -> new FeedbackEntity(
            rs.getLong("id"),
            rs.getLong("user_id"),
            rs.getString("user_name"),
            rs.getString("message"),
            rs.getBoolean("is_read"),
            rs.getTimestamp("created_at").toLocalDateTime());

    @Override
    public void save(Long userId, String message) {
        final String sql = "INSERT INTO tools_core.feedbacks (user_id, message) VALUES (:userId, :message)";
        jdbcTemplate.update(sql, Map.of("userId", userId, "message", message));
    }

    @Override
    public List<FeedbackEntity> findAllSortedByDateDesc() {
        final String sql = """
                    SELECT f.id, f.user_id, u.name as user_name, f.message, f.is_read, f.created_at
                    FROM tools_core.feedbacks f
                    JOIN tools_core.users u ON f.user_id = u.id
                    ORDER BY f.created_at DESC
                """;
        return jdbcTemplate.query(sql, ROW_MAPPER);
    }

    @Override
    public void deleteByIds(List<Long> ids) {
        if (ids == null || ids.isEmpty()) return;
        final String sql = "DELETE FROM tools_core.feedbacks WHERE id IN (:ids)";
        jdbcTemplate.update(sql, Map.of("ids", ids));
    }

    @Override
    public void updateReadStatus(List<Long> ids, boolean isRead) {
        if (ids == null || ids.isEmpty()) return;
        final String sql = "UPDATE tools_core.feedbacks SET is_read = :isRead WHERE id IN (:ids)";
        jdbcTemplate.update(sql, Map.of("ids", ids, "isRead", isRead));
    }
}
