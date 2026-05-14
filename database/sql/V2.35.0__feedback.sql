-- Création de la table feedback

CREATE TABLE tools_core.feedbacks (
    id          BIGSERIAL PRIMARY KEY,
    user_id     BIGINT NOT NULL REFERENCES tools_core.users(id) ON DELETE CASCADE,
    message     TEXT NOT NULL,
    created_at  TIMESTAMP NOT NULL DEFAULT now()
);

COMMENT ON TABLE tools_core.feedbacks IS 'Stockage des retours utilisateurs et rapports de bugs';

-- Index pour la performance des requêtes d'administration
CREATE INDEX idx_feedbacks_user_id ON tools_core.feedbacks(user_id);
CREATE INDEX idx_feedbacks_created_at ON tools_core.feedbacks(created_at DESC);

ALTER TABLE tools_core.feedbacks ADD COLUMN is_read BOOLEAN NOT NULL DEFAULT FALSE;
