package fr.huiitre.tools.modules.todolist.application.usecase;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.todolist.application.ports.TodolistRepository;
import fr.huiitre.tools.modules.todolist.domain.Todolist;
import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;

@Service
@Transactional
public class ListUserTodolistsUseCase implements SecuredUseCase {

    private final TodolistRepository todolistRepository;

    private final AuthenticatedUserProvider authenticatedUserProvider;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.TODOLIST);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public ListUserTodolistsUseCase(
            TodolistRepository todolistRepository,
            AuthenticatedUserProvider authenticatedUserProvider) {
        this.todolistRepository = todolistRepository;
        this.authenticatedUserProvider = authenticatedUserProvider;
    }

    public List<Todolist> execute() {
        Long userId = authenticatedUserProvider.getUserId();
        List<Todolist> todolists = todolistRepository.findAllByUserId(userId);
        return todolists;
    }
}