package fr.huiitre.tools.modules.core.security.application.ports;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;

public interface ModuleAuthorizationPort {

    boolean hasAccess(String userId, ModuleCode moduleCode);
}