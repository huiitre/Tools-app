package fr.huiitre.tools.modules.dofus.catalogue.infrastructure;

import java.util.List;
import java.util.Map;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.jdbc.core.RowMapper;
import org.springframework.jdbc.core.namedparam.MapSqlParameterSource;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;

import fr.huiitre.tools.modules.dofus.catalogue.api.dto.CatalogueSearchQuery;
import fr.huiitre.tools.modules.dofus.catalogue.application.ports.CatalogueItemRepository;
import fr.huiitre.tools.modules.dofus.item.application.dto.ItemDto;
import fr.huiitre.tools.modules.dofus.itemtype.application.view.ItemTypeDto;

public class PostgresCatalogueItemRepository implements CatalogueItemRepository {

    private final NamedParameterJdbcTemplate jdbcTemplate;
    private final static Logger logger = LoggerFactory.getLogger(PostgresCatalogueItemRepository.class);

    public PostgresCatalogueItemRepository(NamedParameterJdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<ItemDto> CATALOGUE_ITEM_DTO_ROW_MAPPER = (rs, rowNum) -> {
        ItemTypeDto itemType = new ItemTypeDto(
                rs.getLong("item_type_id"),
                rs.getLong("type_asset_id"),
                rs.getLong("game_version_id"),
                rs.getString("type_name"));

        return new ItemDto(
                rs.getLong("id"),
                rs.getString("name"),
                rs.getString("description"),
                rs.getBoolean("has_recipe"),
                rs.getLong("asset_id"),
                rs.getLong("game_version_id"),
                rs.getLong("level"),
                itemType,
                null,
                null,
                null,
                null);
    };

    private static final RowMapper<ItemDto> CATALOGUE_INGREDIENT_ROW_MAPPER = (rs, rowNum) -> {
        ItemTypeDto itemType = new ItemTypeDto(
                rs.getLong("item_type_id"),
                rs.getLong("type_asset_id"),
                rs.getLong("game_version_id"),
                rs.getString("type_name"));

        return new ItemDto(
                rs.getLong("id"),
                rs.getString("name"),
                rs.getString("description"),
                rs.getBoolean("has_recipe"),
                rs.getLong("asset_id"),
                rs.getLong("game_version_id"),
                rs.getLong("level"),
                itemType,
                null,
                rs.getLong("parent_item_id"),
                rs.getLong("quantity"),
                null);
    };

    private static final Map<String, String> SORT_COLUMNS = Map.of(
            "name", "i.name",
            "level", "i.level",
            "type", "it.name",
            "asset_id", "i.asset_id",
            "id", "i.id");

    private static final String BASE_QUERY = """
            SELECT
                i.id,
                i.asset_id,
                i.game_version_id,
                i.name,
                i.description,
                i.level,

                it.id AS item_type_id,
                it.name AS type_name,
                it.asset_id AS type_asset_id,

                EXISTS (
                    SELECT 1
                    FROM tools_dofus.recipe r
                    WHERE r.item_id = i.id
                ) AS has_recipe

            FROM tools_dofus.item i
            LEFT JOIN tools_dofus.item_type it ON it.id = i.item_type_id

            WHERE i.game_version_id = :gameVersionId
            AND (
                CAST(:qLike AS TEXT) IS NULL
                OR i.name ILIKE :qLike
                OR CAST(i.id AS TEXT) = :qExact
                OR CAST(i.asset_id AS TEXT) = :qExact
            )
            """;

    @Override
    public List<ItemDto> search(
            CatalogueSearchQuery query,
            Long userId,
            Long gameVersionId) {
        int page = query.getPage() == null || query.getPage() < 1 ? 1 : query.getPage();
        int pageSize = query.getPageSize() == null || query.getPageSize() < 1 ? 20 : query.getPageSize();
        int offset = (page - 1) * pageSize;

        boolean hasSort = query.getSort() != null && SORT_COLUMNS.containsKey(query.getSort());

        String orderBy = hasSort
                ? " ORDER BY " + SORT_COLUMNS.get(query.getSort()) + " " +
                        (query.getDir() == CatalogueSearchQuery.Direction.DESC ? "DESC" : "ASC") +
                        ", i.id ASC"
                : " ORDER BY i.id ASC";

        String sql = BASE_QUERY + orderBy + " LIMIT :limit OFFSET :offset";

        String q = query.getQ();
        String qLike = q == null || q.isBlank() ? null : "%" + q + "%";
        String qExact = q == null || q.isBlank() ? null : q;

        MapSqlParameterSource params = new MapSqlParameterSource()
                .addValue("qLike", qLike)
                .addValue("qExact", qExact)
                .addValue("limit", pageSize)
                .addValue("offset", offset)
                .addValue("gameVersionId", gameVersionId);

        return jdbcTemplate.query(sql, params, CATALOGUE_ITEM_DTO_ROW_MAPPER);
    }

    @Override
    public Long count(
            CatalogueSearchQuery query,
            Long userId,
            Long gameVersionId) {
        String sql = "SELECT COUNT(*) FROM (" + BASE_QUERY + ") sub";

        String q = query.getQ();
        String qLike = q == null || q.isBlank() ? null : "%" + q + "%";
        String qExact = q == null || q.isBlank() ? null : q;

        MapSqlParameterSource params = new MapSqlParameterSource()
                .addValue("qLike", qLike)
                .addValue("qExact", qExact)
                .addValue("gameVersionId", gameVersionId);

        return jdbcTemplate.queryForObject(sql, params, Long.class);
    }

    @Override
    public List<ItemDto> findRecipeByItemId(Long itemId) {
        String sql = """
                    SELECT
                        r.item_id AS parent_item_id,
                        i.id,
                        i.asset_id,
                        i.game_version_id,
                        i.name,
                        i.description,
                        i.level,

                        it.id AS item_type_id,
                        it.name AS type_name,
                        it.asset_id AS type_asset_id,

                        EXISTS (
                            SELECT 1
                            FROM tools_dofus.recipe rec
                            WHERE rec.item_id = i.id
                        ) AS has_recipe,

                        r.quantity

                    FROM tools_dofus.item i
                    LEFT JOIN tools_dofus.item_type it ON it.id = i.item_type_id
                    INNER JOIN tools_dofus.recipe r ON r.ingredient_id = i.id
                    WHERE r.item_id = :itemId
                """;

        MapSqlParameterSource params = new MapSqlParameterSource()
                .addValue("itemId", itemId);

        return jdbcTemplate.query(sql, params, CATALOGUE_INGREDIENT_ROW_MAPPER);
    }
}