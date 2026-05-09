package fr.huiitre.tools.modules.todolist.domain;

public class Todo {
    private final Long id;
    private String name;
    private String description;
    private boolean completed;
    private final Long todolistId;
    private Long displayOrder;
    private TodoPriority priority;

    private Todo(
            Long id,
            String name,
            String description,
            boolean completed,
            Long todolistId,
            Long displayOrder,
            TodoPriority priority) {
        this.id = id;
        this.name = name;
        this.description = description;
        this.completed = completed;
        this.todolistId = todolistId;
        this.displayOrder = displayOrder;
        this.priority = priority;

        validateFields();
    }

    public static Todo rehydrate(
            Long id,
            String name,
            String description,
            boolean completed,
            Long todolistId,
            Long displayOrder,
            TodoPriority priority) {

        if (id == null) {
            throw new IllegalArgumentException("TODO_ID_REQUIRED");
        }

        return new Todo(
                id,
                name,
                description,
                completed,
                todolistId,
                displayOrder,
                priority);
    }

    public static Todo create(
            String name,
            String description,
            Long todolistId,
            Long displayOrder,
            TodoPriority priority) {
        return new Todo(
                null,
                name,
                description,
                false,
                todolistId,
                displayOrder != null ? displayOrder : 0L,
                priority != null ? priority : TodoPriority.NORMAL);
    }

    public void update(
            String name,
            String description,
            Boolean completed,
            Long displayOrder,
            TodoPriority priority) {

        if (name != null) {
            this.name = name;
        }

        if (description != null) {
            this.description = description;
        }

        if (displayOrder != null) {
            this.displayOrder = displayOrder;
        }

        if (priority != null) {
            this.priority = priority;
        }

        if (completed != null) {
            this.completed = completed;
        }

        validateFields();
    }

    private void validateFields() {
        if (name == null || name.isBlank()) {
            throw new IllegalArgumentException("TODO_NAME_REQUIRED");
        }
        if (todolistId == null) {
            throw new IllegalArgumentException("TODO_TODOLIST_ID_REQUIRED");
        }
        if (priority == null) {
            throw new IllegalArgumentException("TODO_PRIORITY_REQUIRED");
        }
    }

    public Long getId() {
        return id;
    }

    public String getName() {
        return name;
    }

    public String getDescription() {
        return description;
    }

    public boolean isCompleted() {
        return completed;
    }

    public Long getTodolistId() {
        return todolistId;
    }

    public Long getDisplayOrder() {
        return displayOrder;
    }

    public TodoPriority getPriority() {
        return priority;
    }
}
