-- tools_health
INSERT INTO tools_health.weight_log (
    user_id,
    logged_at,
    weight_kg,
    notes
)
SELECT
    u.id,
    h.logged_at,
    h.weight_kg,
    h.notes
FROM tools_backup.health_weight_log h
JOIN tools_core.users u
    ON u.email = h.user_email;

    -- tools_todolist
INSERT INTO tools_todolist.todolist (
    user_id,
    name,
    is_active,
    is_favorite,
    color_hex,
    display_order,
    created_at,
    updated_at
)
SELECT
    u.idtodolist,
    t.name,
    t.is_active,
    t.is_favorite,
    t.color_hex,
    t.display_order,
    t.created_at,
    t.updated_at
FROM tools_backup.todolist_todolist t
JOIN tools_core.users u
    ON u.email = t.user_email;

INSERT INTO tools_todolist.todo (
    idtodolist,
    idtodo,
    name,
    description,
    is_completed,
    priority,
    display_order,
    created_at,
    updated_at
)
SELECT
    tl.idtodolist,
    t.idtodo,
    t.name,
    t.description,
    t.is_completed,
    t.priority,
    t.display_order,
    t.created_at,
    t.updated_at
FROM tools_backup.todolist_todo t
JOIN tools_backup.todolist_todolist tl
    ON tl.idtodolist = t.idtodolist;