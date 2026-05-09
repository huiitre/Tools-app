package fr.huiitre.tools.modules.health.weight_log.infrastructure;

import java.util.List;
import java.util.Optional;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.health.weight_log.application.ports.WeightLogRepository;
import fr.huiitre.tools.modules.health.weight_log.application.view.WeightLogView;
import fr.huiitre.tools.modules.health.weight_log.domain.WeightLog;

public class PostgresWeightLogRepository implements WeightLogRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresWeightLogRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<WeightLog> WEIGHT_LOG_ROW_MAPPER = (rs, rowNum) -> new WeightLog(
            rs.getDouble("weight"),
            rs.getTimestamp("logged_at").toLocalDateTime(),
            rs.getString("notes"));

    private static final RowMapper<WeightLogView> WEIGHT_LOG_VIEW_ROW_MAPPER = (rs, rowNum) -> new WeightLogView(
            rs.getLong("id"),
            rs.getDouble("weight"),
            rs.getTimestamp("logged_at").toLocalDateTime(),
            rs.getString("notes"));

    @Override
    public void save(Long userId, WeightLog weightLog) {
        final String sql = """
                    INSERT INTO tools_health.weight_log (user_id, weight, logged_at, notes)
                    VALUES (?, ?, ?, ?)
                """;

        jdbcTemplate.update(
                sql,
                userId,
                weightLog.getWeight(),
                java.sql.Timestamp.valueOf(weightLog.getLogDate()),
                weightLog.getNotes());
    }

    @Override
    public void update(Long userId, Long weightLogId, WeightLog weightLog) {
        final String sql = """
                    UPDATE tools_health.weight_log
                    SET weight = ?, logged_at = ?, notes = ?
                    WHERE user_id = ? AND id = ?
                """;

        jdbcTemplate.update(
                sql,
                weightLog.getWeight(),
                java.sql.Timestamp.valueOf(weightLog.getLogDate()),
                weightLog.getNotes(),
                userId,
                weightLogId);
    }

    @Override
    public Optional<WeightLog> findById(Long userId, Long weightLogId) {
        final String sql = """
                    SELECT weight, logged_at, notes
                    FROM tools_health.weight_log
                    WHERE user_id = ? AND id = ?
                """;

        return jdbcTemplate
                .query(sql, WEIGHT_LOG_ROW_MAPPER, userId, weightLogId)
                .stream()
                .findFirst();
    }

    @Override
    public void delete(Long userId, Long weightLogId) {
        final String sql = """
                    DELETE FROM tools_health.weight_log
                    WHERE user_id = ? AND id = ?
                """;

        jdbcTemplate.update(sql, userId, weightLogId);
    }

    @Override
    public boolean existsById(Long userId, Long weightLogId) {
        final String sql = """
                    SELECT 1
                    FROM tools_health.weight_log
                    WHERE user_id = ? AND id = ?
                """;

        return Boolean.TRUE.equals(
                jdbcTemplate.query(
                        sql,
                        (rs, rowNum) -> true,
                        userId,
                        weightLogId).stream().findFirst().orElse(false));
    }

    @Override
    public List<WeightLogView> findAllByUserId(Long userId) {
        final String sql = """
                    SELECT id, weight, logged_at, notes
                    FROM tools_health.weight_log
                    WHERE user_id = ?
                    ORDER BY logged_at DESC
                """;

        return jdbcTemplate.query(sql, WEIGHT_LOG_VIEW_ROW_MAPPER, userId);
    }
}
