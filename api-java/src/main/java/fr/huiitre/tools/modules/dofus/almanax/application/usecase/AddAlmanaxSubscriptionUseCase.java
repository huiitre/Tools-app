package fr.huiitre.tools.modules.dofus.almanax.application.usecase;

import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.almanax.application.command.AddAlmanaxSubscriptionCommand;
import fr.huiitre.tools.modules.dofus.almanax.application.ports.AlmanaxSubscriptionRepository;

@Service
@Transactional
public class AddAlmanaxSubscriptionUseCase implements SecuredUseCase {

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

    public AddAlmanaxSubscriptionUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            AlmanaxSubscriptionRepository subscriptionRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.subscriptionRepository = subscriptionRepository;
    }

    public void execute(AddAlmanaxSubscriptionCommand command) {
        Long userId = authenticatedUserProvider.getUserId();

        if (subscriptionRepository.existsByUserIdAndAlmanaxId(userId, command.getAlmanaxId())) {
            throw new IllegalArgumentException("ALMANAX_ALREADY_SUBSCRIBED");
        }

        subscriptionRepository.add(userId, command.getAlmanaxId(), command.getDate());
    }
}
