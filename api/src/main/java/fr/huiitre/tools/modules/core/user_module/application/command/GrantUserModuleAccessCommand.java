package fr.huiitre.tools.modules.core.user_module.application.command;

public class GrantUserModuleAccessCommand {

    private final Long userId;
    private final Long moduleId;

    public GrantUserModuleAccessCommand(Long userId, Long moduleId) {
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
