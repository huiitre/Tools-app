package fr.huiitre.tools.modules.todolist.application.usecase;

import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.todolist.application.command.UpdateTodolistCommand;
import fr.huiitre.tools.modules.todolist.application.ports.TodolistRepository;
import fr.huiitre.tools.modules.todolist.domain.Todolist;
import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;

@Service
@Transactional
public class UpdateTodolistUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;

    private final TodolistRepository todolistRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.TODOLIST);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public UpdateTodolistUseCase(
            TodolistRepository todolistRepository,
            AuthenticatedUserProvider authenticatedUserProvider) {
        this.todolistRepository = todolistRepository;
        this.authenticatedUserProvider = authenticatedUserProvider;
    }

    public void execute(Long id, UpdateTodolistCommand command) {
        Long userId = authenticatedUserProvider.getUserId();

        Todolist todolist = todolistRepository.findById(userId, id)
                .orElseThrow(() -> new IllegalArgumentException("TODOLIST_NOT_FOUND"));

        todolist.update(
                command.getName(),
                command.isActive(),
                command.isFavorite(),
                command.getColorHex(),
                command.getDisplayOrder());

        todolistRepository.update(userId, todolist);
    }
}