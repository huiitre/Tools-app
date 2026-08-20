package fr.huiitre.tools.modules.core.security.application.ports;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;

public interface UserRoleProvider {

    /**
     * Retourne le rôle de l'utilisateur. Un utilisateur n'en détient qu'un, globalement comme
     * dans un module : il n'y a donc rien à arbitrer entre plusieurs rôles.
     * - Si moduleCode == null : rôle global (user_role)
     * - Sinon : rôle dans le module (user_module_role)
     */
    RoleCode getUserRole(String userId, ModuleCode moduleCode);
}
