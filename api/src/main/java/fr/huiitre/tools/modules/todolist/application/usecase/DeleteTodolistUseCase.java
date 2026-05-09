package fr.huiitre.tools.modules.todolist.application.usecase;
import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.todolist.application.ports.TodoRepository;
import fr.huiitre.tools.modules.todolist.application.ports.TodolistRepository;
import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;

@Service
@Transactional
public class DeleteTodolistUseCase implements SecuredUseCase {

    private final TodolistRepository todolistRepository;

    private final TodoRepository todoRepository;

    private final AuthenticatedUserProvider authenticatedUserProvider;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.TODOLIST);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public DeleteTodolistUseCase(
            TodolistRepository todolistRepository,
            TodoRepository todoRepository,
            AuthenticatedUserProvider authenticatedUserProvider) {
        this.todolistRepository = todolistRepository;
        this.todoRepository = todoRepository;
        this.authenticatedUserProvider = authenticatedUserProvider;
    }

    public void execute(Long id) {
        Long userId = authenticatedUserProvider.getUserId();

        todoRepository.deleteByTodolistId(userId, id);
        todolistRepository.delete(userId, id);
    }
}