package fr.huiitre.tools.modules.riot.valorant.api;

import java.util.Map;

import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.riot.valorant.application.core.usecase.GetValorantVersionUseCase;

@RestController
@RequestMapping("/riot/valorant")
public class ValorantVersionController {

    private final GetValorantVersionUseCase getVersionUseCase;

    public ValorantVersionController(GetValorantVersionUseCase getVersionUseCase) {
        this.getVersionUseCase = getVersionUseCase;
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/version")
    public Map<String, Object> getVersion() {
        return getVersionUseCase.execute();
    }
}
