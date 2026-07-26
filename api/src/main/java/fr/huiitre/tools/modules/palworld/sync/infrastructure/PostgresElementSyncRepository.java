package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.util.List;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.palworld.sync.application.ElementSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.ports.ElementSyncRepository;
import fr.huiitre.tools.modules.palworld.sync.application.view.ElementRefView;

public class PostgresElementSyncRepository implements ElementSyncRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresElementSyncRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<ElementRefView> ROW_MAPPER = (rs, rowNum) -> new ElementRefView(
            rs.getLong("id"),
            rs.getString("external_code"),
            rs.getString("name"),
            rs.getString("icon_url"));

    @Override
    public List<ElementRefView> findAll() {
        return jdbcTemplate.query("SELECT id, external_code, name, icon_url FROM tools_palworld.element", ROW_MAPPER);
    }

    @Override
    public Long save(ElementSyncData data) {
        final String sql = """
                INSERT INTO tools_palworld.element (external_code, name, icon_url)
                VALUES (?, ?, ?)
                RETURNING id
                """;
        return jdbcTemplate.queryForObject(sql, Long.class, data.getExternalCode(), data.getName(), data.getIconUrl());
    }

    @Override
    public void update(Long id, ElementSyncData data) {
        final String sql = """
                UPDATE tools_palworld.element
                SET name = ?, icon_url = ?
                WHERE id = ?
                """;
        jdbcTemplate.update(sql, data.getName(), data.getIconUrl(), id);
    }

    @Override
    public void delete(Long id) {
        jdbcTemplate.update("DELETE FROM tools_palworld.element WHERE id = ?", id);
    }
}
