package fr.huiitre.tools.modules.core.admin.application.usecase;

import fr.huiitre.tools.modules.core.admin.application.ports.AdminStatsRepository;
import fr.huiitre.tools.modules.core.admin.application.view.AdminStatsView;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import org.springframework.stereotype.Service;

@Service
public class GetAdminStatsUseCase implements SecuredUseCase {

    @Override
    public RoleCode requiredRole() {
        return RoleCode.ADMIN;
    }

    private final AdminStatsRepository adminStatsRepository;

    public GetAdminStatsUseCase(AdminStatsRepository adminStatsRepository) {
        this.adminStatsRepository = adminStatsRepository;
    }

    public AdminStatsView execute() {
        return adminStatsRepository.getStats();
    }
}
