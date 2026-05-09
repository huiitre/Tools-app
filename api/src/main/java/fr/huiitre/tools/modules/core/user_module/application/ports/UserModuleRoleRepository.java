package fr.huiitre.tools.modules.core.user_module.application.ports;

import java.util.List;
import java.util.Optional;

import fr.huiitre.tools.modules.core.user_module.application.view.ModuleUserView;
import fr.huiitre.tools.modules.core.user_module.application.view.UserModuleView;
import fr.huiitre.tools.modules.core.user_module.domain.UserModuleRole;

public interface UserModuleRoleRepository {

    void save(UserModuleRole userModuleRole);

    void deleteByUserIdAndModuleId(UserModuleRole userModuleRole);

    Optional<UserModuleRole> findByUserIdAndModuleId(Long userId, Long moduleId);

    void deleteByModuleId(Long moduleId);

    void updateRoleId(UserModuleRole userModuleRole);

    List<UserModuleView> findAllByUserId(Long userId);

    List<ModuleUserView> findAllByModuleId(Long moduleId);

}
