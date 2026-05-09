package fr.huiitre.tools.modules.riot.valorant.application.usecase;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.ports.ValorantSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.view.ValorantSkinView;

@Service
public class ListValorantSkinsByThemeUseCase implements SecuredUseCase {

    private final ValorantSkinRepository skinRepository;

    public ListValorantSkinsByThemeUseCase(ValorantSkinRepository skinRepository) {
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

    public List<ValorantSkinView> execute(UUID themeUuid) {
        return skinRepository.findAllByTierUuid(themeUuid);
    }
}
