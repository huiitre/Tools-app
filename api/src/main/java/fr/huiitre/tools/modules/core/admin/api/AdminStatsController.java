package fr.huiitre.tools.modules.core.admin.api;

import fr.huiitre.tools.modules.core.admin.application.usecase.GetAdminStatsUseCase;
import fr.huiitre.tools.modules.core.admin.application.view.AdminStatsView;
import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/admin")
public class AdminStatsController {

    private final GetAdminStatsUseCase getAdminStatsUseCase;

    public AdminStatsController(GetAdminStatsUseCase getAdminStatsUseCase) {
        this.getAdminStatsUseCase = getAdminStatsUseCase;
    }

    @RequiredRole(RoleCode.ADMIN)
    @GetMapping("/stats")
    public AdminStatsView getStats() {
        return getAdminStatsUseCase.execute();
    }
}
