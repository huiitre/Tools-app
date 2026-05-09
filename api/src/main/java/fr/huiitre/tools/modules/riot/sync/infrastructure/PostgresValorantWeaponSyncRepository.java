package fr.huiitre.tools.modules.riot.sync.infrastructure;

import java.util.List;
import java.util.UUID;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.riot.sync.application.ValorantWeaponSyncData;
import fr.huiitre.tools.modules.riot.sync.application.ValorantWeaponSyncRepository;
import fr.huiitre.tools.modules.riot.valorant.application.view.ValorantWeaponView;

public class PostgresValorantWeaponSyncRepository implements ValorantWeaponSyncRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresValorantWeaponSyncRepository(JdbcTemplate jdbcTemplate) {
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
                """;
        return jdbcTemplate.query(sql, ROW_MAPPER);
    }

    @Override
    public Long save(ValorantWeaponSyncData data) {
        final String sql = """
                INSERT INTO tools_riot.valorant_weapons (asset_id, name, category, default_skin_asset_id, display_icon_url)
                VALUES (?, ?, ?, ?, ?)
                RETURNING id
                """;
        return jdbcTemplate.queryForObject(sql, Long.class,
                data.getAssetId(),
                data.getName(),
                data.getCategory(),
                data.getDefaultSkinAssetId(),
                data.getDisplayIconUrl());
    }

    @Override
    public void update(Long id, ValorantWeaponSyncData data) {
        final String sql = """
                UPDATE tools_riot.valorant_weapons
                SET name = ?, category = ?, default_skin_asset_id = ?, display_icon_url = ?, updated_at = now()
                WHERE id = ?
                """;
        jdbcTemplate.update(sql,
                data.getName(),
                data.getCategory(),
                data.getDefaultSkinAssetId(),
                data.getDisplayIconUrl(),
                id);
    }

    @Override
    public void delete(Long id) {
        jdbcTemplate.update("DELETE FROM tools_riot.valorant_weapons WHERE id = ?", id);
    }
}
