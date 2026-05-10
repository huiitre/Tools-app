package fr.huiitre.tools.modules.riot.valorant.application.core.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.CurrentUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.ValorantAuthRepository;
import fr.huiitre.tools.modules.riot.valorant.application.core.view.ValorantTokenView;
import org.springframework.stereotype.Service;

import java.util.Optional;

@Service
public class GetValorantAccessTokenUseCase implements SecuredUseCase {

    private final ValorantAuthService valorantAuthService;
    private final ValorantAuthRepository valorantAuthRepository;
    private final CurrentUserProvider currentUserProvider;

    public GetValorantAccessTokenUseCase(ValorantAuthService valorantAuthService,
                                         ValorantAuthRepository valorantAuthRepository,
                                         CurrentUserProvider currentUserProvider) {
        this.valorantAuthService = valorantAuthService;
        this.valorantAuthRepository = valorantAuthRepository;
        this.currentUserProvider = currentUserProvider;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.RIOT);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public ValorantTokenView execute() {
        long userId = Long.parseLong(currentUserProvider.getCurrentUserId());
        String accessToken = valorantAuthService.getOrRefreshAccessToken(userId);
        return new ValorantTokenView(accessToken);
    }

    public void logout() {
        long userId = Long.parseLong(currentUserProvider.getCurrentUserId());
        valorantAuthRepository.deleteByUserId(userId);
    }
}

