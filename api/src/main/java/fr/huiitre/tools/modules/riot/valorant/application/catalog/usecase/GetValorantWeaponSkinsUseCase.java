package fr.huiitre.tools.modules.riot.valorant.application.catalog.usecase;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantWeaponRepository;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.ValorantSkinView;

@Service
public class GetValorantWeaponSkinsUseCase implements SecuredUseCase {

    private final ValorantWeaponRepository weaponRepository;
    private final ValorantSkinRepository skinRepository;

    public GetValorantWeaponSkinsUseCase(ValorantWeaponRepository weaponRepository, ValorantSkinRepository skinRepository) {
        this.weaponRepository = weaponRepository;
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

    public List<ValorantSkinView> execute(Long weaponId) {
        weaponRepository.findById(weaponId)
                .orElseThrow(() -> new IllegalArgumentException("Weapon not found: " + weaponId));
        return skinRepository.findAllByWeaponId(weaponId);
    }
}
