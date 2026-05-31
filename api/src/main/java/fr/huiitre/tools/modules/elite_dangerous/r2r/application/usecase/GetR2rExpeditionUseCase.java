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
import fr.huiitre.tools.modules.elite_dangerous.r2r.application.view.R2rExpeditionDetailView;

@Service
@Transactional(readOnly = true)
public class GetR2rExpeditionUseCase implements SecuredUseCase {

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

    public GetR2rExpeditionUseCase(
            R2rExpeditionRepository repository,
            AuthenticatedUserProvider authenticatedUserProvider) {
        this.repository = repository;
        this.authenticatedUserProvider = authenticatedUserProvider;
    }

    public R2rExpeditionDetailView execute(UUID id) {
        Long userId = authenticatedUserProvider.getUserId();
        return repository.findByIdAndUserId(id, userId)
                .orElseThrow(() -> new IllegalArgumentException("R2R_EXPEDITION_NOT_FOUND"));
    }
}
