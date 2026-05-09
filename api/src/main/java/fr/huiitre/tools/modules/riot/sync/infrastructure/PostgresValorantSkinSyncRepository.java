package fr.huiitre.tools.modules.riot.sync.infrastructure;

import fr.huiitre.tools.modules.riot.sync.application.ValorantSkinSyncData;
import fr.huiitre.tools.modules.riot.sync.application.ValorantSkinSyncRepository;
import fr.huiitre.tools.modules.riot.valorant.application.skin.view.ValorantSkinView;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import java.util.List;
import java.util.UUID;


public class PostgresValorantSkinSyncRepository implements ValorantSkinSyncRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresValorantSkinSyncRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<ValorantSkinView> ROW_MAPPER = (rs, rowNum) -> new ValorantSkinView(
            rs.getLong("id"),
            rs.getObject("asset_id", UUID.class),
            rs.getString("name"),
            rs.getString("icon_url"),
            rs.getObject("tier_uuid", UUID.class),
            rs.getObject("content_tier_uuid", UUID.class),
            rs.getObject("weapon_id", Long.class),
            List.of(),
            false,
            false,
            null,
            null);

    @Override
    public List<ValorantSkinView> findAll() {
        final String sql = """
                SELECT id, asset_id, name, icon_url, tier_uuid, content_tier_uuid, weapon_id
                FROM tools_riot.valorant_weapon_skins
                """;
        return jdbcTemplate.query(sql, ROW_MAPPER);
    }

    @Override
    public Long save(ValorantSkinSyncData data, Long weaponId) {
        final String sql = """
                INSERT INTO tools_riot.valorant_weapon_skins (asset_id, name, icon_url, tier_uuid, content_tier_uuid, weapon_id)
                VALUES (?, ?, ?, ?, ?, ?)
                RETURNING id
                """;
        return jdbcTemplate.queryForObject(sql, Long.class,
                data.getAssetId(),
                data.getName(),
                data.getIconUrl(),
                data.getTierUuid(),
                data.getContentTierUuid(),
                weaponId);
    }

    @Override
    public void update(Long id, ValorantSkinSyncData data, Long weaponId) {
        final String sql = """
                UPDATE tools_riot.valorant_weapon_skins
                SET name = ?, icon_url = ?, tier_uuid = ?, content_tier_uuid = ?, weapon_id = ?
                WHERE id = ?
                """;
        jdbcTemplate.update(sql,
                data.getName(),
                data.getIconUrl(),
                data.getTierUuid(),
                data.getContentTierUuid(),
                weaponId,
                id);
    }

    @Override
    public void delete(Long id) {
        jdbcTemplate.update("DELETE FROM tools_riot.valorant_weapon_skins WHERE id = ?", id);
    }
}
