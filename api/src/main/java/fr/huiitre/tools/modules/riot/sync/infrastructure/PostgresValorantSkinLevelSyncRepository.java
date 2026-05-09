package fr.huiitre.tools.modules.riot.sync.infrastructure;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.riot.sync.application.ValorantSkinLevelSyncData;
import fr.huiitre.tools.modules.riot.sync.application.ValorantSkinLevelSyncRepository;

public class PostgresValorantSkinLevelSyncRepository implements ValorantSkinLevelSyncRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresValorantSkinLevelSyncRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public void deleteAll() {
        jdbcTemplate.update("DELETE FROM tools_riot.valorant_skin_levels");
    }

    @Override
    public void save(Long skinId, ValorantSkinLevelSyncData data) {
        final String sql = """
                INSERT INTO tools_riot.valorant_skin_levels
                    (skin_id, asset_id, level_index, name, level_item, display_icon_url, streamed_video_url)
                VALUES (?, ?, ?, ?, ?, ?, ?)
                """;
        jdbcTemplate.update(sql,
                skinId,
                data.getAssetId(),
                data.getLevelIndex(),
                data.getName(),
                data.getLevelItem(),
                data.getDisplayIconUrl(),
                data.getStreamedVideoUrl());
    }
}
