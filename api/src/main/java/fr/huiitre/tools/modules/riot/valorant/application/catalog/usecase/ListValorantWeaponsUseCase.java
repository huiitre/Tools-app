package fr.huiitre.tools.modules.riot.valorant.application.catalog.usecase;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantWeaponRepository;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.ValorantWeaponView;

@Service
public class ListValorantWeaponsUseCase implements SecuredUseCase {

    private final ValorantWeaponRepository weaponRepository;

    public ListValorantWeaponsUseCase(ValorantWeaponRepository weaponRepository) {
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

    public List<ValorantWeaponView> execute() {
        return weaponRepository.findAll();
    }
}
