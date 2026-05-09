DROP SCHEMA IF EXISTS tools_todolist CASCADE;

CREATE SCHEMA IF NOT EXISTS tools_todolist;
COMMENT ON SCHEMA tools_todolist IS 'Module ToDoList – gestion des listes et tâches utilisateur';

-- Enum pour la priorité des tâches
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_type t
        JOIN pg_namespace n ON n.oid = t.typnamespace
        WHERE t.typname = 'todo_priority' AND n.nspname = 'tools_todolist'
    ) THEN
        CREATE TYPE tools_todolist.todo_priority AS ENUM ('NORMAL', 'HAUT', 'URGENT');
    END IF;
END $$;

-- Table todolist
CREATE TABLE tools_todolist.todolist (
    id            BIGSERIAL PRIMARY KEY,
    user_id       BIGINT NOT NULL,
    name          VARCHAR(255) NOT NULL,
    is_active     BOOLEAN NOT NULL DEFAULT TRUE,
    is_favorite   BOOLEAN NOT NULL DEFAULT FALSE,
    color_hex     VARCHAR(7),
    display_order INTEGER NOT NULL DEFAULT 0,
    created_at    TIMESTAMP NOT NULL DEFAULT now(),
    updated_at    TIMESTAMP NOT NULL DEFAULT now()
);

COMMENT ON TABLE tools_todolist.todolist IS 'Liste de tâches appartenant à un utilisateur';

COMMENT ON COLUMN tools_todolist.todolist.id IS 'Identifiant technique unique de la todo list';
COMMENT ON COLUMN tools_todolist.todolist.user_id IS 'Identifiant de l’utilisateur propriétaire de la liste';
COMMENT ON COLUMN tools_todolist.todolist.name IS 'Nom de la liste de tâches';
COMMENT ON COLUMN tools_todolist.todolist.is_active IS 'Indique si la liste est active ou archivée';
COMMENT ON COLUMN tools_todolist.todolist.is_favorite IS 'Indique si la liste est marquée comme favorite';
COMMENT ON COLUMN tools_todolist.todolist.color_hex IS 'Couleur personnalisée de la liste au format hexadécimal (#RRGGBB)';
COMMENT ON COLUMN tools_todolist.todolist.display_order IS 'Ordre d’affichage de la liste pour l’utilisateur';
COMMENT ON COLUMN tools_todolist.todolist.created_at IS 'Date de création de la liste';
COMMENT ON COLUMN tools_todolist.todolist.updated_at IS 'Date de dernière modification de la liste';

-- Table todo
CREATE TABLE tools_todolist.todo (
    id            BIGSERIAL PRIMARY KEY,
    todolist_id   BIGINT NOT NULL,
    name          VARCHAR(255) NOT NULL,
    description   TEXT,
    is_completed  BOOLEAN NOT NULL DEFAULT FALSE,
    priority      tools_todolist.todo_priority NOT NULL DEFAULT 'NORMAL',
    display_order INTEGER NOT NULL DEFAULT 0,
    created_at    TIMESTAMP NOT NULL DEFAULT now(),
    updated_at    TIMESTAMP NOT NULL DEFAULT now(),

    CONSTRAINT fk_todo_todolist
        FOREIGN KEY (todolist_id)
        REFERENCES tools_todolist.todolist (id)
        ON DELETE CASCADE
);

COMMENT ON TABLE tools_todolist.todo IS 'Tâche appartenant à une todo list';

COMMENT ON COLUMN tools_todolist.todo.id IS 'Identifiant technique unique de la tâche';
COMMENT ON COLUMN tools_todolist.todo.todolist_id IS 'Identifiant de la todo list parente';
COMMENT ON COLUMN tools_todolist.todo.name IS 'Nom de la tâche';
COMMENT ON COLUMN tools_todolist.todo.description IS 'Description détaillée de la tâche';
COMMENT ON COLUMN tools_todolist.todo.is_completed IS 'Indique si la tâche est terminée';
COMMENT ON COLUMN tools_todolist.todo.priority IS 'Priorité de la tâche (NORMAL, HAUT, URGENT)';
COMMENT ON COLUMN tools_todolist.todo.display_order IS 'Ordre d’affichage de la tâche dans la liste';
COMMENT ON COLUMN tools_todolist.todo.created_at IS 'Date de création de la tâche';
COMMENT ON COLUMN tools_todolist.todo.updated_at IS 'Date de dernière modification de la tâche';

INSERT INTO tools_core.module (
    code,
    name,
    description,
    is_active
)
VALUES (
    'TOOLS_TODOLIST',
    'TodoList',
    'Gestion de listes de tâches personnelles avec priorisation et organisation.',
    TRUE
);