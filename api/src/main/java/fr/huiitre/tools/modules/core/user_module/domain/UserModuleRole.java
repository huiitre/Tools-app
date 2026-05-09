package fr.huiitre.tools.modules.core.user_module.domain;

public class UserModuleRole {

    private final Long userId;
    private final Long moduleId;
    private Long roleId;

    public UserModuleRole(Long userId, Long moduleId, Long roleId) {
        if (userId == null) {
            throw new IllegalArgumentException("userId cannot be null");
        }
        if (moduleId == null) {
            throw new IllegalArgumentException("moduleId cannot be null");
        }
        if (roleId == null) {
            throw new IllegalArgumentException("roleId cannot be null");
        }

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

    public void changeRole(Long newRoleId) {
        if (newRoleId == null) {
            throw new IllegalArgumentException("roleId cannot be null");
        }
        this.roleId = newRoleId;
    }
}
