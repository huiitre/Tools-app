package fr.huiitre.tools.modules.core.role.application.ports;

import java.util.List;
import java.util.Optional;

import fr.huiitre.tools.modules.core.role.api.view.RoleView;
import fr.huiitre.tools.modules.core.role.domain.Role;

public interface RoleRepository {

    void save(Role role);

    Optional<Role> findById(Long id);

    Optional<Role> findByCode(String code);

    List<RoleView> findAll();
}
