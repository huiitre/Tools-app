CREATE SCHEMA IF NOT EXISTS tools_backup;

-----------------------------------------------------------
-------------------- TOOLS CORE ---------------------------
-----------------------------------------------------------
CREATE TABLE IF NOT EXISTS tools_backup.user_backup (
    user_id BIGINT PRIMARY KEY,
    email   TEXT NOT NULL
);

INSERT INTO tools_backup.user_backup (user_id, email)
SELECT
    iduser,
    email
FROM tools_core.user;

-----------------------------------------------------------
-------------------- TOOLS HEALTH -------------------------
-----------------------------------------------------------
CREATE TABLE IF NOT EXISTS tools_backup.health_weight_log (
    user_email TEXT NOT NULL,
    logged_at  TIMESTAMPTZ NOT NULL,
    weight  NUMERIC(5,2) NOT NULL,
    notes      TEXT
);

INSERT INTO tools_backup.health_weight_log (
    user_email,
    logged_at,
    weight,
    notes
)
SELECT
    u.email,
    h.logged_at,
    h.weight,
    h.notes
FROM tools_health.weight_log h
JOIN tools_core.user u ON u.iduser = h.iduser;

-----------------------------------------------------------
-------------------- TOOLS TODOLIST -----------------------
-----------------------------------------------------------
CREATE TABLE IF NOT EXISTS tools_backup.todolist_todolist (
    user_email TEXT NOT NULL,
    idtodolist BIGINT NOT NULL,
    name       VARCHAR(255) NOT NULL,
    is_active  BOOLEAN NOT NULL DEFAULT TRUE,
    is_favorite BOOLEAN NOT NULL DEFAULT FALSE,
    color_hex  VARCHAR(7),
    display_order INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT now(),
    updated_at TIMESTAMP NOT NULL
);
CREATE TABLE IF NOT EXISTS tools_backup.todolist_todo (
    idtodo BIGINT NOT NULL,
    idtodolist BIGINT NOT NULL,
    name       VARCHAR(255) NOT NULL,
    description TEXT,
    is_completed BOOLEAN NOT NULL DEFAULT FALSE,
    priority INTEGER NOT NULL DEFAULT 0,
    display_order INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT now(),
    updated_at TIMESTAMP NOT NULL
);

INSERT INTO tools_backup.todolist_todolist (
    user_email,
    idtodolist,
    name,
    is_active,
    is_favorite,
    color_hex,
    display_order,
    created_at,
    updated_at
)
SELECT
    u.email,
    t.idtodolist,
    t.name,
    t.is_active,
    t.is_favorite,
    t.color_hex,
    t.display_order,
    t.created_at,
    t.updated_at
FROM tools_todolist.todolist t
JOIN tools_core.user u ON u.iduser = t.iduser;

INSERT INTO tools_backup.todolist_todo (
    idtodo,
    idtodolist,
    name,
    description,
    is_completed,
    priority,
    display_order,
    created_at,
    updated_at
)
SELECT
    t.idtodo,
    tl.idtodolist,
    t.name,
    t.description,
    t.is_completed,
    t.priority,
    t.display_order,
    t.created_at,
    t.updated_at
FROM tools_todolist.todo t
JOIN tools_backup.todolist_todolist tl ON tl.idtodolist = t.idtodolist;