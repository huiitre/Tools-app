package fr.huiitre.tools.modules.riot.valorant.application.catalog.usecase;

import java.util.Optional;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.skin.view.ValorantSkinView;

@Service
public class GetValorantSkinUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final ValorantSkinRepository skinRepository;

    public GetValorantSkinUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            ValorantSkinRepository skinRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.skinRepository = skinRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.RIOT);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public ValorantSkinView execute(Long id) {
        Long userId = authenticatedUserProvider.getUserId();
        return skinRepository.findById(id, userId)
                .orElseThrow(() -> new IllegalArgumentException("Skin not found: " + id));
    }
}
