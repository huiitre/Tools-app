package fr.huiitre.tools.modules.core.module.api;

import java.util.List;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.core.module.application.command.CreateModuleCommand;
import fr.huiitre.tools.modules.core.module.application.command.UpdateModuleCommand;
import fr.huiitre.tools.modules.core.module.application.usecase.CreateModuleUseCase;
import fr.huiitre.tools.modules.core.module.application.usecase.DeleteModuleUseCase;
import fr.huiitre.tools.modules.core.module.application.usecase.GetAllModulesUseCase;
import fr.huiitre.tools.modules.core.module.application.usecase.UpdateModuleUseCase;
import fr.huiitre.tools.modules.core.user_module.application.command.ChangeUserModuleRoleCommand;
import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.module.api.dto.ChangeUserModuleRoleRequest;
import fr.huiitre.tools.modules.core.module.api.dto.ModuleTechDto;
import fr.huiitre.tools.modules.core.module.domain.Module;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.user_module.application.command.GrantUserModuleAccessCommand;
import fr.huiitre.tools.modules.core.user_module.application.command.RevokeUserModuleAccessCommand;
import fr.huiitre.tools.modules.core.user_module.application.usecase.ChangeUserModuleRoleUseCase;
import fr.huiitre.tools.modules.core.user_module.application.usecase.GetModuleUsersUseCase;
import fr.huiitre.tools.modules.core.user_module.application.usecase.GetUserModulesUseCase;
import fr.huiitre.tools.modules.core.user_module.application.usecase.GrantUserModuleAccessUseCase;
import fr.huiitre.tools.modules.core.user_module.application.usecase.RevokeUserModuleAccessUseCase;
import fr.huiitre.tools.modules.core.user_module.application.view.ModuleUserView;
import fr.huiitre.tools.modules.core.user_module.application.view.UserModuleView;
import io.swagger.v3.oas.annotations.tags.Tag;

@Tag(name = "Core - Module")
@RestController
@RequestMapping("/modules")
public class ModuleController {

    private static final Logger logger = LoggerFactory.getLogger(ModuleController.class);

    private final CreateModuleUseCase createModuleUseCase;
    private final DeleteModuleUseCase deleteModuleUseCase;
    private final UpdateModuleUseCase updateModuleUseCase;
    private final ChangeUserModuleRoleUseCase changeUserModuleRoleUseCase;
    private final GrantUserModuleAccessUseCase grantUserModuleAccessUseCase;
    private final RevokeUserModuleAccessUseCase revokeUserModuleAccessUseCase;
    private final GetAllModulesUseCase getAllModulesUseCase;
    private final GetUserModulesUseCase getUserModulesUseCase;
    private final GetModuleUsersUseCase getModuleUsersUseCase;

    public ModuleController(
            CreateModuleUseCase createModuleUseCase,
            DeleteModuleUseCase deleteModuleUseCase,
            UpdateModuleUseCase updateModuleUseCase,
            ChangeUserModuleRoleUseCase changeUserModuleRoleUseCase,
            GrantUserModuleAccessUseCase grantUserModuleAccessUseCase,
            RevokeUserModuleAccessUseCase revokeUserModuleAccessUseCase,
            GetAllModulesUseCase getAllModulesUseCase,
            GetUserModulesUseCase getUserModulesUseCase,
            GetModuleUsersUseCase getModuleUsersUseCase) {
        this.createModuleUseCase = createModuleUseCase;
        this.deleteModuleUseCase = deleteModuleUseCase;
        this.updateModuleUseCase = updateModuleUseCase;
        this.changeUserModuleRoleUseCase = changeUserModuleRoleUseCase;
        this.grantUserModuleAccessUseCase = grantUserModuleAccessUseCase;
        this.revokeUserModuleAccessUseCase = revokeUserModuleAccessUseCase;
        this.getAllModulesUseCase = getAllModulesUseCase;
        this.getUserModulesUseCase = getUserModulesUseCase;
        this.getModuleUsersUseCase = getModuleUsersUseCase;
    }

    @RequiredRole(RoleCode.TECH)
    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public void createModule(@RequestBody CreateModuleCommand command) {
        createModuleUseCase.execute(command);
    }

    @RequiredRole(RoleCode.TECH)
    @PutMapping("/{moduleId}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void updateModule(
            @PathVariable Long moduleId,
            @RequestBody UpdateModuleCommand command) {
        updateModuleUseCase.execute(moduleId, command);
    }

    @RequiredRole(RoleCode.TECH)
    @DeleteMapping("/{moduleId}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void deleteModule(@PathVariable Long moduleId) {
        deleteModuleUseCase.execute(moduleId);
    }

    @RequiredRole(RoleCode.ADMIN)
    @PostMapping("/{moduleId}/users/{userId}")
    @ResponseStatus(HttpStatus.CREATED)
    public void grantUserAccess(
            @PathVariable Long moduleId,
            @PathVariable Long userId) {
        grantUserModuleAccessUseCase.execute(new GrantUserModuleAccessCommand(userId, moduleId));
    }

    @RequiredRole(RoleCode.ADMIN)
    @PutMapping("/{moduleId}/users/{userId}/role")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void changeUserModuleRole(
            @PathVariable Long moduleId,
            @PathVariable Long userId,
            @RequestBody ChangeUserModuleRoleRequest request) {
        changeUserModuleRoleUseCase.execute(new ChangeUserModuleRoleCommand(userId, moduleId, request.getRoleId()));
    }

    @RequiredRole(RoleCode.ADMIN)
    @DeleteMapping("/{moduleId}/users/{userId}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void revokeUserAccess(
            @PathVariable Long moduleId,
            @PathVariable Long userId) {
        revokeUserModuleAccessUseCase.execute(new RevokeUserModuleAccessCommand(userId, moduleId));
    }

    @RequiredRole(RoleCode.TECH)
    @GetMapping
    public List<ModuleTechDto> getAllModules() {
        List<Module> modules = getAllModulesUseCase.execute();
        return modules.stream()
                .map(module -> new ModuleTechDto(
                        module.getId(),
                        module.getCode(),
                        module.getName(),
                        module.getDescription(),
                        module.getActive(),
                        module.getCreatedAt(),
                        module.getUpdatedAt()))
                .toList();
    }

    @RequiredRole(RoleCode.ADMIN)
    @GetMapping("/users/{userId}")
    public List<UserModuleView> getUserModules(@PathVariable Long userId) {
        return getUserModulesUseCase.execute(userId);
    }

    @RequiredRole(RoleCode.ADMIN)
    @GetMapping("/{moduleId}/users")
    public List<ModuleUserView> getModuleUsers(@PathVariable Long moduleId) {
        return getModuleUsersUseCase.execute(moduleId);
    }
}
