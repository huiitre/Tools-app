package fr.huiitre.tools.modules.core.user.application.usecase;

import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.user.application.ports.UserRepository;
import fr.huiitre.tools.modules.core.user.application.view.UserAdminView;
import org.springframework.stereotype.Service;

import java.util.List;

@Service
public class ListUsersUseCase implements SecuredUseCase {

    @Override
    public RoleCode requiredRole() {
        return RoleCode.ADMIN;
    }

    private final UserRepository userRepository;

    public ListUsersUseCase(UserRepository userRepository) {
        this.userRepository = userRepository;
    }

    public List<UserAdminView> execute() {
        return userRepository.findAllForAdmin();
    }
}
