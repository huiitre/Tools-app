package fr.huiitre.tools.modules.elite_dangerous.r2r.application.usecase;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.elite_dangerous.r2r.application.ports.R2rExpeditionRepository;
import fr.huiitre.tools.modules.elite_dangerous.r2r.application.view.R2rExpeditionSummaryView;

@Service
@Transactional(readOnly = true)
public class GetR2rExpeditionsUseCase implements SecuredUseCase {

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

    public GetR2rExpeditionsUseCase(
            R2rExpeditionRepository repository,
            AuthenticatedUserProvider authenticatedUserProvider) {
        this.repository = repository;
        this.authenticatedUserProvider = authenticatedUserProvider;
    }

    public List<R2rExpeditionSummaryView> execute() {
        Long userId = authenticatedUserProvider.getUserId();
        return repository.findAllByUserId(userId);
    }
}
