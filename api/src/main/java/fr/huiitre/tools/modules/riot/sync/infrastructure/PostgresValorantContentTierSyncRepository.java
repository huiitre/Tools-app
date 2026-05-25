package fr.huiitre.tools.modules.riot.sync.infrastructure;

import java.util.List;
import java.util.UUID;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.riot.sync.application.ValorantContentTierSyncData;
import fr.huiitre.tools.modules.riot.sync.application.ports.ValorantContentTierSyncRepository;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.ValorantContentTierView;

public class PostgresValorantContentTierSyncRepository implements ValorantContentTierSyncRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresValorantContentTierSyncRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<ValorantContentTierView> ROW_MAPPER = (rs, rowNum) -> new ValorantContentTierView(
            rs.getLong("id"),
            rs.getObject("asset_id", UUID.class),
            rs.getString("name"),
            rs.getString("dev_name"),
            rs.getInt("rank"),
            rs.getInt("juice_value"),
            rs.getInt("juice_cost"),
            rs.getString("highlight_color"),
            rs.getString("display_icon_url"));

    @Override
    public List<ValorantContentTierView> findAll() {
        final String sql = """
                SELECT id, asset_id, name, dev_name, rank, juice_value, juice_cost, highlight_color, display_icon_url
                FROM tools_riot.valorant_content_tiers
                """;
        return jdbcTemplate.query(sql, ROW_MAPPER);
    }

    @Override
    public Long save(ValorantContentTierSyncData data) {
        final String sql = """
                INSERT INTO tools_riot.valorant_content_tiers
                    (asset_id, name, dev_name, rank, juice_value, juice_cost, highlight_color, display_icon_url)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?)
                RETURNING id
                """;
        return jdbcTemplate.queryForObject(sql, Long.class,
                data.getAssetId(),
                data.getName(),
                data.getDevName(),
                data.getRank(),
                data.getJuiceValue(),
                data.getJuiceCost(),
                data.getHighlightColor(),
                data.getDisplayIconUrl());
    }

    @Override
    public void update(Long id, ValorantContentTierSyncData data) {
        final String sql = """
                UPDATE tools_riot.valorant_content_tiers
                SET name = ?, dev_name = ?, rank = ?, juice_value = ?, juice_cost = ?,
                    highlight_color = ?, display_icon_url = ?, updated_at = now()
                WHERE id = ?
                """;
        jdbcTemplate.update(sql,
                data.getName(),
                data.getDevName(),
                data.getRank(),
                data.getJuiceValue(),
                data.getJuiceCost(),
                data.getHighlightColor(),
                data.getDisplayIconUrl(),
                id);
    }

    @Override
    public void delete(Long id) {
        jdbcTemplate.update("DELETE FROM tools_riot.valorant_content_tiers WHERE id = ?", id);
    }
}