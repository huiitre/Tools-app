package fr.huiitre.tools.modules.riot.valorant.api;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.usecase.GetValorantStoreUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.ValorantStoreView;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/riot/valorant")
public class ValorantStoreController {

    private final GetValorantStoreUseCase getValorantStoreUseCase;

    public ValorantStoreController(GetValorantStoreUseCase getValorantStoreUseCase) {
        this.getValorantStoreUseCase = getValorantStoreUseCase;
    }

    @RequiredRole(RoleCode.USER)
    @GetMapping("/store")
    public ValorantStoreView getStore(@RequestHeader(value = "X-Riot-Token", required = false) String accessToken,
                                      @RequestParam(required = false) String region) {
        return getValorantStoreUseCase.execute(accessToken, region);
    }
}
