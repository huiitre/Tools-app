package fr.huiitre.tools.modules.riot.valorant.application.catalog.usecase;

import java.util.Optional;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantBundleRepository;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.ValorantBundleView;

@Service
public class GetValorantBundleUseCase implements SecuredUseCase {

    private final ValorantBundleRepository bundleRepository;

    public GetValorantBundleUseCase(ValorantBundleRepository bundleRepository) {
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

    public ValorantBundleView execute(Long id) {
        return bundleRepository.findById(id)
                .orElseThrow(() -> new IllegalArgumentException("Bundle not found: " + id));
    }
}
