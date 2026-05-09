package fr.huiitre.tools.modules.core.role.application.usecase;

import java.util.List;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.role.application.ports.RoleRepository;
import fr.huiitre.tools.modules.core.role.api.view.RoleView;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;

@Service
@Transactional
public class GetAllRolesUseCase implements SecuredUseCase {

    @Override
    public RoleCode requiredRole() {
        return RoleCode.ADMIN;
    }

    private final RoleRepository roleRepository;

    public GetAllRolesUseCase(
            RoleRepository roleRepository) {
        this.roleRepository = roleRepository;
    }

    public List<RoleView> execute() {
        return roleRepository.findAll();
    }
}
