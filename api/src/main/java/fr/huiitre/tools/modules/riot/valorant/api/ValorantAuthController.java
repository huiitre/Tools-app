package fr.huiitre.tools.modules.riot.valorant.api;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.riot.valorant.application.core.command.RefreshTokenCommand;
import fr.huiitre.tools.modules.riot.valorant.application.core.usecase.GetValorantAccessTokenUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.core.usecase.RefreshValorantTokenUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.core.view.ValorantTokenView;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/riot/valorant")
public class ValorantAuthController {

    private final RefreshValorantTokenUseCase refreshValorantTokenUseCase;
    private final GetValorantAccessTokenUseCase getValorantAccessTokenUseCase;

    public ValorantAuthController(RefreshValorantTokenUseCase refreshValorantTokenUseCase,
                                   GetValorantAccessTokenUseCase getValorantAccessTokenUseCase) {
        this.refreshValorantTokenUseCase = refreshValorantTokenUseCase;
        this.getValorantAccessTokenUseCase = getValorantAccessTokenUseCase;
    }

    @RequiredRole(RoleCode.USER)
    @PostMapping("/refresh-token")
    public ValorantTokenView refreshToken(@RequestBody RefreshTokenCommand command) {
        return refreshValorantTokenUseCase.execute(command);
    }

    @RequiredRole(RoleCode.USER)
    @GetMapping("/refresh")
    public ValorantTokenView refresh() {
        return getValorantAccessTokenUseCase.execute();
    }

    @RequiredRole(RoleCode.USER)
    @DeleteMapping("/auth")
    public void logout() {
        getValorantAccessTokenUseCase.logout();
    }
}
