package fr.huiitre.tools.modules.dofus.almanax.application.usecase;

import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.almanax.application.ports.AlmanaxSubscriptionRepository;

@Service
@Transactional
public class RemoveAlmanaxSubscriptionUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final AlmanaxSubscriptionRepository subscriptionRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public RemoveAlmanaxSubscriptionUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            AlmanaxSubscriptionRepository subscriptionRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.subscriptionRepository = subscriptionRepository;
    }

    public void execute(Long almanaxId) {
        Long userId = authenticatedUserProvider.getUserId();
        subscriptionRepository.remove(userId, almanaxId);
    }
}
