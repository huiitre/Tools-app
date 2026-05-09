package fr.huiitre.tools.modules.riot.valorant.application.catalog.usecase;

import java.util.Optional;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantWeaponRepository;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.ValorantWeaponView;

@Service
public class GetValorantWeaponUseCase implements SecuredUseCase {

    private final ValorantWeaponRepository weaponRepository;

    public GetValorantWeaponUseCase(ValorantWeaponRepository weaponRepository) {
        this.weaponRepository = weaponRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.RIOT);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public ValorantWeaponView execute(Long id) {
        return weaponRepository.findById(id)
                .orElseThrow(() -> new IllegalArgumentException("Weapon not found: " + id));
    }
}
