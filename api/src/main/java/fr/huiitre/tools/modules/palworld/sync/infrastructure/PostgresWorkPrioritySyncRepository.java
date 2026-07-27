package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.util.List;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.palworld.sync.application.WorkPrioritySyncData;
import fr.huiitre.tools.modules.palworld.sync.application.ports.WorkPrioritySyncRepository;
import fr.huiitre.tools.modules.palworld.sync.application.view.WorkPriorityRefView;

public class PostgresWorkPrioritySyncRepository implements WorkPrioritySyncRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresWorkPrioritySyncRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<WorkPriorityRefView> ROW_MAPPER = (rs, rowNum) -> new WorkPriorityRefView(
            rs.getLong("id"),
            rs.getString("code"),
            rs.getString("name"),
            rs.getString("icon_url"),
            (Long) rs.getObject("work_suitability_id"),
            rs.getInt("priority"));

    @Override
    public List<WorkPriorityRefView> findAll() {
        return jdbcTemplate.query(
                "SELECT id, code, name, icon_url, work_suitability_id, priority FROM tools_palworld.work_priority",
                ROW_MAPPER);
    }

    @Override
    public Long save(WorkPrioritySyncData data, Long workSuitabilityId) {
        final String sql = """
                INSERT INTO tools_palworld.work_priority (code, name, icon_url, work_suitability_id, priority)
                VALUES (?, ?, ?, ?, ?)
                RETURNING id
                """;
        return jdbcTemplate.queryForObject(sql, Long.class,
                data.getCode(), data.getName(), data.getIconUrl(), workSuitabilityId, data.getPriority());
    }

    @Override
    public void update(Long id, WorkPrioritySyncData data, Long workSuitabilityId) {
        final String sql = """
                UPDATE tools_palworld.work_priority
                SET name = ?, icon_url = ?, work_suitability_id = ?, priority = ?
                WHERE id = ?
                """;
        jdbcTemplate.update(sql, data.getName(), data.getIconUrl(), workSuitabilityId, data.getPriority(), id);
    }

    @Override
    public void delete(Long id) {
        jdbcTemplate.update("DELETE FROM tools_palworld.work_priority WHERE id = ?", id);
    }
}
