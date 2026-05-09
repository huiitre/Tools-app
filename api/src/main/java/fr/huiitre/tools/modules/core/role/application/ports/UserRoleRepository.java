package fr.huiitre.tools.modules.core.role.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.core.role.api.view.RoleView;
import fr.huiitre.tools.modules.core.role.domain.UserRole;

public interface UserRoleRepository {

    void save(UserRole userRole);

    List<RoleView> findAllByUserId(Long userId);

    void deleteAllByUserId(Long userId);
}
