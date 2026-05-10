-- Création des tables pour le système de notifications global

CREATE TABLE tools_core.notifications (
    id              BIGSERIAL PRIMARY KEY,
    title           VARCHAR(255) NOT NULL,
    body            TEXT NOT NULL,
    type            VARCHAR(20) NOT NULL DEFAULT 'INFO', -- INFO, SUCCESS, WARNING, ERROR
    
    -- Pour historique/audit : on garde les critères originaux
    target_user_id   BIGINT,
    target_role_id   BIGINT,
    target_module_id BIGINT,
    
    metadata        JSONB,
    created_at      TIMESTAMP NOT NULL DEFAULT now()
);

COMMENT ON TABLE tools_core.notifications IS 'Stockage des messages sources';

CREATE TABLE tools_core.user_notifications (
    user_id         BIGINT NOT NULL REFERENCES tools_core.users(id) ON DELETE CASCADE,
    notification_id BIGINT NOT NULL REFERENCES tools_core.notifications(id) ON DELETE CASCADE,
    is_read         BOOLEAN NOT NULL DEFAULT FALSE,
    read_at         TIMESTAMP,
    PRIMARY KEY (user_id, notification_id)
);

COMMENT ON TABLE tools_core.user_notifications IS 'État de lecture par utilisateur (1 ligne par destinataire). Suppression = DELETE physique.';

-- Index
CREATE INDEX idx_notifications_created_at ON tools_core.notifications(created_at DESC);
CREATE INDEX idx_user_notifications_query ON tools_core.user_notifications(user_id, is_read);
