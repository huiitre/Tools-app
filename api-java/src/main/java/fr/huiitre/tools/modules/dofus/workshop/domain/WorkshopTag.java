package fr.huiitre.tools.modules.dofus.workshop.domain;

public class WorkshopTag {
    
    private Long id;
    private String name;
    private String color;

    private WorkshopTag(Long id, String name, String color) {

        validateName(name);
        validateColor(color);

        this.id = id;
        this.name = name.trim();
        this.color = color.trim();
    }

    public static WorkshopTag create(String name, String color) {
        return new WorkshopTag(null, name, color);
    }

    public static WorkshopTag rehydrate(Long id, String name, String color) {
        return new WorkshopTag(id, name, color);
    }

    public void update(String name, String color) {
        validateName(name);
        validateColor(color);
        this.name = name.trim();
        this.color = color.trim();
    }

    private void validateName(String name) {
        if (name == null || name.isBlank()) {
            throw new IllegalArgumentException("Le nom du tag ne peut pas être vide.");
        }

        if (name.length() > 20) {
            throw new IllegalArgumentException("Le nom du tag ne peut pas dépasser 20 caractères.");
        }
    }

    private void validateColor(String color) {
        if (color == null || color.isBlank()) {
            throw new IllegalArgumentException("La couleur du tag ne peut pas être vide.");
        }

        if (!color.matches("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")) {
            throw new IllegalArgumentException("La couleur du tag doit être au format hexadécimal.");
        }
    }

    public Long getId() {
        return id;
    }

    public String getName() {
        return name;
    }

    public String getColor() {
        return color;
    }
}
