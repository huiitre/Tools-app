package fr.huiitre.tools.modules.core.user_module.application.command;

public class RevokeUserModuleAccessCommand {

    private final Long userId;
    private final Long moduleId;

    public RevokeUserModuleAccessCommand(Long userId, Long moduleId) {

        if (userId == null) {
            throw new IllegalArgumentException("userId cannot be null");
        }
        if (moduleId == null) {
            throw new IllegalArgumentException("moduleId cannot be null");
        }

        this.userId = userId;
        this.moduleId = moduleId;
    }

    public Long getUserId() {
        return userId;
    }

    public Long getModuleId() {
        return moduleId;
    }
}
