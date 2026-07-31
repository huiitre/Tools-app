package fr.huiitre.tools.modules.riot.valorant.api;

import java.util.List;
import java.util.UUID;

import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.usecase.GetValorantSkinByAssetIdUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.usecase.GetValorantSkinByLevelUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.usecase.GetValorantSkinUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.usecase.ListValorantSkinsUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.usecase.ListValorantSkinsByThemeUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.skin.view.ValorantSkinView;

@RestController
@RequestMapping("/riot/valorant")
public class ValorantSkinController {

    private final ListValorantSkinsUseCase listValorantSkinsUseCase;
    private final GetValorantSkinUseCase getSkinUseCase;
    private final GetValorantSkinByAssetIdUseCase getSkinByAssetIdUseCase;
    private final GetValorantSkinByLevelUseCase getSkinByLevelUseCase;
    private final ListValorantSkinsByThemeUseCase listSkinsByThemeUseCase;

    public ValorantSkinController(
            ListValorantSkinsUseCase listValorantSkinsUseCase,
            GetValorantSkinUseCase getSkinUseCase,
            GetValorantSkinByAssetIdUseCase getSkinByAssetIdUseCase,
            GetValorantSkinByLevelUseCase getSkinByLevelUseCase,
            ListValorantSkinsByThemeUseCase listSkinsByThemeUseCase) {
        this.listValorantSkinsUseCase = listValorantSkinsUseCase;
        this.getSkinUseCase = getSkinUseCase;
        this.getSkinByAssetIdUseCase = getSkinByAssetIdUseCase;
        this.getSkinByLevelUseCase = getSkinByLevelUseCase;
        this.listSkinsByThemeUseCase = listSkinsByThemeUseCase;
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/skins")
    public List<ValorantSkinView> listSkins(@RequestParam(required = false) Long accountId) {
        return listValorantSkinsUseCase.execute(accountId);
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/skins/{id}")
    public ValorantSkinView getSkin(@PathVariable Long id, @RequestParam(required = false) Long accountId) {
        return getSkinUseCase.execute(id, accountId);
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/skins/by-asset/{assetId}")
    public ValorantSkinView getSkinByAssetId(@PathVariable UUID assetId, @RequestParam(required = false) Long accountId) {
        return getSkinByAssetIdUseCase.execute(assetId, accountId);
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/skins/by-level/{levelAssetId}")
    public ValorantSkinView getSkinByLevel(@PathVariable UUID levelAssetId, @RequestParam(required = false) Long accountId) {
        return getSkinByLevelUseCase.execute(levelAssetId, accountId);
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/skins/by-theme/{themeUuid}")
    public List<ValorantSkinView> listSkinsByTheme(@PathVariable UUID themeUuid, @RequestParam(required = false) Long accountId) {
        return listSkinsByThemeUseCase.execute(themeUuid, accountId);
    }
}
