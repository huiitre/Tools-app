package fr.huiitre.tools.modules.core.module.application.command;

public class UpdateModuleCommand {

    private final String name;
    private final String description;
    private final String code;
    private final Boolean active;

    public UpdateModuleCommand(String name, String description, String code, boolean active) {
        this.name = name;
        this.description = description;
        this.code = code;
        this.active = active;
    }

    public String getName() {
        return this.name;
    }

    public String getDescription() {
        return this.description;
    }

    public String getCode() {
        return this.code;
    }

    public Boolean getActive() {
        return this.active;
    }
}
