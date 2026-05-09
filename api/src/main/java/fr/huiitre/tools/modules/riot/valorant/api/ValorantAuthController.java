package fr.huiitre.tools.modules.riot.valorant.api;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.riot.valorant.application.core.command.RefreshTokenCommand;
import fr.huiitre.tools.modules.riot.valorant.application.core.usecase.RefreshValorantTokenUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.core.view.ValorantTokenView;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/riot/valorant")
public class ValorantAuthController {

    private final RefreshValorantTokenUseCase refreshValorantTokenUseCase;

    public ValorantAuthController(RefreshValorantTokenUseCase refreshValorantTokenUseCase) {
        this.refreshValorantTokenUseCase = refreshValorantTokenUseCase;
    }

    @RequiredRole(RoleCode.USER)
    @PostMapping("/refresh-token")
    public ValorantTokenView refreshToken(@RequestBody RefreshTokenCommand command) {
        return refreshValorantTokenUseCase.execute(command);
    }
}
