package fr.huiitre.tools.modules.riot.valorant.api;

import java.util.List;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.usecase.GetValorantWeaponSkinsUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.usecase.GetValorantWeaponUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.usecase.ListValorantWeaponsUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.skin.view.ValorantSkinView;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.ValorantWeaponView;

@RestController
@RequestMapping("/riot/valorant")
public class ValorantWeaponController {

    private final ListValorantWeaponsUseCase listWeaponsUseCase;
    private final GetValorantWeaponUseCase getWeaponUseCase;
    private final GetValorantWeaponSkinsUseCase getWeaponSkinsUseCase;

    public ValorantWeaponController(
            ListValorantWeaponsUseCase listWeaponsUseCase,
            GetValorantWeaponUseCase getWeaponUseCase,
            GetValorantWeaponSkinsUseCase getWeaponSkinsUseCase) {
        this.listWeaponsUseCase = listWeaponsUseCase;
        this.getWeaponUseCase = getWeaponUseCase;
        this.getWeaponSkinsUseCase = getWeaponSkinsUseCase;
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/weapons")
    public List<ValorantWeaponView> listWeapons() {
        return listWeaponsUseCase.execute();
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/weapons/{id}")
    public ValorantWeaponView getWeapon(@PathVariable Long id) {
        return getWeaponUseCase.execute(id);
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/weapons/{id}/skins")
    public List<ValorantSkinView> getWeaponSkins(@PathVariable Long id, @RequestParam(required = false) Long accountId) {
        return getWeaponSkinsUseCase.execute(id, accountId);
    }
}
