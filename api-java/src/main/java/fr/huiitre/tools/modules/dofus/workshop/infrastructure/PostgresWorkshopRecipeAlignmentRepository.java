package fr.huiitre.tools.modules.dofus.workshop.infrastructure;

import java.sql.PreparedStatement;
import java.sql.SQLException;
import java.sql.Types;
import java.util.Collection;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import org.springframework.jdbc.core.BatchPreparedStatementSetter;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRecipeAlignmentRepository;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopItemIngredient;

public class PostgresWorkshopRecipeAlignmentRepository implements WorkshopRecipeAlignmentRepository {

    private static final RowMapper<WorkshopItemRef> WORKSHOP_ITEM_REF_ROW_MAPPER = (rs, i) -> new WorkshopItemRef(
            rs.getLong("workshop_item_id"),
            rs.getLong("workshop_id"),
            rs.getString("workshop_name"),
            rs.getLong("item_id"));

    private static final RowMapper<WorkshopItemIngredient> INGREDIENT_ROW_MAPPER = (rs, i) -> WorkshopItemIngredient.rehydrate(
            rs.getLong("id"),
            rs.getLong("workshop_item_id"),
            rs.getLong("item_id"),
            (Long) rs.getObject("parent_ingredient_id"),
            rs.getLong("quantity_obtained"));

    private final JdbcTemplate jdbcTemplate;

    public PostgresWorkshopRecipeAlignmentRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public List<WorkshopItemRef> findAllWorkshopItems(Long gameVersionId) {
        String sql = """
            SELECT wi.id AS workshop_item_id, w.id AS workshop_id, w.name AS workshop_name, wi.item_id
            FROM tools_dofus.workshop_item wi
            JOIN tools_dofus.workshop w ON w.id = wi.workshop_id
            WHERE w.game_version_id = ?
            ORDER BY w.id, wi.id
        """;
        return jdbcTemplate.query(sql, WORKSHOP_ITEM_REF_ROW_MAPPER, gameVersionId);
    }

    @Override
    public List<WorkshopItemIngredient> findAllIngredients(Long gameVersionId) {
        String sql = """
            SELECT wii.id, wii.workshop_item_id, wii.item_id, wii.parent_ingredient_id, wii.quantity_obtained
            FROM tools_dofus.workshop_item_ingredient wii
            JOIN tools_dofus.workshop_item wi ON wi.id = wii.workshop_item_id
            JOIN tools_dofus.workshop w ON w.id = wi.workshop_id
            WHERE w.game_version_id = ?
            ORDER BY wii.id
        """;
        return jdbcTemplate.query(sql, INGREDIENT_ROW_MAPPER, gameVersionId);
    }

    @Override
    public Map<Long, String> findItemNames(Collection<Long> itemIds) {
        Map<Long, String> names = new HashMap<>();
        if (itemIds == null || itemIds.isEmpty()) {
            return names;
        }
        String sql = "SELECT id, name FROM tools_dofus.item WHERE id = ANY(?)";
        jdbcTemplate.query(
                con -> {
                    PreparedStatement ps = con.prepareStatement(sql);
                    ps.setArray(1, con.createArrayOf("bigint", itemIds.toArray()));
                    return ps;
                },
                rs -> {
                    names.put(rs.getLong("id"), rs.getString("name"));
                });
        return names;
    }

    // Les sous-ingrédients d'un ingrédient supprimé partent avec lui (ON DELETE CASCADE).
    @Override
    public void deleteIngredients(List<Long> ingredientIds) {
        if (ingredientIds == null || ingredientIds.isEmpty()) {
            return;
        }
        jdbcTemplate.batchUpdate(
                "DELETE FROM tools_dofus.workshop_item_ingredient WHERE id = ?",
                ingredientIds,
                ingredientIds.size(),
                (ps, id) -> ps.setLong(1, id));
    }

    @Override
    public void insertIngredients(List<WorkshopItemIngredient> ingredients) {
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
                    ps.setNull(3, Types.BIGINT);
                }
                ps.setLong(4, ingredient.getQuantityObtained());
            }

            @Override
            public int getBatchSize() {
                return ingredients.size();
            }
        });
    }
}
