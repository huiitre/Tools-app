package fr.huiitre.tools.modules.dofus.area.infrastructure;

import java.util.List;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.dofus.area.application.ports.AreaRepository;
import fr.huiitre.tools.modules.dofus.area.domain.Area;

public class PostgresAreaRepository implements AreaRepository {
    
    private final JdbcTemplate jdbcTemplate;

    public PostgresAreaRepository(
            JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<Area> AREA_ROW_MAPPER = (rs, rowNum) -> Area.rehydrate(
            rs.getLong("id"),
            rs.getLong("asset_id"),
            rs.getLong("game_version_id"),
            rs.getString("name")
    );

    @Override
    public List<Area> findAllByGameVersionId(Long gameVersionId) {
        
        final String sql = """
            SELECT id, asset_id, game_version_id, name
            FROM tools_dofus.area
            WHERE game_version_id = ?
        """;

        return jdbcTemplate.query(sql, AREA_ROW_MAPPER, gameVersionId);
    }

    @Override
    public void insert(Area area) {
        final String sql = """
            INSERT INTO tools_dofus.area (asset_id, game_version_id, name)
            VALUES (?, ?, ?)
        """;
        jdbcTemplate.update(sql, area.getAssetId(), area.getGameVersionId(), area.getName());
    }

    @Override
    public void update(Area area) {
        final String sql = """
            UPDATE tools_dofus.area
            SET asset_id = ?, game_version_id = ?, name = ?
            WHERE id = ?
        """;
        jdbcTemplate.update(sql, area.getAssetId(), area.getGameVersionId(), area.getName(), area.getId());
    }
}
