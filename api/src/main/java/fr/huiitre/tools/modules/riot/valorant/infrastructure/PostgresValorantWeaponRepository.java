package fr.huiitre.tools.modules.riot.valorant.infrastructure;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantWeaponRepository;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.ValorantWeaponView;

public class PostgresValorantWeaponRepository implements ValorantWeaponRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresValorantWeaponRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<ValorantWeaponView> ROW_MAPPER = (rs, rowNum) -> new ValorantWeaponView(
            rs.getLong("id"),
            rs.getObject("asset_id", UUID.class),
            rs.getString("name"),
            rs.getString("category"),
            rs.getObject("default_skin_asset_id", UUID.class),
            rs.getString("display_icon_url"));

    @Override
    public List<ValorantWeaponView> findAll() {
        final String sql = """
                SELECT id, asset_id, name, category, default_skin_asset_id, display_icon_url
                FROM tools_riot.valorant_weapons
                ORDER BY name
                """;
        return jdbcTemplate.query(sql, ROW_MAPPER);
    }

    @Override
    public Optional<ValorantWeaponView> findById(Long id) {
        final String sql = """
                SELECT id, asset_id, name, category, default_skin_asset_id, display_icon_url
                FROM tools_riot.valorant_weapons
                WHERE id = ?
                """;
        return jdbcTemplate.query(sql, ROW_MAPPER, id).stream().findFirst();
    }
}
