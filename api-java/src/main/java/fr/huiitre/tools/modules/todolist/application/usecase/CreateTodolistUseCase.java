package fr.huiitre.tools.modules.todolist.application.usecase;

import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.todolist.application.command.CreateTodolistCommand;
import fr.huiitre.tools.modules.todolist.application.ports.TodolistRepository;
import fr.huiitre.tools.modules.todolist.domain.Todolist;
import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;

@Service
@Transactional
public class CreateTodolistUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.TODOLIST);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    private final TodolistRepository todolistRepository;

    public CreateTodolistUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            TodolistRepository todolistRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.todolistRepository = todolistRepository;
    }

    public void execute(CreateTodolistCommand command) {
        Long userId = authenticatedUserProvider.getUserId();

        Todolist todolist = Todolist.create(
                command.getName(),
                command.isActive(),
                command.isFavorite(),
                command.getColorHex(),
                command.getDisplayOrder());
        todolistRepository.save(userId, todolist);
    }
}