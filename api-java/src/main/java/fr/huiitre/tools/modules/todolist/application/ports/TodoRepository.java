package fr.huiitre.tools.modules.todolist.application.ports;

import java.util.List;
import java.util.Optional;

import fr.huiitre.tools.modules.todolist.domain.Todo;

public interface TodoRepository {

    void save(Long userId, Todo todo);

    void update(Long userId, Todo todo);

    void delete(Long userId, Long todoId);

    List<Todo> findAllByUserIdAndTodolistId(Long userId, Long todolistId);

    Optional<Todo> findById(Long userId, Long todoId);

    void deleteByTodolistId(Long userId, Long todolistId);
}
