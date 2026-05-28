package fr.huiitre.tools.modules.dofus.almanax.application.usecase;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.almanax.application.ports.AlmanaxSubscriptionRepository;

@Service
@Transactional(readOnly = true)
public class GetMyAlmanaxSubscriptionsUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final AlmanaxSubscriptionRepository subscriptionRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public GetMyAlmanaxSubscriptionsUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            AlmanaxSubscriptionRepository subscriptionRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.subscriptionRepository = subscriptionRepository;
    }

    public List<Long> execute() {
        Long userId = authenticatedUserProvider.getUserId();
        return subscriptionRepository.findAlmanaxIdsByUserId(userId);
    }
}
