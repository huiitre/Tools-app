package fr.huiitre.tools.modules.dofus.itemtype.infrastructure;

import java.util.List;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.dofus.itemtype.application.ports.ItemTypeRepository;
import fr.huiitre.tools.modules.dofus.itemtype.domain.ItemType;

public class PostgresItemTypeRepository implements ItemTypeRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresItemTypeRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<ItemType> ITEM_TYPE_ROW_MAPPER = (rs, rowNum) -> ItemType.rehydrate(
            rs.getLong("id"),
            rs.getLong("asset_id"),
            rs.getLong("game_version_id"),
            rs.getLong("category_id"),
            rs.getString("name"));

    @Override
    public List<ItemType> findAllByGameVersionId(Long gameVersionId) {
        final String sql = """
                    SELECT
                        id,
                        asset_id,
                        game_version_id,
                        category_id,
                        name
                    FROM
                        tools_dofus.item_type
                    WHERE
                        game_version_id = ?
                """;

        return jdbcTemplate.query(
                sql,
                ITEM_TYPE_ROW_MAPPER,
                gameVersionId);
    }

    @Override
    public void save(ItemType itemType) {
        final String sql = """
                    INSERT INTO tools_dofus.item_type (
                        asset_id,
                        game_version_id,
                        category_id,
                        name
                    ) VALUES (?, ?, ?, ?)
                """;

        jdbcTemplate.update(
                sql,
                itemType.getAssetId(),
                itemType.getGameVersionId(),
                itemType.getCategoryId(),
                itemType.getName());
    }

    @Override
    public void update(ItemType itemType) {
        final String sql = """
                    UPDATE tools_dofus.item_type
                    SET
                        asset_id = ?,
                        game_version_id = ?,
                        category_id = ?,
                        name = ?
                    WHERE id = ? AND game_version_id = ?
                """;

        jdbcTemplate.update(
                sql,
                itemType.getAssetId(),
                itemType.getGameVersionId(),
                itemType.getCategoryId(),
                itemType.getName(),
                itemType.getId(),
                itemType.getGameVersionId());
    }
}
