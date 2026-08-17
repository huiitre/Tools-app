package fr.huiitre.tools.modules.riot.valorant.api;

import java.util.List;
import java.util.UUID;

import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.usecase.GetValorantBundleByAssetIdUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.usecase.GetValorantBundleUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.usecase.ListValorantBundlesUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.ValorantBundleView;

@RestController
@RequestMapping("/riot/valorant")
public class ValorantBundleController {

    private final ListValorantBundlesUseCase listBundlesUseCase;
    private final GetValorantBundleUseCase getBundleUseCase;
    private final GetValorantBundleByAssetIdUseCase getBundleByAssetIdUseCase;

    public ValorantBundleController(
            ListValorantBundlesUseCase listBundlesUseCase,
            GetValorantBundleUseCase getBundleUseCase,
            GetValorantBundleByAssetIdUseCase getBundleByAssetIdUseCase) {
        this.listBundlesUseCase = listBundlesUseCase;
        this.getBundleUseCase = getBundleUseCase;
        this.getBundleByAssetIdUseCase = getBundleByAssetIdUseCase;
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/bundles")
    public List<ValorantBundleView> listBundles() {
        return listBundlesUseCase.execute();
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/bundles/{id}")
    public ValorantBundleView getBundle(@PathVariable Long id) {
        return getBundleUseCase.execute(id);
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/bundles/by-asset/{assetId}")
    public ValorantBundleView getBundleByAssetId(@PathVariable UUID assetId) {
        return getBundleByAssetIdUseCase.execute(assetId);
    }
}
