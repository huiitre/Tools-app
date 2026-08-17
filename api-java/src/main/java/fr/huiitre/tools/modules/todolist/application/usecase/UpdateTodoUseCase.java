package fr.huiitre.tools.modules.todolist.application.usecase;

import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.todolist.application.command.UpdateTodoCommand;
import fr.huiitre.tools.modules.todolist.application.ports.TodoRepository;
import fr.huiitre.tools.modules.todolist.domain.Todo;
import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;

@Service
@Transactional
public class UpdateTodoUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;

    private final TodoRepository todoRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.TODOLIST);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public UpdateTodoUseCase(
            TodoRepository todoRepository,
            AuthenticatedUserProvider authenticatedUserProvider) {
        this.todoRepository = todoRepository;
        this.authenticatedUserProvider = authenticatedUserProvider;
    }

    public void execute(Long todolistId, Long todoId, UpdateTodoCommand command) {
        Long userId = authenticatedUserProvider.getUserId();

        Todo todo = todoRepository.findById(userId, todoId)
                .orElseThrow(() -> new IllegalArgumentException("Todo not found"));

        if (!todo.getTodolistId().equals(todolistId)) {
            throw new IllegalArgumentException("TODO_NOT_IN_TODOLIST");
        }

        todo.update(
                command.getName(),
                command.getDescription(),
                command.isCompleted(),
                command.getDisplayOrder(),
                command.getPriority());

        todoRepository.update(userId, todo);
    }
}