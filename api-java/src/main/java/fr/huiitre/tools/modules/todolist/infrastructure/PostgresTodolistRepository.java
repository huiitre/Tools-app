package fr.huiitre.tools.modules.todolist.infrastructure;

import java.util.List;
import java.util.Optional;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.todolist.application.ports.TodolistRepository;
import fr.huiitre.tools.modules.todolist.domain.Todolist;

public class PostgresTodolistRepository implements TodolistRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresTodolistRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<Todolist> TODOLIST_ROW_MAPPER = (rs, rowNum) ->
        Todolist.rehydrate(
            rs.getLong("id"),
            rs.getString("name"),
            rs.getBoolean("is_active"),
            rs.getBoolean("is_favorite"),
            rs.getString("color_hex"),
            rs.getLong("display_order")
        );

    @Override
    public void save(Long userId, Todolist todolist) {
        final String sql = """
            INSERT INTO tools_todolist.todolist
                (user_id, name, is_active, is_favorite, color_hex, display_order)
            VALUES (?, ?, ?, ?, ?, ?)
        """;

        jdbcTemplate.update(
            sql,
            userId,
            todolist.getName(),
            todolist.isActive(),
            todolist.isFavorite(),
            todolist.getColorHex(),
            todolist.getDisplayOrder()
        );
    }

    @Override
    public void update(Long userId, Todolist todolist) {
        final String sql = """
            UPDATE tools_todolist.todolist
            SET
                name = ?,
                is_active = ?,
                is_favorite = ?,
                color_hex = ?,
                display_order = ?
            WHERE user_id = ? AND id = ?
        """;

        jdbcTemplate.update(
            sql,
            todolist.getName(),
            todolist.isActive(),
            todolist.isFavorite(),
            todolist.getColorHex(),
            todolist.getDisplayOrder(),
            userId,
            todolist.getId()
        );
    }

    @Override
    public void delete(Long userId, Long todolistId) {
        final String sql = """
            DELETE FROM tools_todolist.todolist
            WHERE user_id = ? AND id = ?
        """;

        int affected = jdbcTemplate.update(sql, userId, todolistId);
        if (affected == 0) {
            throw new IllegalArgumentException("TODOLIST_NOT_FOUND_OR_NOT_OWNED");
        }
    }

    @Override
    public List<Todolist> findAllByUserId(Long userId) {
        final String sql = """
            SELECT id, name, is_active, is_favorite, color_hex, display_order
            FROM tools_todolist.todolist
            WHERE user_id = ?
            ORDER BY display_order ASC
        """;

        return jdbcTemplate.query(sql, TODOLIST_ROW_MAPPER, userId);
    }

    @Override
    public Optional<Todolist> findById(Long userId, Long todolistId) {
        final String sql = """
            SELECT id, name, is_active, is_favorite, color_hex, display_order
            FROM tools_todolist.todolist
            WHERE user_id = ? AND id = ?
        """;

        return jdbcTemplate
            .query(sql, TODOLIST_ROW_MAPPER, userId, todolistId)
            .stream()
            .findFirst();
    }
}
