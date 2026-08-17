package fr.huiitre.tools.modules.palworld.serverdata.infrastructure;

import java.util.List;
import java.util.UUID;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.palworld.serverdata.application.ports.PalInstanceQueryRepository;
import fr.huiitre.tools.modules.palworld.serverdata.application.view.PalInstanceSnapshotView;
import fr.huiitre.tools.modules.palworld.serverdata.application.view.PalInstanceSummaryView;

public class PostgresPalInstanceQueryRepository implements PalInstanceQueryRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresPalInstanceQueryRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<PalInstanceSummaryView> SUMMARY_ROW_MAPPER = (rs, rowNum) -> new PalInstanceSummaryView(
            rs.getObject("instance_id", UUID.class),
            rs.getString("character_id"),
            (Long) rs.getObject("pal_id"),
            rs.getString("pal_name"),
            rs.getString("pal_image_url"),
            (Integer) rs.getObject("pal_food_amount"),
            rs.getBoolean("is_alpha"),
            rs.getObject("owner_player_uid", UUID.class),
            (Integer) rs.getObject("level"),
            (Integer) rs.getObject("exp"),
            rs.getBigDecimal("full_stomach"),
            (Boolean) rs.getObject("is_sick"),
            rs.getString("workable_type"),
            rs.getString("task_id"),
            (Integer) rs.getObject("work_state"),
            rs.getBigDecimal("current_work_amount"),
            rs.getBigDecimal("required_work_amount"),
            rs.getObject("first_seen_at", java.time.OffsetDateTime.class),
            rs.getObject("last_seen_at", java.time.OffsetDateTime.class));

    private static final RowMapper<PalInstanceSnapshotView> SNAPSHOT_ROW_MAPPER = (rs, rowNum) -> new PalInstanceSnapshotView(
            rs.getObject("captured_at", java.time.OffsetDateTime.class),
            (Integer) rs.getObject("level"),
            (Integer) rs.getObject("exp"),
            rs.getBigDecimal("full_stomach"),
            (Boolean) rs.getObject("is_sick"),
            rs.getString("workable_type"),
            rs.getString("task_id"),
            (Integer) rs.getObject("work_state"),
            rs.getBigDecimal("current_work_amount"),
            rs.getBigDecimal("required_work_amount"));

    @Override
    public List<PalInstanceSummaryView> findByBaseId(UUID baseId) {
        final String sql = """
                SELECT pi.instance_id, pi.character_id, pi.pal_id, p.name AS pal_name, p.image_url AS pal_image_url,
                       p.food_amount AS pal_food_amount,
                       pi.is_alpha, pi.owner_player_uid, pi.level, pi.exp, pi.full_stomach, pi.is_sick,
                       pi.workable_type, pi.task_id, pi.work_state, pi.current_work_amount, pi.required_work_amount,
                       pi.first_seen_at, pi.last_seen_at
                FROM tools_palworld.pal_instance pi
                LEFT JOIN tools_palworld.pal p ON p.id = pi.pal_id
                WHERE pi.base_id = ?
                ORDER BY p.name NULLS LAST, pi.character_id
                """;
        return jdbcTemplate.query(sql, SUMMARY_ROW_MAPPER, baseId);
    }

    @Override
    public List<PalInstanceSnapshotView> findHistoryByInstanceId(UUID instanceId) {
        final String sql = """
                SELECT captured_at, level, exp, full_stomach, is_sick, workable_type, task_id, work_state,
                       current_work_amount, required_work_amount
                FROM tools_palworld.pal_instance_snapshot
                WHERE instance_id = ?
                ORDER BY captured_at
                """;
        return jdbcTemplate.query(sql, SNAPSHOT_ROW_MAPPER, instanceId);
    }
}
