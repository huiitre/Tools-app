package fr.huiitre.tools.modules.todolist.infrastructure;

import java.util.List;
import java.util.Optional;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.todolist.application.ports.TodoRepository;
import fr.huiitre.tools.modules.todolist.domain.Todo;
import fr.huiitre.tools.modules.todolist.domain.TodoPriority;

public class PostgresTodoRepository implements TodoRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresTodoRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<Todo> TODO_ROW_MAPPER = (rs, rowNum) ->
        Todo.rehydrate(
            rs.getLong("id"),
            rs.getString("name"),
            rs.getString("description"),
            rs.getBoolean("is_completed"),
            rs.getLong("todolist_id"),
            rs.getLong("display_order"),
            TodoPriority.valueOf(rs.getString("priority"))
        );

    @Override
    public void save(Long userId, Todo todo) {
        final String sql = """
            INSERT INTO tools_todolist.todo (todolist_id, name, description, display_order, priority)
            SELECT id, ?, ?, ?, ?
            FROM tools_todolist.todolist
            WHERE user_id = ? AND id = ?
        """;

        int affected = jdbcTemplate.update(
            sql,
            todo.getName(),
            todo.getDescription(),
            todo.getDisplayOrder(),
            todo.getPriority().name(),
            userId,
            todo.getTodolistId()
        );

        if (affected == 0) {
            throw new IllegalArgumentException("TODOLIST_NOT_FOUND_OR_NOT_OWNED");
        }
    }

    @Override
    public void update(Long userId, Todo todo) {
        final String sql = """
            UPDATE tools_todolist.todo t
            SET name = ?, description = ?, is_completed = ?, display_order = ?, priority = ?
            WHERE t.id = ?
              AND t.todolist_id = ?
              AND EXISTS (
                  SELECT 1
                  FROM tools_todolist.todolist tl
                  WHERE tl.id = t.todolist_id
                    AND tl.user_id = ?
              )
        """;

        int affected = jdbcTemplate.update(
            sql,
            todo.getName(),
            todo.getDescription(),
            todo.isCompleted(),
            todo.getDisplayOrder(),
            todo.getPriority().name(),
            todo.getId(),
            todo.getTodolistId(),
            userId
        );

        if (affected == 0) {
            throw new IllegalArgumentException("TODO_NOT_FOUND_OR_NOT_OWNED");
        }
    }

    @Override
    public void delete(Long userId, Long todoId) {
        final String sql = """
            DELETE FROM tools_todolist.todo
            WHERE id = ?
              AND todolist_id IN (
                  SELECT id
                  FROM tools_todolist.todolist
                  WHERE user_id = ?
              )
        """;

        int affected = jdbcTemplate.update(sql, todoId, userId);

        if (affected == 0) {
            throw new IllegalArgumentException("TODO_NOT_FOUND_OR_NOT_OWNED");
        }
    }

    @Override
    public List<Todo> findAllByUserIdAndTodolistId(Long userId, Long todolistId) {
        final String sql = """
            SELECT t.id, t.todolist_id, t.name, t.description, t.is_completed, t.display_order, t.priority
            FROM tools_todolist.todo t
            JOIN tools_todolist.todolist l ON t.todolist_id = l.id
            WHERE l.user_id = ? AND l.id = ?
            ORDER BY t.display_order ASC
        """;

        return jdbcTemplate.query(sql, TODO_ROW_MAPPER, userId, todolistId);
    }

    @Override
    public Optional<Todo> findById(Long userId, Long todoId) {
        final String sql = """
            SELECT t.id, t.todolist_id, t.name, t.description, t.is_completed, t.display_order, t.priority
            FROM tools_todolist.todo t
            JOIN tools_todolist.todolist l ON t.todolist_id = l.id
            WHERE t.id = ? AND l.user_id = ?
        """;

        List<Todo> results = jdbcTemplate.query(sql, TODO_ROW_MAPPER, todoId, userId);
        return results.stream().findFirst();
    }

    @Override
    public void deleteByTodolistId(Long userId, Long todolistId) {
        final String sql = """
            DELETE FROM tools_todolist.todo
            WHERE todolist_id = ?
              AND todolist_id IN (
                  SELECT id
                  FROM tools_todolist.todolist
                  WHERE user_id = ?
              )
        """;

        jdbcTemplate.update(sql, todolistId, userId);
    }
}
