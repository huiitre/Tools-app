package fr.huiitre.tools.modules.dofus.subarea.infrastructure;

import java.util.List;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.dofus.subarea.application.ports.SubareaRepository;
import fr.huiitre.tools.modules.dofus.subarea.domain.Subarea;

public class PostgresSubareaRepository implements SubareaRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresSubareaRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<Subarea> SUBAREA_ROW_MAPPER = (rs, rowNum) -> Subarea.rehydrate(
            rs.getLong("id"),
            rs.getLong("asset_id"),
            rs.getLong("game_version_id"),
            rs.getLong("area_id"),
            rs.getString("name")
    );

    @Override
    public List<Subarea> findAllByGameVersionId(Long gameVersionId) {
        
        final String sql = """
            SELECT id, asset_id, game_version_id, area_id, name
            FROM tools_dofus.subarea
            WHERE game_version_id = ?
        """;

        return jdbcTemplate.query(sql, SUBAREA_ROW_MAPPER, gameVersionId);
    }

    @Override
    public void insert(Subarea subarea) {
        final String sql = """
            INSERT INTO tools_dofus.subarea (asset_id, game_version_id, area_id, name)
            VALUES (?, ?, ?, ?)
        """;
        jdbcTemplate.update(sql, subarea.getAssetId(), subarea.getGameVersionId(), subarea.getAreaId(), subarea.getName());
    }

    @Override
    public void update(Subarea subarea) {
        final String sql = """
            UPDATE tools_dofus.subarea
            SET asset_id = ?, game_version_id = ?, area_id = ?, name = ?
            WHERE id = ?
        """;
        jdbcTemplate.update(sql, subarea.getAssetId(), subarea.getGameVersionId(), subarea.getAreaId(), subarea.getName(), subarea.getId());
    }
}
