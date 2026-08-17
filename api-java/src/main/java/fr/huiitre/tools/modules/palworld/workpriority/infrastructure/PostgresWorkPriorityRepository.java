package fr.huiitre.tools.modules.palworld.workpriority.infrastructure;

import java.util.List;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.palworld.workpriority.application.ports.WorkPriorityRepository;
import fr.huiitre.tools.modules.palworld.workpriority.application.view.WorkPriorityView;
import fr.huiitre.tools.modules.palworld.workpriority.application.view.WorkSuitabilityRefView;

public class PostgresWorkPriorityRepository implements WorkPriorityRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresWorkPriorityRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public List<WorkPriorityView> findAll() {
        final String sql = """
                SELECT wp.id, wp.code, wp.name, wp.icon_url, wp.priority,
                       ws.id AS ws_id, ws.slug AS ws_slug, ws.name AS ws_name, ws.icon_url AS ws_icon_url
                FROM tools_palworld.work_priority wp
                LEFT JOIN tools_palworld.work_suitability ws ON ws.id = wp.work_suitability_id
                ORDER BY wp.priority, wp.name
                """;
        return jdbcTemplate.query(sql, (rs, rowNum) -> {
            Long wsId = (Long) rs.getObject("ws_id");
            WorkSuitabilityRefView workSuitability = wsId == null
                    ? null
                    : new WorkSuitabilityRefView(wsId, rs.getString("ws_slug"), rs.getString("ws_name"), rs.getString("ws_icon_url"));
            return new WorkPriorityView(
                    rs.getLong("id"), rs.getString("code"), rs.getString("name"), rs.getString("icon_url"),
                    rs.getInt("priority"), workSuitability);
        });
    }
}
