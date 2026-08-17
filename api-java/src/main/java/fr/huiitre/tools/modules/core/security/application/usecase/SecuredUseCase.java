package fr.huiitre.tools.modules.core.security.application.usecase;

import java.util.Optional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;

public interface SecuredUseCase {

    default Optional<ModuleCode> requiredModule() {
        return Optional.empty();
    }

    default RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }
}
