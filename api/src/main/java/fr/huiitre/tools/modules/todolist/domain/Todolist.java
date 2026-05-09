package fr.huiitre.tools.modules.todolist.domain;

public class Todolist {

    private Long id;

    private String name;
    private boolean active;
    private boolean favorite;
    private String colorHex;
    private Long displayOrder;

    private Todolist(
            Long id,
            String name,
            boolean active,
            boolean favorite,
            String colorHex,
            Long displayOrder) {
        this.id = id;
        this.name = name;
        this.active = active;
        this.favorite = favorite;
        this.colorHex = colorHex;
        this.displayOrder = displayOrder;

        validateFields();
    }

    public static Todolist rehydrate(
            Long id,
            String name,
            boolean active,
            boolean favorite,
            String colorHex,
            Long displayOrder) {

        if (id == null) {
            throw new IllegalArgumentException("TODOLIST_ID_REQUIRED");
        }

        return new Todolist(
                id,
                name,
                active,
                favorite,
                colorHex,
                displayOrder);
    }

    public static Todolist create(
            String name,
            Boolean active,
            Boolean favorite,
            String colorHex,
            Long displayOrder) {

        return new Todolist(
                null,
                name,
                active != null ? active : true,
                favorite != null ? favorite : false,
                colorHex != null ? colorHex : "#FFFFFF",
                displayOrder != null ? displayOrder : 0L);
    }

    public void update(
            String name,
            Boolean active,
            Boolean favorite,
            String colorHex,
            Long displayOrder) {

        if (name != null) {
            this.name = name;
        }
        if (active != null) {
            this.active = active;
        }
        if (favorite != null) {
            this.favorite = favorite;
        }
        if (colorHex != null) {
            this.colorHex = colorHex;
        }
        if (displayOrder != null) {
            this.displayOrder = displayOrder;
        }

        validateFields();
    }

    private void validateFields() {
        if (name == null || name.isBlank()) {
            throw new IllegalArgumentException("NAME_REQUIRED");
        }

        if (colorHex == null || colorHex.isBlank()) {
            throw new IllegalArgumentException("COLOR_HEX_REQUIRED");
        }

        if (active == false && favorite == true) {
            throw new IllegalArgumentException("INACTIVE_TODOLIST_CANNOT_BE_FAVORITE");
        }

        if (!colorHex.matches("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")) {
            throw new IllegalArgumentException("COLOR_HEX_INVALID");
        }
    }

    public Long getId() {
        return this.id;
    }

    public String getName() {
        return this.name;
    }

    public boolean isActive() {
        return this.active;
    }

    public boolean isFavorite() {
        return this.favorite;
    }

    public String getColorHex() {
        return this.colorHex;
    }

    public Long getDisplayOrder() {
        return this.displayOrder;
    }
}
