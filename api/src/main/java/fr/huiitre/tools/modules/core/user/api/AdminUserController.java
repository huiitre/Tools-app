package fr.huiitre.tools.modules.core.user.api;

import fr.huiitre.tools.modules.core.auth.api.dto.UserProfileDto;
import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.application.command.SetUserGlobalRoleCommand;
import fr.huiitre.tools.modules.core.role.application.usecase.SetUserGlobalRoleUseCase;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.user.application.usecase.GetUserDetailUseCase;
import fr.huiitre.tools.modules.core.user.application.usecase.ListUsersUseCase;
import fr.huiitre.tools.modules.core.user.application.view.UserAdminView;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/users")
public class AdminUserController {

    private final ListUsersUseCase listUsersUseCase;
    private final GetUserDetailUseCase getUserDetailUseCase;
    private final SetUserGlobalRoleUseCase setUserGlobalRoleUseCase;

    public AdminUserController(
            ListUsersUseCase listUsersUseCase,
            GetUserDetailUseCase getUserDetailUseCase,
            SetUserGlobalRoleUseCase setUserGlobalRoleUseCase) {
        this.listUsersUseCase = listUsersUseCase;
        this.getUserDetailUseCase = getUserDetailUseCase;
        this.setUserGlobalRoleUseCase = setUserGlobalRoleUseCase;
    }

    @RequiredRole(RoleCode.ADMIN)
    @GetMapping
    public List<UserAdminView> listUsers() {
        return listUsersUseCase.execute();
    }

    @RequiredRole(RoleCode.ADMIN)
    @GetMapping("/{userId}")
    public UserProfileDto getUserDetail(@PathVariable Long userId) {
        return getUserDetailUseCase.execute(userId);
    }

    @RequiredRole(RoleCode.ADMIN)
    @PutMapping("/{userId}/role")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void setUserRole(@PathVariable Long userId, @RequestBody SetUserGlobalRoleCommand command) {
        setUserGlobalRoleUseCase.execute(userId, command);
    }
}
