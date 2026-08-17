package fr.huiitre.tools.modules.todolist.application.usecase;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.todolist.application.ports.TodoRepository;
import fr.huiitre.tools.modules.todolist.domain.Todo;
import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;

@Service
@Transactional
public class ListUserTodoUseCase implements SecuredUseCase {

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

    public ListUserTodoUseCase(
            TodoRepository todoRepository,
            AuthenticatedUserProvider authenticatedUserProvider) {
        this.todoRepository = todoRepository;
        this.authenticatedUserProvider = authenticatedUserProvider;
    }

    public List<Todo> execute(Long todolistId) {
        Long userId = authenticatedUserProvider.getUserId();
        return todoRepository.findAllByUserIdAndTodolistId(userId, todolistId);
    }
}