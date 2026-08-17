package fr.huiitre.tools.modules.riot.valorant.application.core.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.ValorantAuthRepository;
import org.springframework.stereotype.Service;

import java.util.Optional;

@Service
public class UnlinkValorantAccountUseCase implements SecuredUseCase {

    private final ValorantAuthRepository valorantAuthRepository;
    private final AuthenticatedUserProvider authenticatedUserProvider;

    public UnlinkValorantAccountUseCase(ValorantAuthRepository valorantAuthRepository,
                                         AuthenticatedUserProvider authenticatedUserProvider) {
        this.valorantAuthRepository = valorantAuthRepository;
        this.authenticatedUserProvider = authenticatedUserProvider;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.RIOT);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public void execute(Long accountId) {
        Long userId = authenticatedUserProvider.getUserId();
        if (!valorantAuthRepository.existsByIdAndUserId(accountId, userId)) {
            throw new IllegalArgumentException("VALORANT_ACCOUNT_NOT_FOUND");
        }
        valorantAuthRepository.deleteById(accountId);
    }
}
