package fr.huiitre.tools.modules.core.role.api.view;

public class RoleView {

    private final Long id;
    private final String code;
    private final String name;
    private final String description;
    private final boolean active;

    public RoleView(
            Long id,
            String code,
            String name,
            String description,
            boolean active) {
        this.id = id;
        this.code = code;
        this.name = name;
        this.description = description;
        this.active = active;
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

    public boolean getActive() {
        return this.active;
    }
}
