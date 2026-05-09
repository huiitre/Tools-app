package fr.huiitre.tools.modules.core.security.infrastructure;

import fr.huiitre.tools.modules.core.security.application.ports.ModuleAuthorizationPort;
import fr.huiitre.tools.modules.core.module.domain.ModuleCode;

public class FakeModuleAuthorizationAdapter implements ModuleAuthorizationPort {

    @Override
    public boolean hasAccess(String userId, ModuleCode moduleCode) {
        // TEMPORAIRE : tout est autorisé
        return true;
    }
}
