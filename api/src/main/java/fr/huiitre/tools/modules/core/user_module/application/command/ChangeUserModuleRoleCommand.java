package fr.huiitre.tools.modules.core.user_module.application.command;

public class ChangeUserModuleRoleCommand {

    private final Long userId;
    private final Long moduleId;
    private final Long roleId;

    public ChangeUserModuleRoleCommand(Long userId, Long moduleId, Long roleId) {

        if (userId == null)
            throw new IllegalArgumentException("USER_ID_REQUIRED");
        if (moduleId == null)
            throw new IllegalArgumentException("MODULE_ID_REQUIRED");
        if (roleId == null)
            throw new IllegalArgumentException("ROLE_ID_REQUIRED");

        this.userId = userId;
        this.moduleId = moduleId;
        this.roleId = roleId;
    }

    public Long getUserId() {
        return userId;
    }

    public Long getModuleId() {
        return moduleId;
    }

    public Long getRoleId() {
        return roleId;
    }
}
