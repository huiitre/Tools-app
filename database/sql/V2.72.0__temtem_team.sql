-- Équipes de combat Temtem, propres à chaque utilisateur.
--
-- Le catalogue (V2.71.0) décrit le jeu ; ces trois tables décrivent ce que l'utilisateur en fait.
-- Elles ne portent donc aucune donnée du jeu : uniquement des références vers lui.

-- ---------------------------------------------------------------------------
-- team
-- ---------------------------------------------------------------------------
CREATE TABLE tools_temtem.team (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL,
    name VARCHAR(100) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT now(),
    updated_at TIMESTAMP NOT NULL DEFAULT now(),

    CONSTRAINT fk_temtem_team_user
        FOREIGN KEY (user_id) REFERENCES tools_core.users (id) ON DELETE CASCADE,

    -- Deux équipes homonymes chez le même utilisateur ne seraient pas distinguables dans la
    -- popup « ajouter à une équipe ».
    CONSTRAINT uq_temtem_team_user_name UNIQUE (user_id, name)
);

COMMENT ON TABLE tools_temtem.team IS 'Équipes de combat, une par ligne, propres à un utilisateur';
COMMENT ON COLUMN tools_temtem.team.name IS 'Nom donné par l''utilisateur, unique chez lui';

CREATE INDEX idx_temtem_team_user ON tools_temtem.team(user_id);

-- ---------------------------------------------------------------------------
-- team_member : un Temtem à une place donnée dans une équipe
-- ---------------------------------------------------------------------------
-- La place borne l'équipe à six sans qu'aucun code n'ait à compter les lignes, et donne un ordre
-- d'affichage stable. Le même Temtem peut occuper deux places : le jeu l'autorise, et l'interdire
-- ici reviendrait à décider à la place du joueur.
CREATE TABLE tools_temtem.team_member (
    id BIGSERIAL PRIMARY KEY,
    team_id BIGINT NOT NULL,
    temtem_id INT NOT NULL,
    slot INT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT now(),

    CONSTRAINT fk_temtem_team_member_team
        FOREIGN KEY (team_id) REFERENCES tools_temtem.team (id) ON DELETE CASCADE,

    -- Un Temtem retiré du jeu par une synchronisation emporte les lignes qui le désignent :
    -- laisser la référence bloquerait la synchronisation pour préserver une équipe devenue
    -- injouable.
    CONSTRAINT fk_temtem_team_member_temtem
        FOREIGN KEY (temtem_id) REFERENCES tools_temtem.temtem (id) ON DELETE CASCADE,

    CONSTRAINT uq_temtem_team_member_slot UNIQUE (team_id, slot),
    CONSTRAINT ck_temtem_team_member_slot CHECK (slot BETWEEN 1 AND 6)
);

COMMENT ON TABLE tools_temtem.team_member IS 'Composition d''une équipe : un Temtem par place';
COMMENT ON COLUMN tools_temtem.team_member.slot IS 'Place dans l''équipe, 1 à 6 ; la contrainte d''unicité borne l''équipe à six membres';

CREATE INDEX idx_temtem_team_member_team ON tools_temtem.team_member(team_id);
CREATE INDEX idx_temtem_team_member_temtem ON tools_temtem.team_member(temtem_id);

-- ---------------------------------------------------------------------------
-- team_member_technique : les techniques retenues pour ce membre
-- ---------------------------------------------------------------------------
-- Quatre au maximum, mais aucune contrainte SQL ne sait compter des lignes : c'est le use case
-- qui refuse la cinquième. Il lui revient aussi de vérifier que le Temtem apprend réellement la
-- technique — tools_temtem.temtem_technique le dit, une clé étrangère ne le dirait pas.
CREATE TABLE tools_temtem.team_member_technique (
    team_member_id BIGINT NOT NULL,
    technique_id INT NOT NULL,

    PRIMARY KEY (team_member_id, technique_id),

    CONSTRAINT fk_temtem_team_member_technique_member
        FOREIGN KEY (team_member_id) REFERENCES tools_temtem.team_member (id) ON DELETE CASCADE,

    CONSTRAINT fk_temtem_team_member_technique_technique
        FOREIGN KEY (technique_id) REFERENCES tools_temtem.technique (id) ON DELETE CASCADE
);

COMMENT ON TABLE tools_temtem.team_member_technique IS 'Techniques retenues pour un membre d''équipe, quatre au maximum (borne appliquée par le use case)';

CREATE INDEX idx_temtem_team_member_technique_technique ON tools_temtem.team_member_technique(technique_id);
