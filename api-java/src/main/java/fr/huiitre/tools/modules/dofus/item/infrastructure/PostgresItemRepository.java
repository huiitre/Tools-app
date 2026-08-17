package fr.huiitre.tools.modules.dofus.item.infrastructure;

import java.nio.file.Path;
import java.util.ArrayList;
import java.util.Collection;
import java.util.Comparator;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.function.Function;
import java.util.stream.Collectors;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;
import org.springframework.jdbc.core.namedparam.MapSqlParameterSource;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;

import fr.huiitre.tools.modules.core.filesystem.infrastructure.FileSystemChecker;
import fr.huiitre.tools.modules.dofus.area.application.dto.AreaDto;
import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;
import fr.huiitre.tools.modules.dofus.item.application.dto.FarmZoneDto;
import fr.huiitre.tools.modules.dofus.item.application.dto.ItemDto;
import fr.huiitre.tools.modules.dofus.item.application.dto.ItemImageDto;
import fr.huiitre.tools.modules.dofus.item.application.dto.ItemLightDTO;
import fr.huiitre.tools.modules.dofus.item.application.ports.ItemRepository;
import fr.huiitre.tools.modules.dofus.item.domain.Item;
import fr.huiitre.tools.modules.dofus.itemtype.application.view.ItemTypeDto;
import fr.huiitre.tools.modules.dofus.monster.application.dto.MonsterDto;
import fr.huiitre.tools.modules.dofus.subarea.application.dto.SubareaDto;

public class PostgresItemRepository implements ItemRepository {

    @Value("${tools.assets.base-path}")
    private Path assetsBasePath;

    private static final Logger logger = LoggerFactory.getLogger(PostgresItemRepository.class);

    private final JdbcTemplate jdbcTemplate;

    private final NamedParameterJdbcTemplate namedParameterJdbcTemplate;

    public PostgresItemRepository(JdbcTemplate jdbcTemplate, NamedParameterJdbcTemplate namedParameterJdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
        this.namedParameterJdbcTemplate = namedParameterJdbcTemplate;
    }

    private static final RowMapper<Item> ITEM_ROW_MAPPER = (rs, rowNum) -> Item.rehydrate(
            rs.getLong("id"),
            rs.getLong("asset_id"),
            rs.getLong("game_version_id"),
            rs.getLong("item_type_id"),
            rs.getString("name"),
            rs.getLong("level"),
            rs.getString("description"));

    @Override
    public List<Item> findAllByGameVersionId(Long gameVersionId) {
        final String sql = """
                    SELECT
                        id,
                        asset_id,
                        game_version_id,
                        item_type_id,
                        name,
                        level,
                        description
                    FROM tools_dofus.item
                    WHERE game_version_id = ?
                """;

        return jdbcTemplate.query(sql, ITEM_ROW_MAPPER, gameVersionId);
    }

    @Override
    public Long save(Item item) {
        final String sql = """
                    INSERT INTO tools_dofus.item (
                        asset_id,
                        game_version_id,
                        item_type_id,
                        name,
                        level,
                        description
                    ) VALUES (?, ?, ?, ?, ?, ?)
                    RETURNING id
                """;

        return jdbcTemplate.queryForObject(
                sql,
                Long.class,
                item.getAssetId(),
                item.getGameVersionId(),
                item.getItemTypeId(),
                item.getName(),
                item.getLevel(),
                item.getDescription());
    }

    @Override
    public void update(Item item) {
        final String sql = """
                    UPDATE tools_dofus.item
                    SET
                        asset_id = ?,
                        game_version_id = ?,
                        item_type_id = ?,
                        name = ?,
                        level = ?,
                        description = ?
                    WHERE id = ? AND game_version_id = ?
                """;

        jdbcTemplate.update(
                sql,
                item.getAssetId(),
                item.getGameVersionId(),
                item.getItemTypeId(),
                item.getName(),
                item.getLevel(),
                item.getDescription(),
                item.getId(),
                item.getGameVersionId());
    }

    @Override
    public boolean refreshImages(Long itemId, Long iconId, GameVersionData gameVersionData) {
        ImageExistence images = checkItemImagesExistence(iconId, gameVersionData);

        final String deleteSql = """
                    DELETE FROM tools_dofus.item_image
                    WHERE item_id = ?
                """;

        final String insertSql = """
                    INSERT INTO tools_dofus.item_image (
                        item_id,
                        icon_id,
                        resolution
                    ) VALUES (?, ?, ?)
                """;

        jdbcTemplate.update(deleteSql, itemId);

        if (images.has1x()) {
            jdbcTemplate.update(insertSql, itemId, iconId, "X1");
        }

        if (images.has2x()) {
            jdbcTemplate.update(insertSql, itemId, iconId, "X2");
        }

        return true;
    }

    private ImageExistence checkItemImagesExistence(Long iconId, GameVersionData gameVersionData) {

        if ("retro".equals(gameVersionData.getCode())) {
            Path image = assetsBasePath.resolve(
                    "tools_dofus/retro/img/items/" + iconId + ".svg");

            boolean exists = FileSystemChecker.exists(image);
            return new ImageExistence(exists, exists);
        }

        Path image1x = assetsBasePath.resolve(
                "tools_dofus/dofus3/img/item/1x/" + iconId + "-64.png");

        Path image2x = assetsBasePath.resolve(
                "tools_dofus/dofus3/img/item/2x/" + iconId + "-128.png");

        return new ImageExistence(
                FileSystemChecker.exists(image1x),
                FileSystemChecker.exists(image2x));
    }

    @Override
    public Optional<Item> findByAssetId(Long assetId, long gameVersionId) {
        final String sql = """
                    SELECT
                        id,
                        asset_id,
                        game_version_id,
                        item_type_id,
                        name,
                        level,
                        description
                    FROM tools_dofus.item
                    WHERE asset_id = ? AND game_version_id = ?
                """;

        List<Item> items = jdbcTemplate.query(sql, ITEM_ROW_MAPPER, assetId, gameVersionId);

        if (items.isEmpty()) {
            return Optional.empty();
        } else {
            return Optional.of(items.get(0));
        }
    }

    private static final RowMapper<ItemDto> ITEM_VIEW_ROW_MAPPER = (rs, rowNum) -> {

        ItemTypeDto itemTypeDto = new ItemTypeDto(
                rs.getLong("item_type_id"),
                rs.getLong("item_type_asset_id"),
                rs.getLong("item_type_game_version_id"),
                rs.getString("item_type_name"));

        return new ItemDto(
                rs.getLong("id"),
                rs.getString("name"),
                rs.getString("description"),
                rs.getBoolean("has_recipe"),
                rs.getLong("asset_id"),
                rs.getLong("game_version_id"),
                rs.getLong("level"),
                itemTypeDto,
                List.of(),
                null,
                null,
                null);
    };

    private static final RowMapper<ItemImageDto> ITEM_IMAGE_DTO_ROW_MAPPER = (rs, rowNum) -> {

        return new ItemImageDto(
                rs.getLong("id"),
                rs.getLong("item_id"),
                rs.getString("resolution"),
                rs.getLong("icon_id"));
    };

    @Override
    public List<ItemImageDto> findImageByItemId(Long itemId) {
        final String sql = """
                    SELECT
                        id,
                        item_id,
                        icon_id,
                        resolution
                    FROM tools_dofus.item_image
                    WHERE item_id = ?
                """;

        return jdbcTemplate.query(
                sql,
                ITEM_IMAGE_DTO_ROW_MAPPER,
                itemId);
    }

    @Override
    public List<ItemImageDto> findImageByItemIds(Collection<Long> itemIds) {
        if (itemIds == null || itemIds.isEmpty()) {
            return List.of();
        }

        final String sql = """
                    SELECT
                        id,
                        item_id,
                        icon_id,
                        resolution
                    FROM tools_dofus.item_image
                    WHERE item_id = ANY(:itemIds)
                """;

        MapSqlParameterSource params = new MapSqlParameterSource()
                .addValue("itemIds", itemIds.toArray(new Long[0]));

        return namedParameterJdbcTemplate.query(sql, params, ITEM_IMAGE_DTO_ROW_MAPPER);
    }

    @Override
    public Map<Long, ItemDto> findByGameVersionIdAndItemIds(Long gameVersionId, Collection<Long> itemIds) {
        if (itemIds == null || itemIds.isEmpty()) {
            return Map.of();
        }

        final String sql = """
                SELECT
                    i.id,
                    i.asset_id,
                    i.game_version_id,
                    i.name,
                    i.level,
                    i.description,
                    it.id AS item_type_id,
                    it.asset_id AS item_type_asset_id,
                    it.name AS item_type_name,
                    it.game_version_id AS item_type_game_version_id,
                    EXISTS (
                        SELECT 1
                        FROM tools_dofus.recipe r
                        WHERE r.item_id = i.id
                    ) AS has_recipe
                FROM tools_dofus.item i
                JOIN tools_dofus.item_type it
                    ON i.item_type_id = it.id
                    AND i.game_version_id = it.game_version_id
                WHERE i.game_version_id = :gameVersionId
                AND i.id = ANY(:itemIds)
                """;

        MapSqlParameterSource params = new MapSqlParameterSource()
                .addValue("gameVersionId", gameVersionId)
                .addValue("itemIds", itemIds.toArray(new Long[0]));

        List<ItemDto> items = namedParameterJdbcTemplate.query(
                sql,
                params,
                ITEM_VIEW_ROW_MAPPER);

        return items.stream()
                .collect(Collectors.toMap(ItemDto::getId, Function.identity()));
    }

    private static final RowMapper<ItemLightDTO> ITEM_LIGHT_ROW_MAPPER = (rs, rowNum) -> {

        ItemTypeDto itemTypeDto = new ItemTypeDto(
                rs.getLong("item_type_id"),
                rs.getLong("item_type_asset_id"),
                rs.getLong("item_type_game_version_id"),
                rs.getString("item_type_name"));

        return new ItemLightDTO(
                rs.getLong("id"),
                rs.getString("name"),
                rs.getLong("level"),
                rs.getLong("game_version_id"),
                rs.getLong("asset_id"),
                itemTypeDto,
                List.of());
    };

    @Override
    public List<ItemLightDTO> findLightByAssetIds(Long gameVersionId, Collection<Long> assetIds) {
        if (assetIds == null || assetIds.isEmpty()) {
            return List.of();
        }

        final String sql = """
                SELECT
                    i.id,
                    i.asset_id,
                    i.game_version_id,
                    i.name,
                    i.level,
                    it.id AS item_type_id,
                    it.asset_id AS item_type_asset_id,
                    it.name AS item_type_name,
                    it.game_version_id AS item_type_game_version_id
                FROM tools_dofus.item i
                JOIN tools_dofus.item_type it
                    ON i.item_type_id = it.id
                    AND i.game_version_id = it.game_version_id
                WHERE i.game_version_id = :gameVersionId
                AND i.asset_id = ANY(:assetIds)
                """;

        MapSqlParameterSource params = new MapSqlParameterSource()
                .addValue("gameVersionId", gameVersionId)
                .addValue("assetIds", assetIds.toArray(new Long[0]));

        return namedParameterJdbcTemplate.query(
                sql,
                params,
                ITEM_LIGHT_ROW_MAPPER);
    }

    @Override
    public Map<Long, List<FarmZoneDto>> findFarmZonesByItemIds(Collection<Long> itemIds) {
        if (itemIds == null || itemIds.isEmpty()) {
            return Map.of();
        }

        final String sql = """
                    SELECT DISTINCT
                        md.item_id,

                        a.id AS area_id,
                        a.asset_id AS area_asset_id,
                        a.game_version_id AS area_game_version_id,
                        a.name AS area_name,

                        s.id AS subarea_id,
                        s.asset_id AS subarea_asset_id,
                        s.name AS subarea_name,

                        m.id AS monster_id,
                        m.name AS monster_name

                    FROM tools_dofus.monster_drop md
                    INNER JOIN tools_dofus.monster m ON md.monster_id = m.id
                    INNER JOIN tools_dofus.monster_subarea ms ON m.id = ms.monster_id
                    INNER JOIN tools_dofus.subarea s ON ms.subarea_id = s.id
                    INNER JOIN tools_dofus.area a ON s.area_id = a.id

                    WHERE md.item_id = ANY(:itemIds)
                    AND a.name NOT IN ('Expéditions')

                    ORDER BY md.item_id, a.name, s.name, m.name
                """;

        MapSqlParameterSource params = new MapSqlParameterSource()
                .addValue("itemIds", itemIds.toArray(new Long[0]));

        List<Map<String, Object>> rows = namedParameterJdbcTemplate.queryForList(sql, params);

        Map<Long, Map<String, FarmZoneDto>> farmZonesByItemIdAndKey = new HashMap<>();

        for (Map<String, Object> row : rows) {
            Long itemId = ((Number) row.get("item_id")).longValue();
            Long areaId = ((Number) row.get("area_id")).longValue();
            Long subareaId = ((Number) row.get("subarea_id")).longValue();

            String key = areaId + "-" + subareaId;

            farmZonesByItemIdAndKey
                    .computeIfAbsent(itemId, k -> new HashMap<>())
                    .compute(key, (k, existingZone) -> {
                        if (existingZone == null) {
                            AreaDto area = new AreaDto(
                                    areaId,
                                    ((Number) row.get("area_asset_id")).longValue(),
                                    (String) row.get("area_name"));

                            SubareaDto subarea = new SubareaDto(
                                    subareaId,
                                    areaId,
                                    ((Number) row.get("subarea_asset_id")).longValue(),
                                    (String) row.get("subarea_name"));

                            existingZone = new FarmZoneDto(area, subarea, new ArrayList<>(), false);
                        }

                        MonsterDto monster = new MonsterDto(
                                ((Number) row.get("monster_id")).longValue(),
                                (String) row.get("monster_name"),
                                null);

                        existingZone.getMonsters().add(monster);

                        return existingZone;
                    });
        }

        Map<Long, List<FarmZoneDto>> result = new HashMap<>();

        for (Map.Entry<Long, Map<String, FarmZoneDto>> entry : farmZonesByItemIdAndKey.entrySet()) {
            Long itemId = entry.getKey();
            List<FarmZoneDto> zones = new ArrayList<>(entry.getValue().values());

            zones.sort(Comparator.comparing(z -> -z.getMonsters().size()));

            if (!zones.isEmpty()) {
                FarmZoneDto primary = zones.get(0);
                zones.set(0, new FarmZoneDto(
                        primary.getArea(),
                        primary.getSubarea(),
                        primary.getMonsters(),
                        true));
            }

            result.put(itemId, zones);
        }

        return result;
    }

    @Override
    public List<ItemDto> findCraftableItemsByGameVersionIdAndName(Long gameVersionId, Long workshopId, String query) {
        final String sql = """
            SELECT
                i.id,
                i.asset_id,
                i.game_version_id,
                i.name,
                i.level,
                i.description,
                it.id AS item_type_id,
                it.asset_id AS item_type_asset_id,
                it.name AS item_type_name,
                it.game_version_id AS item_type_game_version_id,
                TRUE AS has_recipe
            FROM tools_dofus.item i
            JOIN tools_dofus.item_type it
                ON i.item_type_id = it.id
                AND i.game_version_id = it.game_version_id
            WHERE i.game_version_id = ?
            AND EXISTS (
                SELECT 1
                FROM tools_dofus.recipe r
                WHERE r.item_id = i.id
            )
            AND NOT EXISTS (
                SELECT 1
                FROM tools_dofus.workshop_item wi
                WHERE wi.item_id = i.id
                AND wi.workshop_id = ?
            )
            AND i.name ILIKE ?
            ORDER BY i.name
            LIMIT 100
        """;
        
        if (query == null || query.isBlank()) {
            query = "";
        }
        String likeQuery = "%" + query + "%";

        return jdbcTemplate.query(
            sql,
            ITEM_VIEW_ROW_MAPPER,
            gameVersionId,
            workshopId,
            likeQuery);
    }

    private record ImageExistence(boolean has1x, boolean has2x) {
    }
}
