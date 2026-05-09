package fr.huiitre.tools.modules.riot.valorant.application.catalog.usecase;

import java.util.Optional;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.ValorantSkinView;

@Service
public class GetValorantSkinUseCase implements SecuredUseCase {

    private final ValorantSkinRepository skinRepository;

    public GetValorantSkinUseCase(ValorantSkinRepository skinRepository) {
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
        return skinRepository.findById(id)
                .orElseThrow(() -> new IllegalArgumentException("Skin not found: " + id));
    }
}
