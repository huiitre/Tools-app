package fr.huiitre.tools.modules.dofus.workshop.infrastructure;

import java.sql.PreparedStatement;
import java.sql.SQLException;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;

import org.springframework.jdbc.core.BatchPreparedStatementSetter;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRepository;
import fr.huiitre.tools.modules.dofus.workshop.domain.LinkSource;
import fr.huiitre.tools.modules.dofus.workshop.domain.Workshop;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopItem;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopItemIngredient;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopLink;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopTag;

public class PostgresWorkshopRepository implements WorkshopRepository {
    
    private final JdbcTemplate jdbcTemplate;

    public PostgresWorkshopRepository(
        JdbcTemplate jdbcTemplate
    ) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public boolean existsByUserIdAndName(Long userId, String name) {
        String sql = """
            SELECT EXISTS (
                SELECT 1
                FROM tools_dofus.workshop
                WHERE user_id = ? AND name = ?
            )
        """;

        return Boolean.TRUE.equals(
            jdbcTemplate.queryForObject(
                sql,
                Boolean.class,
                userId,
                name
            )
        );
    }

    @Override
    public boolean existsByIdAndUserId(Long userId, Long workshopId) {
        String sql = """
            SELECT EXISTS (
                SELECT 1
                FROM tools_dofus.workshop
                WHERE id = ? AND user_id = ?
            )
        """;

        return Boolean.TRUE.equals(
            jdbcTemplate.queryForObject(
                sql,
                Boolean.class,
                workshopId,
                userId
            )
        );
    }

    @Override
    public Long create(Long gameVersionId, Long userId, Workshop workshop) {
        String sql = """
            INSERT INTO tools_dofus.workshop (game_version_id, user_id, name, is_active, is_pinned)
            VALUES (?, ?, ?, ?, ?)
            RETURNING id
        """;

        return jdbcTemplate.queryForObject(
            sql,
            Long.class,
            gameVersionId,
            userId,
            workshop.getName(),
            workshop.isActive(),
            workshop.isPinned()
        );
    }

    public void update(Long userId, Workshop workshop) {
        String sql = """
            UPDATE tools_dofus.workshop
            SET name = ?, is_active = ?, is_pinned = ?
            WHERE id = ? AND user_id = ?
        """;

        jdbcTemplate.update(
            sql,
            workshop.getName(),
            workshop.isActive(),
            workshop.isPinned(),
            workshop.getId(),
            userId
        );
    }

    @Override
    public void delete(Long userId, Long workshopId) {
        String sql = """
            DELETE FROM tools_dofus.workshop
            WHERE id = ? AND user_id = ?
        """;

        jdbcTemplate.update(sql, workshopId, userId);
    }

    @Override
    public Optional<Workshop> findByIdAndUserId(Long userId, Long workshopId) {
        String sql = """
            SELECT id, name, is_active, is_pinned
            FROM tools_dofus.workshop
            WHERE id = ? AND user_id = ?
        """;

        return jdbcTemplate.query(sql, WORKSHOP_ROW_MAPPER, workshopId, userId)
            .stream()
            .findFirst()
            .map(workshop -> Workshop.rehydrate(
                workshop.getId(),
                workshop.getName(),
                workshop.isActive(),
                workshop.isPinned(),
                findAllLinksByUserIdAndWorkshopId(userId, workshopId)
            ));
    }

    @Override
    public List<Workshop> findAllByUserIdAndGameVersionId(Long userId, Long gameVersionId) {
        String sql = """
            SELECT id, name, is_active, is_pinned
            FROM tools_dofus.workshop
            WHERE user_id = ? AND game_version_id = ?
            ORDER BY is_pinned DESC, name ASC
        """;

        return jdbcTemplate.query(sql, WORKSHOP_ROW_MAPPER, userId, gameVersionId);
    }

    @Override
    public List<WorkshopTag> findAllTagsByUserIdAndWorkshopId(Long userId, Long workshopId) {
        String sql = """
            select wt.id, wt.name, wt.color
            from tools_dofus.workshop_tag wt
            join tools_dofus.workshop_has_tag wht on wht.tag_id = wt.id
            where wht.workshop_id = ? and wt.user_id = ?
        """;

        return jdbcTemplate.query(sql, WORKSHOP_TAG_ROW_MAPPER, workshopId, userId);
    }

    @Override
    public List<WorkshopItem> findAllItemsByUserIdAndWorkshopId(Long userId, Long workshopId) {
        String sql = """
            SELECT wi.id, wi.item_id, wi.quantity
            FROM tools_dofus.workshop_item wi
            join tools_dofus.workshop w on w.id = wi.workshop_id
            WHERE wi.workshop_id = ? AND w.user_id = ?
        """;

        return jdbcTemplate.query(sql, WORKSHOP_ITEM_ROW_MAPPER, workshopId, userId);
    }

    @Override
    public Long addItemToWorkshop(Long userId, Long workshopId, WorkshopItem workshopItem) {
        String sql = """
            INSERT INTO tools_dofus.workshop_item (workshop_id, item_id, quantity)
            SELECT ?, ?, ?
            FROM tools_dofus.workshop w
            WHERE w.id = ? AND w.user_id = ?
            RETURNING id
        """;

        return jdbcTemplate.queryForObject(
            sql,
            Long.class,
            workshopId,
            workshopItem.getItemId(),
            workshopItem.getQuantity(),
            workshopId,
            userId
        );
    }

    @Override
    public List<WorkshopItemIngredient> findAllIngredientsByUserIdAndWorkshopItemId(Long userId, Long workshopItemId) {
        String sql = """
            SELECT wii.id, wii.workshop_item_id, wii.item_id, wii.parent_ingredient_id, wii.quantity_obtained
            FROM tools_dofus.workshop_item_ingredient wii
            JOIN tools_dofus.workshop_item wi ON wi.id = wii.workshop_item_id
            JOIN tools_dofus.workshop w ON w.id = wi.workshop_id
            WHERE wii.workshop_item_id = ? AND w.user_id = ?
        """;

        return jdbcTemplate.query(sql, WORKSHOP_ITEM_INGREDIENT_ROW_MAPPER, workshopItemId, userId);
    }

    @Override
    public void addIngredients(Long userId, List<WorkshopItemIngredient> ingredients) {
        if (ingredients == null || ingredients.isEmpty()) {
            return;
        }

        String sql = """
            INSERT INTO tools_dofus.workshop_item_ingredient (workshop_item_id, item_id, parent_ingredient_id, quantity_obtained)
            VALUES (?, ?, ?, ?)
        """;

        jdbcTemplate.batchUpdate(sql, new BatchPreparedStatementSetter() {
            @Override
            public void setValues(PreparedStatement ps, int i) throws SQLException {
                WorkshopItemIngredient ingredient = ingredients.get(i);
                ps.setLong(1, ingredient.getWorkshopItemId());
                ps.setLong(2, ingredient.getItemId());
                
                if (ingredient.getParentIngredientId() != null) {
                    ps.setLong(3, ingredient.getParentIngredientId());
                } else {
                    ps.setNull(3, java.sql.Types.BIGINT);
                }
                
                ps.setLong(4, ingredient.getQuantityObtained());
            }

            @Override
            public int getBatchSize() {
                return ingredients.size();
            }
        });
    }

    @Override
    public void deleteWorkshopItem(Long userId, Long workshopId, Long workshopItemId) {
        String sql = """
            DELETE FROM tools_dofus.workshop_item wi
            USING tools_dofus.workshop w
            WHERE wi.id = ?
            AND wi.workshop_id = ?
            AND w.id = wi.workshop_id
            AND w.user_id = ?
        """;

        jdbcTemplate.update(sql, workshopItemId, workshopId, userId);
    }

    @Override
    public void updateWorkshopItemQuantity(Long userId, Long workshopId, Long workshopItemId, Long quantity) {
        String sql = """
            UPDATE tools_dofus.workshop_item wi
            SET quantity = ?
            FROM tools_dofus.workshop w
            WHERE wi.id = ?
            AND wi.workshop_id = ?
            AND w.id = wi.workshop_id
            AND w.user_id = ?
        """;

        jdbcTemplate.update(sql, quantity, workshopItemId, workshopId, userId);
    }

    @Override
    public Optional<WorkshopItemIngredient> findIngredientByIdAndUserId(Long userId, Long ingredientId) {
        String sql = """
            SELECT wii.id, wii.workshop_item_id, wii.item_id, wii.parent_ingredient_id, wii.quantity_obtained
            FROM tools_dofus.workshop_item_ingredient wii
            JOIN tools_dofus.workshop_item wi ON wi.id = wii.workshop_item_id
            JOIN tools_dofus.workshop w ON w.id = wi.workshop_id
            WHERE wii.id = ? AND w.user_id = ?
        """;

        return jdbcTemplate.query(sql, WORKSHOP_ITEM_INGREDIENT_ROW_MAPPER, ingredientId, userId)
            .stream()
            .findFirst();
    }

    @Override
    public List<WorkshopItemIngredient> findIngredientsByParentIngredientId(Long userId, Long parentIngredientId) {
        String sql = """
            SELECT wii.id, wii.workshop_item_id, wii.item_id, wii.parent_ingredient_id, wii.quantity_obtained
            FROM tools_dofus.workshop_item_ingredient wii
            JOIN tools_dofus.workshop_item wi ON wi.id = wii.workshop_item_id
            JOIN tools_dofus.workshop w ON w.id = wi.workshop_id
            WHERE wii.parent_ingredient_id = ? AND w.user_id = ?
        """;

        return jdbcTemplate.query(sql, WORKSHOP_ITEM_INGREDIENT_ROW_MAPPER, parentIngredientId, userId);
    }

    @Override
    public void updateIngredientQuantityObtained(Long userId, Long ingredientId, Long quantityObtained) {
        String sql = """
            UPDATE tools_dofus.workshop_item_ingredient wii
            SET quantity_obtained = ?
            FROM tools_dofus.workshop_item wi, tools_dofus.workshop w
            WHERE wii.id = ?
            AND wii.workshop_item_id = wi.id
            AND wi.workshop_id = w.id
            AND w.user_id = ?
        """;

        jdbcTemplate.update(sql, quantityObtained, ingredientId, userId);
    }

    @Override
    public void deleteIngredientsByParentId(Long userId, Long parentIngredientId) {
        String sql = """
            DELETE FROM tools_dofus.workshop_item_ingredient wii
            USING tools_dofus.workshop_item wi, tools_dofus.workshop w
            WHERE wii.parent_ingredient_id = ?
            AND wii.workshop_item_id = wi.id
            AND wi.workshop_id = w.id
            AND w.user_id = ?
        """;

        jdbcTemplate.update(sql, parentIngredientId, userId);
    }

    @Override
    public void addTagsToWorkshop(Long userId, Long workshopId, List<Long> tagIds) {
        if (tagIds == null || tagIds.isEmpty()) {
            return;
        }

        String sql = """
            INSERT INTO tools_dofus.workshop_has_tag (workshop_id, tag_id)
            SELECT ?, ?
            FROM tools_dofus.workshop w
            WHERE w.id = ? AND w.user_id = ?
            ON CONFLICT DO NOTHING
        """;

        jdbcTemplate.batchUpdate(sql, new BatchPreparedStatementSetter() {
            @Override
            public void setValues(PreparedStatement ps, int i) throws SQLException {
                ps.setLong(1, workshopId);
                ps.setLong(2, tagIds.get(i));
                ps.setLong(3, workshopId);
                ps.setLong(4, userId);
            }

            @Override
            public int getBatchSize() {
                return tagIds.size();
            }
        });
    }

    @Override
    public void removeTagFromWorkshop(Long userId, Long workshopId, Long tagId) {
        String sql = """
            DELETE FROM tools_dofus.workshop_has_tag wht
            USING tools_dofus.workshop w
            WHERE wht.workshop_id = ?
            AND wht.tag_id = ?
            AND w.id = wht.workshop_id
            AND w.user_id = ?
        """;

        jdbcTemplate.update(sql, workshopId, tagId, userId);
    }

    @Override
    public Long addLink(Long userId, Long workshopId, WorkshopLink link) {
        String sql = """
            INSERT INTO tools_dofus.workshop_link (workshop_id, source, url, label, created_at)
            SELECT ?, ?, ?, ?, ?
            FROM tools_dofus.workshop w
            WHERE w.id = ? AND w.user_id = ?
            RETURNING id
        """;

        return jdbcTemplate.queryForObject(
            sql,
            Long.class,
            workshopId,
            link.getSource().name(),
            link.getUrl(),
            link.getLabel(),
            link.getCreatedAt(),
            workshopId,
            userId
        );
    }

    @Override
    public List<WorkshopLink> findAllLinksByUserIdAndWorkshopId(Long userId, Long workshopId) {
        String sql = """
            SELECT wl.id, wl.source, wl.url, wl.label, wl.created_at
            FROM tools_dofus.workshop_link wl
            JOIN tools_dofus.workshop w ON w.id = wl.workshop_id
            WHERE wl.workshop_id = ? AND w.user_id = ?
            ORDER BY wl.created_at ASC
        """;

        return jdbcTemplate.query(sql, WORKSHOP_LINK_ROW_MAPPER, workshopId, userId);
    }

    @Override
    public Map<Long, List<WorkshopLink>> findAllLinksByUserIdGroupedByWorkshopId(Long userId) {
        String sql = """
            SELECT wl.id, wl.source, wl.url, wl.label, wl.created_at, wl.workshop_id
            FROM tools_dofus.workshop_link wl
            JOIN tools_dofus.workshop w ON w.id = wl.workshop_id
            WHERE w.user_id = ?
            ORDER BY wl.created_at ASC
        """;

        Map<Long, List<WorkshopLink>> result = new HashMap<>();
        jdbcTemplate.query(sql, rs -> {
            Long workshopId = rs.getLong("workshop_id");
            WorkshopLink link = WorkshopLink.rehydrate(
                rs.getLong("id"),
                LinkSource.valueOf(rs.getString("source")),
                rs.getString("url"),
                rs.getString("label"),
                rs.getTimestamp("created_at").toLocalDateTime()
            );
            result.computeIfAbsent(workshopId, k -> new ArrayList<>()).add(link);
        }, userId);
        return result;
    }

    @Override
    public int deleteLink(Long userId, Long workshopId, Long linkId) {
        String sql = """
            DELETE FROM tools_dofus.workshop_link
            WHERE id = ? AND workshop_id = ? AND workshop_id IN (
                SELECT id FROM tools_dofus.workshop WHERE id = ? AND user_id = ?
            )
        """;

        return jdbcTemplate.update(sql, linkId, workshopId, workshopId, userId);
    }

    @Override
    public Optional<WorkshopLink> findLinkByIdAndWorkshopId(Long userId, Long workshopId, Long linkId) {
        String sql = """
            SELECT wl.id, wl.source, wl.url, wl.label, wl.created_at
            FROM tools_dofus.workshop_link wl
            JOIN tools_dofus.workshop w ON w.id = wl.workshop_id
            WHERE wl.id = ? AND wl.workshop_id = ? AND w.user_id = ?
        """;

        return jdbcTemplate.query(sql, WORKSHOP_LINK_ROW_MAPPER, linkId, workshopId, userId)
            .stream()
            .findFirst();
    }

    @Override
    public void updateLink(Long userId, Long workshopId, Long linkId, String url, String label) {
        String sql = """
            UPDATE tools_dofus.workshop_link SET url = ?, label = ?
            WHERE id = ? AND workshop_id = ? AND workshop_id IN (
                SELECT id FROM tools_dofus.workshop WHERE id = ? AND user_id = ?
            )
        """;

        jdbcTemplate.update(sql, url, label, linkId, workshopId, workshopId, userId);
    }

    @Override
    public Long findOwnerUserId(Long workshopId) {
        String sql = """
            SELECT user_id
            FROM tools_dofus.workshop
            WHERE id = ?
        """;

        return jdbcTemplate.queryForObject(sql, Long.class, workshopId);
    }

    @Override
    public Optional<Workshop> findById(Long id, Long gameVersionId) {
        String sql = """
            SELECT id, name, is_active, is_pinned, user_id
            FROM tools_dofus.workshop
            WHERE id = ? AND game_version_id = ?
        """;

        return jdbcTemplate.query(sql, (rs, rowNum) -> {
            Long ownerId = rs.getLong("user_id");
            return Workshop.rehydrate(
                rs.getLong("id"),
                rs.getString("name"),
                rs.getBoolean("is_active"),
                rs.getBoolean("is_pinned"),
                findAllLinksByUserIdAndWorkshopId(ownerId, id)
            );
        }, id, gameVersionId).stream().findFirst();
    }

    private static final RowMapper<WorkshopItemIngredient> WORKSHOP_ITEM_INGREDIENT_ROW_MAPPER = (rs, rowNum) -> WorkshopItemIngredient.rehydrate(
        rs.getLong("id"),
        rs.getLong("workshop_item_id"),
        rs.getLong("item_id"),
        rs.getObject("parent_ingredient_id", Long.class),
        rs.getLong("quantity_obtained")
    );

    private static final RowMapper<Workshop> WORKSHOP_ROW_MAPPER = (rs, rowNum) -> Workshop.rehydrate(
        rs.getLong("id"),
        rs.getString("name"),
        rs.getBoolean("is_active"),
        rs.getBoolean("is_pinned"),
        null
    );
    
    private static final RowMapper<WorkshopTag> WORKSHOP_TAG_ROW_MAPPER = (rs, rowNum) -> WorkshopTag.rehydrate(
        rs.getLong("id"),
        rs.getString("name"),
        rs.getString("color")
    );
    
    private static final RowMapper<WorkshopItem> WORKSHOP_ITEM_ROW_MAPPER = (rs, rowNum) -> WorkshopItem.rehydrate(
        rs.getLong("id"),
        rs.getLong("item_id"),
        rs.getLong("quantity")
    );

    private static final RowMapper<WorkshopLink> WORKSHOP_LINK_ROW_MAPPER = (rs, rowNum) -> WorkshopLink.rehydrate(
        rs.getLong("id"),
        LinkSource.valueOf(rs.getString("source")),
        rs.getString("url"),
        rs.getString("label"),
        rs.getTimestamp("created_at").toLocalDateTime()
    );
}