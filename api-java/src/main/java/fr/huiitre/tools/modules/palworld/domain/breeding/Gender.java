package fr.huiitre.tools.modules.palworld.domain.breeding;

public enum Gender {
    MALE,
    FEMALE;

    public static Gender fromCode(String code) {
        if (code == null) return null;
        return switch (code) {
            case "Male" -> MALE;
            case "Female" -> FEMALE;
            default -> throw new IllegalArgumentException("Unknown Palworld breeding gender code: " + code);
        };
    }

    public String toCode() {
        return this == MALE ? "Male" : "Female";
    }
}
