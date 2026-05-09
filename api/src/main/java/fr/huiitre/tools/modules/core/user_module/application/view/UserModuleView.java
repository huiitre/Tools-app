package fr.huiitre.tools.modules.core.user_module.application.view;

import java.util.List;

import fr.huiitre.tools.modules.core.role.api.view.RoleView;

public class UserModuleView {
    private final Long id;
    private final String code;
    private final String name;
    private final String description;
    private final boolean active;
    private final List<RoleView> roles;

    public UserModuleView(
            Long id,
            String code,
            String name,
            String description,
            boolean active,
            List<RoleView> roles) {
        this.id = id;
        this.code = code;
        this.name = name;
        this.description = description;
        this.active = active;
        this.roles = roles;
    }

    public Long getId() {
        return this.id;
    }

    public String getCode() {
        return this.code;
    }

    public String getName() {
        return this.name;
    }

    public String getDescription() {
        return this.description;
    }

    public boolean isActive() {
        return this.active;
    }

    public List<RoleView> getRoles() {
        return this.roles;
    }
}
