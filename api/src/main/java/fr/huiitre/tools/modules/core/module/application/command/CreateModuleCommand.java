package fr.huiitre.tools.modules.core.module.application.command;

public class CreateModuleCommand {
    private final String name;
    private final String description;
    private final String code;

    public CreateModuleCommand(String name, String description, String code) {

        if (name == null || name.isBlank())
            throw new IllegalArgumentException("NAME_REQUIRED");

        if (description == null || description.isBlank())
            throw new IllegalArgumentException("DESCRIPTION_REQUIRED");

        if (code == null || code.isBlank())
            throw new IllegalArgumentException("CODE_REQUIRED");

        this.name = name;
        this.description = description;
        this.code = code;
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
}
