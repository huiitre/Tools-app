package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.util.List;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.palworld.sync.application.WorkSuitabilitySyncData;
import fr.huiitre.tools.modules.palworld.sync.application.ports.WorkSuitabilitySyncRepository;
import fr.huiitre.tools.modules.palworld.sync.application.view.WorkSuitabilityRefView;

public class PostgresWorkSuitabilitySyncRepository implements WorkSuitabilitySyncRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresWorkSuitabilitySyncRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<WorkSuitabilityRefView> ROW_MAPPER = (rs, rowNum) -> new WorkSuitabilityRefView(
            rs.getLong("id"),
            rs.getString("external_code"),
            rs.getString("slug"),
            rs.getString("name"),
            rs.getString("icon_url"));

    @Override
    public List<WorkSuitabilityRefView> findAll() {
        return jdbcTemplate.query("SELECT id, external_code, slug, name, icon_url FROM tools_palworld.work_suitability", ROW_MAPPER);
    }

    @Override
    public Long save(WorkSuitabilitySyncData data) {
        final String sql = """
                INSERT INTO tools_palworld.work_suitability (external_code, slug, name, icon_url)
                VALUES (?, ?, ?, ?)
                RETURNING id
                """;
        return jdbcTemplate.queryForObject(sql, Long.class,
                data.getExternalCode(), data.getSlug(), data.getName(), data.getIconUrl());
    }

    @Override
    public void update(Long id, WorkSuitabilitySyncData data) {
        final String sql = """
                UPDATE tools_palworld.work_suitability
                SET external_code = ?, name = ?, icon_url = ?
                WHERE id = ?
                """;
        jdbcTemplate.update(sql, data.getExternalCode(), data.getName(), data.getIconUrl(), id);
    }

    @Override
    public void delete(Long id) {
        jdbcTemplate.update("DELETE FROM tools_palworld.work_suitability WHERE id = ?", id);
    }
}
