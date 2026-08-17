package fr.huiitre.tools.modules.elite_dangerous.r2r.application.usecase;

import java.util.Optional;
import java.util.UUID;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.elite_dangerous.r2r.application.ports.R2rExpeditionRepository;

@Service
@Transactional
public class DeleteR2rExpeditionUseCase implements SecuredUseCase {

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.ELITE_DANGEROUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    private final R2rExpeditionRepository repository;
    private final AuthenticatedUserProvider authenticatedUserProvider;

    public DeleteR2rExpeditionUseCase(
            R2rExpeditionRepository repository,
            AuthenticatedUserProvider authenticatedUserProvider) {
        this.repository = repository;
        this.authenticatedUserProvider = authenticatedUserProvider;
    }

    public void execute(UUID id) {
        Long userId = authenticatedUserProvider.getUserId();

        if (!repository.existsByIdAndUserId(id, userId)) {
            throw new IllegalArgumentException("R2R_EXPEDITION_NOT_FOUND");
        }

        repository.delete(id, userId);
    }
}
