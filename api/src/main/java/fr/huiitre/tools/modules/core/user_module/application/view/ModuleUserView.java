package fr.huiitre.tools.modules.core.user_module.application.view;

public class ModuleUserView {

    private final Long userId;
    private final String email;
    private final String name;
    private final Long roleId;
    private final String roleCode;

    public ModuleUserView(Long userId, String email, String name, Long roleId, String roleCode) {
        this.userId = userId;
        this.email = email;
        this.name = name;
        this.roleId = roleId;
        this.roleCode = roleCode;
    }

    public Long getUserId() { return userId; }
    public String getEmail() { return email; }
    public String getName() { return name; }
    public Long getRoleId() { return roleId; }
    public String getRoleCode() { return roleCode; }
}
