package fr.huiitre.tools.modules.riot.valorant.application.user.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.ValorantAuthRepository;
import fr.huiitre.tools.modules.riot.valorant.application.user.ports.ValorantUserSkinRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.Optional;

@Service
@Transactional
public class RemoveMyValorantSkinUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final ValorantAuthRepository valorantAuthRepository;
    private final ValorantUserSkinRepository userSkinRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.RIOT);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public RemoveMyValorantSkinUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            ValorantAuthRepository valorantAuthRepository,
            ValorantUserSkinRepository userSkinRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.valorantAuthRepository = valorantAuthRepository;
        this.userSkinRepository = userSkinRepository;
    }

    public void execute(Long skinId, Long accountId) {
        if (!valorantAuthRepository.existsByIdAndUserId(accountId, authenticatedUserProvider.getUserId())) {
            throw new IllegalArgumentException("VALORANT_ACCOUNT_NOT_FOUND");
        }
        userSkinRepository.remove(accountId, skinId);
    }
}
