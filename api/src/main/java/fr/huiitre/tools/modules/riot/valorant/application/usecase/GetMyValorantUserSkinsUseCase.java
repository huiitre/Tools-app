package fr.huiitre.tools.modules.riot.valorant.application.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.ports.ValorantUserSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.view.ValorantUserSkinView;
import org.springframework.stereotype.Service;

import java.util.List;
import java.util.Optional;

@Service
public class GetMyValorantUserSkinsUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final ValorantUserSkinRepository userSkinRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.RIOT);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public GetMyValorantUserSkinsUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            ValorantUserSkinRepository userSkinRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.userSkinRepository = userSkinRepository;
    }

    public List<ValorantUserSkinView> execute() {
        Long userId = authenticatedUserProvider.getUserId();
        return userSkinRepository.findAllByUserId(userId);
    }
}
