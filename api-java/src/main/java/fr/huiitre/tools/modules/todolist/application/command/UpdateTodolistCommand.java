package fr.huiitre.tools.modules.todolist.application.command;

public class UpdateTodolistCommand {
    private final String name;
    private final Boolean active;
    private final Boolean favorite;
    private final String colorHex;
    private final Long displayOrder;

    public UpdateTodolistCommand(
        String name,
        Boolean active,
        Boolean favorite,
        String colorHex,
        Long displayOrder
    ) {
        this.name = name;
        this.active = active;
        this.favorite = favorite;
        this.colorHex = colorHex;
        this.displayOrder = displayOrder;
    }

    public String getName() {
        return name;
    }

    public Boolean isActive() {
        return active;
    }

    public Boolean isFavorite() {
        return favorite;
    }

    public String getColorHex() {
        return colorHex;
    }

    public Long getDisplayOrder() {
        return displayOrder;
    }
}