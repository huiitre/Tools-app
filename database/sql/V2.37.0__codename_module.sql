-- =========================
-- SCHEMA : tools_codename
-- =========================
CREATE SCHEMA IF NOT EXISTS tools_codename;

COMMENT ON SCHEMA tools_codename IS 'Schéma du module Codename : jeu de plateau multijoueur en temps réel';

-- =========================
-- MODULE & RÔLE
-- =========================
INSERT INTO tools_core.module (code, name, description, is_active)
VALUES ('TOOLS_CODENAME', 'Codename', 'Jeu de plateau Codename multijoueur en temps réel', TRUE);

-- =========================
-- TABLE: codename_words
-- =========================
CREATE TABLE tools_codename.codename_words (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    content     VARCHAR(50) NOT NULL UNIQUE,
    validated   BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE  tools_codename.codename_words              IS 'Dictionnaire des mots utilisables dans une partie Codename';
COMMENT ON COLUMN tools_codename.codename_words.id           IS 'Identifiant unique du mot';
COMMENT ON COLUMN tools_codename.codename_words.content      IS 'Mot affiché sur la carte';
COMMENT ON COLUMN tools_codename.codename_words.validated    IS 'FALSE = mot désactivé temporairement sans suppression';
COMMENT ON COLUMN tools_codename.codename_words.created_at   IS 'Date d''ajout du mot';

-- =========================
-- TABLE: codename_tags
-- =========================
CREATE TABLE tools_codename.codename_tags (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    label       VARCHAR(30) NOT NULL UNIQUE
);

COMMENT ON TABLE  tools_codename.codename_tags        IS 'Catégories/thèmes associables aux mots du dictionnaire';
COMMENT ON COLUMN tools_codename.codename_tags.id     IS 'Identifiant unique du tag';
COMMENT ON COLUMN tools_codename.codename_tags.label  IS 'Libellé du tag (ex: Anime, Jeux Vidéo)';

-- =========================
-- TABLE: codename_words_tags (M:N)
-- =========================
CREATE TABLE tools_codename.codename_words_tags (
    word_id UUID NOT NULL REFERENCES tools_codename.codename_words(id) ON DELETE CASCADE,
    tag_id  UUID NOT NULL REFERENCES tools_codename.codename_tags(id)  ON DELETE CASCADE,
    PRIMARY KEY (word_id, tag_id)
);

COMMENT ON TABLE tools_codename.codename_words_tags IS 'Association mots ↔ tags (many-to-many)';

-- =========================
-- TABLE: codename_word_proposals
-- =========================
CREATE TABLE tools_codename.codename_word_proposals (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    content         VARCHAR(50) NOT NULL,
    suggested_tags  JSONB,
    proposed_by     BIGINT      REFERENCES tools_core.users(id) ON DELETE SET NULL,
    status          VARCHAR(20) NOT NULL DEFAULT 'PENDING',  -- PENDING | VALIDATED | REJECTED
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT chk_proposal_status CHECK (status IN ('PENDING', 'VALIDATED', 'REJECTED'))
);

COMMENT ON TABLE  tools_codename.codename_word_proposals               IS 'Propositions de nouveaux mots soumises par les utilisateurs';
COMMENT ON COLUMN tools_codename.codename_word_proposals.suggested_tags IS 'Tags suggérés par le proposant (stockés en JSON avant validation)';
COMMENT ON COLUMN tools_codename.codename_word_proposals.proposed_by   IS 'Utilisateur auteur de la proposition (NULL si invité)';
COMMENT ON COLUMN tools_codename.codename_word_proposals.status        IS 'Statut de modération : PENDING, VALIDATED, REJECTED';

-- =========================
-- TABLE: codename_sessions
-- =========================
CREATE TABLE tools_codename.codename_sessions (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_id    BIGINT      REFERENCES tools_core.users(id) ON DELETE SET NULL,
    status      VARCHAR(20) NOT NULL DEFAULT 'LOBBY',  -- LOBBY | IN_PROGRESS | FINISHED
    state_json  JSONB,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT chk_session_status CHECK (status IN ('LOBBY', 'IN_PROGRESS', 'FINISHED'))
);

COMMENT ON TABLE  tools_codename.codename_sessions            IS 'Sessions de jeu Codename (une session = une partie)';
COMMENT ON COLUMN tools_codename.codename_sessions.owner_id  IS 'Créateur de la session (NULL si créée par un invité non lié)';
COMMENT ON COLUMN tools_codename.codename_sessions.status    IS 'État de la partie : LOBBY, IN_PROGRESS, FINISHED';
COMMENT ON COLUMN tools_codename.codename_sessions.state_json IS 'État complet de la grille : board (25 cartes), scores, tour actuel, indice en cours';

-- =========================
-- TABLE: codename_session_players
-- =========================
CREATE TABLE tools_codename.codename_session_players (
    id          UUID        PRIMARY KEY,  -- player_session_id stocké dans le localStorage du client
    session_id  UUID        NOT NULL REFERENCES tools_codename.codename_sessions(id) ON DELETE CASCADE,
    user_id     BIGINT      REFERENCES tools_core.users(id) ON DELETE SET NULL,
    nickname    VARCHAR(50) NOT NULL,
    team        VARCHAR(10),              -- RED | BLUE | NULL (spectateur)
    role        VARCHAR(20) NOT NULL DEFAULT 'SPECTATOR',  -- SPYMASTER | OPERATIVE | SPECTATOR
    is_ready    BOOLEAN     NOT NULL DEFAULT FALSE,
    joined_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT chk_player_team CHECK (team IN ('RED', 'BLUE') OR team IS NULL),
    CONSTRAINT chk_player_role CHECK (role IN ('SPYMASTER', 'OPERATIVE', 'SPECTATOR'))
);

COMMENT ON TABLE  tools_codename.codename_session_players           IS 'Joueurs présents dans une session (incluant spectateurs et invités)';
COMMENT ON COLUMN tools_codename.codename_session_players.id        IS 'UUID généré côté client et persisté dans le localStorage (permet la reconnexion)';
COMMENT ON COLUMN tools_codename.codename_session_players.user_id   IS 'Compte Tools lié, NULL pour les invités';
COMMENT ON COLUMN tools_codename.codename_session_players.team      IS 'Équipe du joueur : RED, BLUE, ou NULL si spectateur';
COMMENT ON COLUMN tools_codename.codename_session_players.role      IS 'Rôle dans la partie : SPYMASTER, OPERATIVE, SPECTATOR';
COMMENT ON COLUMN tools_codename.codename_session_players.is_ready  IS 'Indique si le joueur a validé son prêt pour le démarrage';

-- =========================
-- TABLE: codename_events
-- =========================
CREATE TABLE tools_codename.codename_events (
    id                  UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    session_id          UUID        NOT NULL REFERENCES tools_codename.codename_sessions(id) ON DELETE CASCADE,
    player_session_id   UUID        REFERENCES tools_codename.codename_session_players(id) ON DELETE SET NULL,
    type                VARCHAR(20) NOT NULL,  -- CHAT_MSG | CARD_CLICK | CLUE_GIVEN | PLAYER_JOIN | PLAYER_READY | TEAM_CHANGE | ROLE_CHANGE | GAME_START | GAME_END
    content             TEXT,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT chk_event_type CHECK (type IN ('CHAT_MSG', 'CARD_CLICK', 'CLUE_GIVEN', 'PLAYER_JOIN', 'PLAYER_READY', 'TEAM_CHANGE', 'ROLE_CHANGE', 'GAME_START', 'GAME_END'))
);

COMMENT ON TABLE  tools_codename.codename_events                    IS 'Journal chronologique de tous les événements d''une session (chat, coups joués, transitions)';
COMMENT ON COLUMN tools_codename.codename_events.type              IS 'Type d''événement : CHAT_MSG, CARD_CLICK, CLUE_GIVEN, PLAYER_JOIN, PLAYER_READY, TEAM_CHANGE, ROLE_CHANGE, GAME_START, GAME_END';
COMMENT ON COLUMN tools_codename.codename_events.content           IS 'Contenu textuel (message chat) ou données JSON sérialisées (indice, index carte…)';
COMMENT ON COLUMN tools_codename.codename_events.player_session_id IS 'Joueur ayant déclenché l''événement (NULL pour les événements système)';

-- =========================
-- TABLE: codename_history
-- =========================
CREATE TABLE tools_codename.codename_history (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     BIGINT      NOT NULL REFERENCES tools_core.users(id) ON DELETE CASCADE,
    session_id  UUID        NOT NULL REFERENCES tools_codename.codename_sessions(id) ON DELETE CASCADE,
    team        VARCHAR(10) NOT NULL,
    role        VARCHAR(20) NOT NULL,
    result      VARCHAR(10) NOT NULL,  -- WIN | LOSS
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT chk_history_team   CHECK (team   IN ('RED', 'BLUE')),
    CONSTRAINT chk_history_role   CHECK (role   IN ('SPYMASTER', 'OPERATIVE')),
    CONSTRAINT chk_history_result CHECK (result IN ('WIN', 'LOSS'))
);

COMMENT ON TABLE  tools_codename.codename_history          IS 'Historique des parties jouées par les utilisateurs connectés';
COMMENT ON COLUMN tools_codename.codename_history.user_id  IS 'Utilisateur concerné (invités exclus)';
COMMENT ON COLUMN tools_codename.codename_history.result   IS 'Résultat de la partie : WIN ou LOSS';

-- =========================
-- INDEX
-- =========================
CREATE INDEX idx_codename_words_validated        ON tools_codename.codename_words(validated);
CREATE INDEX idx_codename_words_tags_tag_id      ON tools_codename.codename_words_tags(tag_id);
CREATE INDEX idx_codename_proposals_status       ON tools_codename.codename_word_proposals(status);
CREATE INDEX idx_codename_sessions_status        ON tools_codename.codename_sessions(status);
CREATE INDEX idx_codename_sessions_created_at    ON tools_codename.codename_sessions(created_at DESC);
CREATE INDEX idx_codename_players_session_id     ON tools_codename.codename_session_players(session_id);
CREATE INDEX idx_codename_players_user_id        ON tools_codename.codename_session_players(user_id);
CREATE INDEX idx_codename_events_session_id      ON tools_codename.codename_events(session_id);
CREATE INDEX idx_codename_events_created_at      ON tools_codename.codename_events(created_at);
CREATE INDEX idx_codename_history_user_id        ON tools_codename.codename_history(user_id);
CREATE INDEX idx_codename_history_session_id     ON tools_codename.codename_history(session_id);