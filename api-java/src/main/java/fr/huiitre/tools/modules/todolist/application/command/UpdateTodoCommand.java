package fr.huiitre.tools.modules.todolist.application.command;

import fr.huiitre.tools.modules.todolist.domain.TodoPriority;

public class UpdateTodoCommand {
    
    public String name;
    public String description;
    public Boolean completed;
    public Long displayOrder;
    public TodoPriority priority;

    public UpdateTodoCommand(
        String name,
        String description,
        Boolean completed,
        Long displayOrder,
        TodoPriority priority
    ) {
        this.name = name;
        this.description = description;
        this.completed = completed;
        this.displayOrder = displayOrder;
        this.priority = priority;
    }

    public String getName() {
        return name;
    }
    public String getDescription() {
        return description;
    }
    public Boolean isCompleted() {
        return completed;
    }
    public Long getDisplayOrder() {
        return displayOrder;
    }
    public TodoPriority getPriority() {
        return priority;
    }
}
