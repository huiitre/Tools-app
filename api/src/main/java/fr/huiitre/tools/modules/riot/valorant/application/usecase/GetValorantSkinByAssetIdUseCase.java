package fr.huiitre.tools.modules.riot.valorant.application.usecase;

import java.util.Optional;
import java.util.UUID;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.ports.ValorantSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.view.ValorantSkinView;

@Service
public class GetValorantSkinByAssetIdUseCase implements SecuredUseCase {

    private final ValorantSkinRepository skinRepository;

    public GetValorantSkinByAssetIdUseCase(ValorantSkinRepository skinRepository) {
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

    public ValorantSkinView execute(UUID assetId) {
        return skinRepository.findByAssetId(assetId)
                .orElseThrow(() -> new IllegalArgumentException("Skin not found: " + assetId));
    }
}
