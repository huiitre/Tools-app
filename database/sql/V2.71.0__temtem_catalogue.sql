-- Aligne le schéma tools_temtem sur les données produites par l'extracteur
-- (tools_assets/tools_temtem/*.json). V2.24.0 avait été écrite avant ces fichiers : elle couvre
-- quatre tables sur huit, et plusieurs de ses colonnes ne correspondent plus.
--
-- Les quatre tables existantes sont vides en développement, en QA et en production : les
-- modifications ci-dessous n'ont aucune reprise de données à faire, et les colonnes ajoutées
-- peuvent être NOT NULL d'emblée.
--
-- Les URL d'images sont à construire par la synchronisation depuis AssetsBaseUrl et le nom de
-- fichier, jamais recopiées du champ « image » des JSON : celui-ci porte le chemin du site
-- source (/img/temtemdex/...), qui ne désigne rien chez nous.

-- ---------------------------------------------------------------------------
-- Référentiels : catégorie et priorité des techniques
-- ---------------------------------------------------------------------------
-- Les deux étaient des valeurs libres dans technique ; l'extracteur les publie désormais comme
-- des tables à part entière, avec leur libellé français et leur icône.
CREATE TABLE tools_temtem.category (
    code VARCHAR(20) PRIMARY KEY,
    label VARCHAR(50) NOT NULL,
    image_url TEXT
);

COMMENT ON TABLE tools_temtem.category IS 'Catégories de technique : PHYSICAL, SPECIAL, STATUS';
COMMENT ON COLUMN tools_temtem.category.label IS 'Libellé français (Physique, Spéciale, État)';
COMMENT ON COLUMN tools_temtem.category.image_url IS 'URL de l''icône, construite depuis le champ « filename » (tools_temtem/images/categories/<filename>.png) — il ne se déduit pas du code : STATUS donne « statut »';

CREATE TABLE tools_temtem.priority (
    priority_order INT PRIMARY KEY,
    label VARCHAR(50) NOT NULL,
    image_url TEXT
);

COMMENT ON TABLE tools_temtem.priority IS 'Vitesse d''exécution d''une technique, du plus lent au plus rapide';
COMMENT ON COLUMN tools_temtem.priority.priority_order IS '0 Très lent, 1 Lente, 2 Normal, 3 Rapide, 4 Très rapide, 5 Ultra';
COMMENT ON COLUMN tools_temtem.priority.image_url IS 'URL de l''icône (tools_temtem/images/priorities/<filename>.png). Attention aux noms de la source : « hight » et « veryhight »';

-- ---------------------------------------------------------------------------
-- type : le slug identifie la ligne dans la source et nomme son image
-- ---------------------------------------------------------------------------
ALTER TABLE tools_temtem.type
    ADD COLUMN slug VARCHAR(100) NOT NULL,
    ADD COLUMN image_url TEXT,
    ADD CONSTRAINT uq_temtem_type_slug UNIQUE (slug);

COMMENT ON COLUMN tools_temtem.type.slug IS 'Identifiant textuel stable (ex. « electricite »), unique ; nomme le fichier image';
COMMENT ON COLUMN tools_temtem.type.image_url IS 'URL de l''icône (tools_temtem/images/types/<slug>.png)';

-- ---------------------------------------------------------------------------
-- temtem : slug, image et les sept statistiques de base
-- ---------------------------------------------------------------------------
ALTER TABLE tools_temtem.temtem
    ADD COLUMN slug VARCHAR(100) NOT NULL,
    ADD COLUMN image_url TEXT,
    ADD COLUMN hp INT NOT NULL,
    ADD COLUMN stamina INT NOT NULL,
    ADD COLUMN speed INT NOT NULL,
    ADD COLUMN attack INT NOT NULL,
    ADD COLUMN defense INT NOT NULL,
    ADD COLUMN special_attack INT NOT NULL,
    ADD COLUMN special_defense INT NOT NULL,
    ADD CONSTRAINT uq_temtem_slug UNIQUE (slug);

COMMENT ON COLUMN tools_temtem.temtem.slug IS 'Identifiant textuel stable (ex. « mimit »), unique ; nomme le fichier image';
COMMENT ON COLUMN tools_temtem.temtem.image_url IS 'URL du portrait (tools_temtem/images/temtem/<slug>.png)';
COMMENT ON COLUMN tools_temtem.temtem.hp IS 'Statistique de base, objet « stats » du JSON';

-- ---------------------------------------------------------------------------
-- technique
-- ---------------------------------------------------------------------------
ALTER TABLE tools_temtem.technique
    ADD COLUMN slug VARCHAR(100) NOT NULL,
    ADD COLUMN effect TEXT,
    ADD COLUMN stamina INT,
    ADD COLUMN category_code VARCHAR(20) NOT NULL REFERENCES tools_temtem.category(code),
    ADD COLUMN priority_order INT NOT NULL REFERENCES tools_temtem.priority(priority_order),
    ADD CONSTRAINT uq_temtem_technique_slug UNIQUE (slug);

-- 100 techniques sur 317 n'infligent aucun dégât : ce sont les techniques de statut. NULL et 0
-- ne veulent pas dire la même chose, et le DEFAULT 0 aurait transformé les unes en les autres.
ALTER TABLE tools_temtem.technique
    ALTER COLUMN damage DROP DEFAULT,
    ALTER COLUMN damage DROP NOT NULL;

-- Le champ que V2.24.0 appelait « nombre de cibles » n'en était pas un : l'extracteur le publie
-- désormais sous le nom « chargeTurns ». Le vrai ciblage est arrivé à part, dans sa propre table.
ALTER TABLE tools_temtem.technique
    RENAME COLUMN number_of_targets TO charge_turns;

ALTER TABLE tools_temtem.technique
    ALTER COLUMN charge_turns DROP NOT NULL,
    -- Borne basse seulement : rien ne garantit qu'une extension du jeu n'ira pas au-delà des
    -- 5 tours observés aujourd'hui.
    ADD CONSTRAINT ck_temtem_technique_charge_turns CHECK (charge_turns > 0);

COMMENT ON COLUMN tools_temtem.technique.slug IS 'Identifiant textuel stable (ex. « aboiement »), unique';
COMMENT ON COLUMN tools_temtem.technique.effect IS 'Description française de l''effet';
COMMENT ON COLUMN tools_temtem.technique.stamina IS 'Coût en endurance ; NULL sur 5 techniques que la source ne renseigne pas';
COMMENT ON COLUMN tools_temtem.technique.damage IS 'Dégâts de base ; NULL pour une technique sans dégâts, à ne pas confondre avec 0';
COMMENT ON COLUMN tools_temtem.technique.charge_turns IS 'Nombre de tours de chargement avant que la technique puisse être utilisée. 1 à 5 ; NULL pour les 150 techniques utilisables immédiatement. Ce n''est pas un temps de recharge après usage : le champ portait le nom trompeur « targets » avant l''extract du 30/08/2026';

-- ---------------------------------------------------------------------------
-- technique_target : le ciblage, arrivé avec l'extract du 30/08/2026
-- ---------------------------------------------------------------------------
-- Une technique vise une ou deux cibles possibles — jamais zéro, jamais plus de deux dans les
-- données actuelles. D'où une table de liaison plutôt qu'une colonne.
--
-- Le champ « mandatory » de la source n'est volontairement pas repris : il doit disparaître.
CREATE TABLE tools_temtem.technique_target (
    technique_id INT NOT NULL REFERENCES tools_temtem.technique(id) ON DELETE CASCADE,
    target VARCHAR(20) NOT NULL,

    PRIMARY KEY (technique_id, target),
    CONSTRAINT ck_temtem_technique_target CHECK (target IN (
        'SELF', 'ALLY', 'SELF_AND_ALLY', 'SINGLE_OPPONENT',
        'OWN_FIELD', 'OPPONENT_FIELD', 'ANY_ON_FIELD', 'EVERYONE'
    ))
);

COMMENT ON TABLE tools_temtem.technique_target IS 'Cibles possibles de chaque technique';
COMMENT ON COLUMN tools_temtem.technique_target.target IS 'SELF, ALLY, SELF_AND_ALLY, SINGLE_OPPONENT, OWN_FIELD, OPPONENT_FIELD, ANY_ON_FIELD, EVERYONE';

-- ---------------------------------------------------------------------------
-- temtem_technique : la clé primaire est contredite par les données
-- ---------------------------------------------------------------------------
-- Un même couple (temtem, technique) apparaît deux fois quand la technique s'apprend par deux
-- moyens : Tateru (54) apprend la technique 188 au niveau 16 ET par entraînement. La source fait
-- donc partie de l'identité de la ligne.
ALTER TABLE tools_temtem.temtem_technique
    ADD COLUMN source VARCHAR(20) NOT NULL,
    ADD COLUMN level INT;

-- Colonne ajoutée d'abord, clé refaite ensuite : deux instructions pour ne pas dépendre de
-- l'ordre dans lequel PostgreSQL traite les sous-commandes d'un même ALTER TABLE.
ALTER TABLE tools_temtem.temtem_technique
    DROP CONSTRAINT temtem_technique_pkey,
    ADD CONSTRAINT pk_temtem_technique PRIMARY KEY (temtem_id, technique_id, source);

ALTER TABLE tools_temtem.temtem_technique
    ADD CONSTRAINT ck_temtem_technique_source
        CHECK (source IN ('LEVEL', 'BREEDING', 'TRAINING')),
    -- Le niveau n'a de sens que pour un apprentissage par montée de niveau : il est toujours
    -- renseigné dans ce cas, et toujours absent dans les deux autres.
    ADD CONSTRAINT ck_temtem_technique_level
        CHECK ((source = 'LEVEL') = (level IS NOT NULL));

COMMENT ON COLUMN tools_temtem.temtem_technique.source IS 'Moyen d''apprentissage : LEVEL, BREEDING ou TRAINING';
COMMENT ON COLUMN tools_temtem.temtem_technique.level IS 'Niveau d''apprentissage, 1 à 100 ; NULL sauf pour source = LEVEL';

-- ---------------------------------------------------------------------------
-- trait : table absente du schéma
-- ---------------------------------------------------------------------------
CREATE TABLE tools_temtem.trait (
    id INT PRIMARY KEY,
    slug VARCHAR(100) NOT NULL,
    name VARCHAR(100) NOT NULL,
    effect TEXT,

    CONSTRAINT uq_temtem_trait_slug UNIQUE (slug)
);

COMMENT ON TABLE tools_temtem.trait IS 'Traits passifs des Temtem';
COMMENT ON COLUMN tools_temtem.trait.name IS 'Nom français';
COMMENT ON COLUMN tools_temtem.trait.effect IS 'Description française de l''effet';

-- ---------------------------------------------------------------------------
-- temtem_trait : table absente du schéma
-- ---------------------------------------------------------------------------
CREATE TABLE tools_temtem.temtem_trait (
    temtem_id INT NOT NULL REFERENCES tools_temtem.temtem(id) ON DELETE CASCADE,
    trait_id INT NOT NULL REFERENCES tools_temtem.trait(id) ON DELETE CASCADE,

    PRIMARY KEY (temtem_id, trait_id)
);

COMMENT ON TABLE tools_temtem.temtem_trait IS 'Traits possibles de chaque Temtem (deux en général)';

-- ---------------------------------------------------------------------------
-- type_matrix : table absente du schéma
-- ---------------------------------------------------------------------------
-- 144 lignes : la matrice est pleine, 12 types attaquants × 12 défenseurs, y compris les
-- multiplicateurs neutres. Aucune ligne n'est donc à déduire par absence.
CREATE TABLE tools_temtem.type_matrix (
    attacker_type_id INT NOT NULL REFERENCES tools_temtem.type(id) ON DELETE CASCADE,
    defender_type_id INT NOT NULL REFERENCES tools_temtem.type(id) ON DELETE CASCADE,
    multiplier NUMERIC(2, 1) NOT NULL,

    PRIMARY KEY (attacker_type_id, defender_type_id),
    CONSTRAINT ck_temtem_type_matrix_multiplier CHECK (multiplier IN (0.5, 1, 2))
);

COMMENT ON TABLE tools_temtem.type_matrix IS 'Table d''efficacité des types, attaquant contre défenseur';
COMMENT ON COLUMN tools_temtem.type_matrix.multiplier IS 'Multiplicateur de dégâts : 0.5, 1 ou 2';

-- ---------------------------------------------------------------------------
-- Index des nouvelles clés étrangères — PostgreSQL ne les crée pas tout seul
-- ---------------------------------------------------------------------------
CREATE INDEX idx_temtem_technique_category ON tools_temtem.technique(category_code);
CREATE INDEX idx_temtem_technique_priority ON tools_temtem.technique(priority_order);
CREATE INDEX idx_temtem_technique_target_technique ON tools_temtem.technique_target(technique_id);
CREATE INDEX idx_temtem_trait_temtem ON tools_temtem.temtem_trait(temtem_id);
CREATE INDEX idx_temtem_trait_trait ON tools_temtem.temtem_trait(trait_id);
CREATE INDEX idx_temtem_type_matrix_defender ON tools_temtem.type_matrix(defender_type_id);
