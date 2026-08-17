package fr.huiitre.tools.modules.riot.valorant.infrastructure;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantBundleRepository;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.ValorantBundleView;

public class PostgresValorantBundleRepository implements ValorantBundleRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresValorantBundleRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<ValorantBundleView> ROW_MAPPER = (rs, rowNum) -> new ValorantBundleView(
            rs.getLong("id"),
            rs.getObject("asset_id", UUID.class),
            rs.getString("name"),
            rs.getString("banner_url"));

    @Override
    public List<ValorantBundleView> findAll() {
        final String sql = """
                SELECT id, asset_id, name, banner_url
                FROM tools_riot.valorant_bundles
                ORDER BY name
                """;
        return jdbcTemplate.query(sql, ROW_MAPPER);
    }

    @Override
    public Optional<ValorantBundleView> findById(Long id) {
        final String sql = """
                SELECT id, asset_id, name, banner_url
                FROM tools_riot.valorant_bundles
                WHERE id = ?
                """;
        return jdbcTemplate.query(sql, ROW_MAPPER, id).stream().findFirst();
    }

    @Override
    public Optional<ValorantBundleView> findByAssetId(UUID assetId) {
        final String sql = """
                SELECT id, asset_id, name, banner_url
                FROM tools_riot.valorant_bundles
                WHERE asset_id = ?
                """;
        return jdbcTemplate.query(sql, ROW_MAPPER, assetId).stream().findFirst();
    }
}
