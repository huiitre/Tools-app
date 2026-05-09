package fr.huiitre.tools.modules.core.auth.api.dto;

public class UserModuleDto {

    private final String code;
    private final String name;
    private final String description;
    private final String role;

    public UserModuleDto(String code, String name, String description, String role) {
        this.code = code;
        this.name = name;
        this.description = description;
        this.role = role;
    }

    public String getCode() { return code; }
    public String getName() { return name; }
    public String getDescription() { return description; }
    public String getRole() { return role; }
}
