package fr.huiitre.tools.modules.riot.sync.infrastructure;

import java.util.List;
import java.util.UUID;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.riot.sync.application.ValorantBundleSyncData;
import fr.huiitre.tools.modules.riot.sync.application.ValorantBundleSyncRepository;
import fr.huiitre.tools.modules.riot.valorant.application.view.ValorantBundleView;

public class PostgresValorantBundleSyncRepository implements ValorantBundleSyncRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresValorantBundleSyncRepository(JdbcTemplate jdbcTemplate) {
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
                """;
        return jdbcTemplate.query(sql, ROW_MAPPER);
    }

    @Override
    public Long save(ValorantBundleSyncData data) {
        final String sql = """
                INSERT INTO tools_riot.valorant_bundles (asset_id, name, banner_url)
                VALUES (?, ?, ?)
                RETURNING id
                """;
        return jdbcTemplate.queryForObject(sql, Long.class,
                data.getAssetId(),
                data.getName(),
                data.getBannerUrl());
    }

    @Override
    public void update(Long id, ValorantBundleSyncData data) {
        final String sql = """
                UPDATE tools_riot.valorant_bundles
                SET name = ?, banner_url = ?
                WHERE id = ?
                """;
        jdbcTemplate.update(sql, data.getName(), data.getBannerUrl(), id);
    }

    @Override
    public void delete(Long id) {
        jdbcTemplate.update("DELETE FROM tools_riot.valorant_bundles WHERE id = ?", id);
    }
}
