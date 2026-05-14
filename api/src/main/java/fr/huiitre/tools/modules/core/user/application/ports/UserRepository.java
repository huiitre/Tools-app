package fr.huiitre.tools.modules.core.user.application.ports;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Optional;

import fr.huiitre.tools.modules.core.user.application.view.UserAdminView;
import fr.huiitre.tools.modules.core.user.domain.User;

public interface UserRepository {

    void save(User user);

    Optional<User> findByEmail(String email);

    Optional<User> findById(Long id);

    List<UserAdminView> findAllForAdmin();

    void deleteUnvalidatedUsersWithExpiredEmailVerification(LocalDateTime now);

    void deleteUnvalidatedUsersWithoutEmailVerification();

    List<Long> findAllIds();

    List<Long> findAllIdsByRoleId(Long roleId);

    List<Long> findAllIdsByRoleCodes(List<String> roleCodes);

    List<Long> findAllIdsByModuleId(Long moduleId);
}
