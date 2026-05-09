package fr.huiitre.tools.modules.riot.valorant.application.usecase;

import java.util.Optional;
import java.util.UUID;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.ports.ValorantBundleRepository;
import fr.huiitre.tools.modules.riot.valorant.application.view.ValorantBundleView;

@Service
public class GetValorantBundleByAssetIdUseCase implements SecuredUseCase {

    private final ValorantBundleRepository bundleRepository;

    public GetValorantBundleByAssetIdUseCase(ValorantBundleRepository bundleRepository) {
        this.bundleRepository = bundleRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.RIOT);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public ValorantBundleView execute(UUID assetId) {
        return bundleRepository.findByAssetId(assetId)
                .orElseThrow(() -> new IllegalArgumentException("Bundle not found: " + assetId));
    }
}
