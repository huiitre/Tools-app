package fr.huiitre.tools.modules.todolist.application.usecase;

import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.todolist.application.command.CreateTodoCommand;
import fr.huiitre.tools.modules.todolist.application.ports.TodoRepository;
import fr.huiitre.tools.modules.todolist.domain.Todo;
import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;

@Service
@Transactional
public class CreateTodoUseCase implements SecuredUseCase {

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

    public CreateTodoUseCase(
            TodoRepository todoRepository,
            AuthenticatedUserProvider authenticatedUserProvider) {
        this.todoRepository = todoRepository;
        this.authenticatedUserProvider = authenticatedUserProvider;
    }

    public void execute(Long todolistId, CreateTodoCommand command) {
        Long userId = authenticatedUserProvider.getUserId();

        Todo todo = Todo.create(
                command.getName(),
                command.getDescription(),
                todolistId,
                command.getDisplayOrder(),
                command.getPriority());

        todoRepository.save(userId, todo);
    }
}