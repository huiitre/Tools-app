package fr.huiitre.tools.modules.riot.valorant.infrastructure;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import java.sql.Timestamp;
import java.time.LocalDateTime;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.skin.view.ValorantSkinLevelView;
import fr.huiitre.tools.modules.riot.valorant.application.skin.view.ValorantSkinView;

public class PostgresValorantSkinRepository implements ValorantSkinRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresValorantSkinRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final String SELECT_WITH_LEVELS = """
            SELECT s.id, s.asset_id, s.name, s.icon_url, s.tier_uuid, s.content_tier_uuid, s.weapon_id,
                   l.asset_id AS level_asset_id, l.level_index, l.display_icon_url, l.streamed_video_url,
                   (us.id IS NOT NULL) as owned, us.created_at as owned_at,
                   (w.id IS NOT NULL) as watched, w.created_at as watched_at
            FROM tools_riot.valorant_weapon_skins s
            LEFT JOIN tools_riot.valorant_skin_levels l ON l.skin_id = s.id
            LEFT JOIN tools_riot.valorant_user_skins us ON us.skin_id = s.id AND us.user_id = ?
            LEFT JOIN tools_riot.valorant_skin_watchlist w ON w.skin_id = s.id AND w.user_id = ?
            """;

    @Override
    public List<ValorantSkinView> findAll(Long userId) {
        final String sql = SELECT_WITH_LEVELS + " ORDER BY s.name, l.level_index";
        return buildMany(sql, userId, userId);
    }

    @Override
    public Optional<ValorantSkinView> findById(Long id, Long userId) {
        final String sql = SELECT_WITH_LEVELS + " WHERE s.id = ? ORDER BY l.level_index";
        return buildSingle(sql, userId, userId, id);
    }

    @Override
    public Optional<ValorantSkinView> findByAssetId(UUID assetId, Long userId) {
        final String sql = SELECT_WITH_LEVELS + " WHERE s.asset_id = ? ORDER BY l.level_index";
        return buildSingle(sql, userId, userId, assetId);
    }

    @Override
    public Optional<ValorantSkinView> findByLevelAssetId(UUID levelAssetId, Long userId) {
        final String sql = SELECT_WITH_LEVELS + """
                 WHERE s.id = (SELECT skin_id FROM tools_riot.valorant_skin_levels WHERE asset_id = ?)
                 ORDER BY l.level_index
                """;
        return buildSingle(sql, userId, userId, levelAssetId);
    }

    @Override
    public List<ValorantSkinView> findAllByWeaponId(Long weaponId, Long userId) {
        final String sql = SELECT_WITH_LEVELS + " WHERE s.weapon_id = ? ORDER BY s.name, l.level_index";
        return buildMany(sql, userId, userId, weaponId);
    }

    @Override
    public List<ValorantSkinView> findAllByTierUuid(UUID tierUuid, Long userId) {
        final String sql = SELECT_WITH_LEVELS + " WHERE s.tier_uuid = ? ORDER BY s.name, l.level_index";
        return buildMany(sql, userId, userId, tierUuid);
    }

    @Override
    public List<ValorantSkinView> findAllOwnedByUserId(Long userId) {
        final String sql = SELECT_WITH_LEVELS + " WHERE us.id IS NOT NULL ORDER BY s.name, l.level_index";
        return buildMany(sql, userId, userId);
    }

    @Override
    public List<ValorantSkinView> findAllWatchedByUserId(Long userId) {
        final String sql = SELECT_WITH_LEVELS + " WHERE w.id IS NOT NULL ORDER BY s.name, l.level_index";
        return buildMany(sql, userId, userId);
    }

    private List<ValorantSkinView> buildMany(String sql, Object... params) {
        return jdbcTemplate.query(sql, rs -> {
            LinkedHashMap<Long, ValorantSkinView> map = new LinkedHashMap<>();
            List<ValorantSkinLevelView> levels = new ArrayList<>();
            long lastSkinId = -1;

            while (rs.next()) {
                long skinId = rs.getLong("id");

                if (skinId != lastSkinId) {
                    if (lastSkinId != -1) {
                        rebuildLast(map, lastSkinId, levels);
                        levels = new ArrayList<>();
                    }
                    map.put(skinId, new ValorantSkinView(
                            skinId,
                            rs.getObject("asset_id", UUID.class),
                            rs.getString("name"),
                            rs.getString("icon_url"),
                            rs.getObject("tier_uuid", UUID.class),
                            rs.getObject("content_tier_uuid", UUID.class),
                            rs.getObject("weapon_id", Long.class),
                            new ArrayList<>(),
                            rs.getBoolean("owned"),
                            rs.getBoolean("watched"),
                            toLocalDateTime(rs.getTimestamp("owned_at")),
                            toLocalDateTime(rs.getTimestamp("watched_at"))));
                    lastSkinId = skinId;
                }

                String levelAssetIdStr = rs.getString("level_asset_id");
                if (levelAssetIdStr != null) {
                    levels.add(new ValorantSkinLevelView(
                            UUID.fromString(levelAssetIdStr),
                            rs.getInt("level_index"),
                            rs.getString("display_icon_url"),
                            rs.getString("streamed_video_url")));
                }
            }

            if (lastSkinId != -1) {
                rebuildLast(map, lastSkinId, levels);
            }

            return new ArrayList<>(map.values());
        }, params);
    }

    private Optional<ValorantSkinView> buildSingle(String sql, Object... params) {
        return jdbcTemplate.query(sql, rs -> {
            ValorantSkinView skin = null;
            List<ValorantSkinLevelView> levels = new ArrayList<>();

            while (rs.next()) {
                if (skin == null) {
                    skin = new ValorantSkinView(
                            rs.getLong("id"),
                            rs.getObject("asset_id", UUID.class),
                            rs.getString("name"),
                            rs.getString("icon_url"),
                            rs.getObject("tier_uuid", UUID.class),
                            rs.getObject("content_tier_uuid", UUID.class),
                            rs.getObject("weapon_id", Long.class),
                            levels,
                            rs.getBoolean("owned"),
                            rs.getBoolean("watched"),
                            toLocalDateTime(rs.getTimestamp("owned_at")),
                            toLocalDateTime(rs.getTimestamp("watched_at")));
                }

                String levelAssetIdStr = rs.getString("level_asset_id");
                if (levelAssetIdStr != null) {
                    levels.add(new ValorantSkinLevelView(
                            UUID.fromString(levelAssetIdStr),
                            rs.getInt("level_index"),
                            rs.getString("display_icon_url"),
                            rs.getString("streamed_video_url")));
                }
            }

            return Optional.ofNullable(skin);
        }, params);
    }

    private void rebuildLast(LinkedHashMap<Long, ValorantSkinView> map, long skinId, List<ValorantSkinLevelView> levels) {
        ValorantSkinView prev = map.get(skinId);
        map.put(skinId, new ValorantSkinView(
                prev.id(), prev.assetId(), prev.name(), prev.iconUrl(),
                prev.tierUuid(), prev.contentTierUuid(), prev.weaponId(), List.copyOf(levels),
                prev.owned(), prev.watched(), prev.ownedAt(), prev.watchedAt()));
    }

    private LocalDateTime toLocalDateTime(Timestamp ts) {
        return ts != null ? ts.toLocalDateTime() : null;
    }
}
