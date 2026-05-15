package fr.huiitre.tools.modules.riot.sync.infrastructure;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.riot.sync.application.ValorantSkinChromaSyncData;
import fr.huiitre.tools.modules.riot.sync.application.ValorantSkinChromaSyncRepository;

public class PostgresValorantSkinChromaSyncRepository implements ValorantSkinChromaSyncRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresValorantSkinChromaSyncRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public void deleteAll() {
        jdbcTemplate.update("DELETE FROM tools_riot.valorant_skin_chromas");
    }

    @Override
    public void save(Long skinId, ValorantSkinChromaSyncData data) {
        final String sql = """
                INSERT INTO tools_riot.valorant_skin_chromas
                    (skin_id, asset_id, chroma_index, name, display_icon_url, full_render_url, swatch_url, streamed_video_url)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?)
                """;
        jdbcTemplate.update(sql,
                skinId,
                data.getAssetId(),
                data.getChromaIndex(),
                data.getName(),
                data.getDisplayIconUrl(),
                data.getFullRenderUrl(),
                data.getSwatchUrl(),
                data.getStreamedVideoUrl());
    }
}
