-- =========================================================================
-- MODULE PALWORLD : modèle de données
-- =========================================================================
-- Deux familles de données, dans le même schéma tools_palworld :
--
-- 1. Données de référence (statiques) : Pals, compétences, aptitudes de
--    travail, éléments. Alimentées aujourd'hui par un scraper de paldb.cc,
--    mais la clé métier retenue (pal.tribe) est une valeur native du jeu
--    (pas un identifiant du site source) pour rester stable si on recoupe
--    ou remplace la source plus tard. Chaque import de source externe est
--    tracé dans pal_source / skill_source (payload brut + provenance) afin
--    de ne jamais perdre l'information d'origine, même si les colonnes
--    "promues" sur pal/skill sont ensuite fusionnées/arbitrées côté code.
--
-- 2. Données d'activité serveur (dynamiques) : guildes, bases, et l'état
--    des Pals posés en base, ingérées depuis les snapshots JSON produits
--    toutes les 5 minutes par tools_palworld_server_data_extractor.
--    pal_instance porte l'état courant (upsert à chaque ingestion, lecture
--    bon marché pour le dashboard live). pal_instance_snapshot conserve
--    l'historique complet (une ligne par instance par snapshot ingéré) :
--    full_stomach varie en continu, donc un historique "on change" n'aurait
--    quasiment rien économisé par rapport à un snapshot complet.
-- =========================================================================

-- =========================
-- REFERENCE : element
-- =========================
CREATE TABLE tools_palworld.element (
    id            BIGSERIAL PRIMARY KEY,
    external_code VARCHAR(4)   NOT NULL UNIQUE,
    name          VARCHAR(50)  NOT NULL UNIQUE,
    icon_url      TEXT
);

COMMENT ON TABLE  tools_palworld.element               IS 'Table de référence des éléments (Feu, Eau, Électrique...)';
COMMENT ON COLUMN tools_palworld.element.external_code  IS 'Code source d''origine (ex: "01" chez paldb.cc), conservé pour traçabilité';
COMMENT ON COLUMN tools_palworld.element.name           IS 'Nom de l''élément';
COMMENT ON COLUMN tools_palworld.element.icon_url       IS 'URL de l''icône (CDN)';

-- =========================
-- REFERENCE : work_suitability
-- =========================
CREATE TABLE tools_palworld.work_suitability (
    id            BIGSERIAL PRIMARY KEY,
    external_code VARCHAR(4)   NOT NULL UNIQUE,
    slug          VARCHAR(50)  NOT NULL UNIQUE,
    name          VARCHAR(100) NOT NULL,
    icon_url      TEXT
);

COMMENT ON TABLE  tools_palworld.work_suitability               IS 'Table de référence des aptitudes de travail (Arrosage, Transport, Extraction...)';
COMMENT ON COLUMN tools_palworld.work_suitability.external_code IS 'Code source d''origine (ex: "07" chez paldb.cc)';
COMMENT ON COLUMN tools_palworld.work_suitability.slug          IS 'Slug technique stable (ex: Mining), utilisé pour recouper avec workable_type côté serveur';
COMMENT ON COLUMN tools_palworld.work_suitability.name           IS 'Nom affiché (FR)';
COMMENT ON COLUMN tools_palworld.work_suitability.icon_url       IS 'URL de l''icône (CDN)';

-- =========================
-- REFERENCE : item (catalogue léger, alimenté par les drops pour l'instant)
-- =========================
CREATE TABLE tools_palworld.item (
    id       BIGSERIAL PRIMARY KEY,
    slug     VARCHAR(150) NOT NULL UNIQUE,
    name     VARCHAR(255) NOT NULL,
    icon_url TEXT
);

COMMENT ON TABLE  tools_palworld.item      IS 'Catalogue d''items (matériaux, nourriture...), pour l''instant uniquement alimenté via les drops de Pals';
COMMENT ON COLUMN tools_palworld.item.slug IS 'Identifiant technique de l''item côté source (ex: Wool)';
COMMENT ON COLUMN tools_palworld.item.name IS 'Nom affiché (FR)';

-- =========================
-- REFERENCE : pal
-- =========================
CREATE TABLE tools_palworld.pal (
    id                       BIGSERIAL PRIMARY KEY,
    tribe                    VARCHAR(150) NOT NULL UNIQUE,
    paldex_index             INTEGER,
    paldex_suffix            VARCHAR(10),
    name                     VARCHAR(255) NOT NULL,
    image_url                TEXT,
    description              TEXT,
    size                     VARCHAR(10),
    rarity                   SMALLINT,
    base_hp                  INTEGER,
    base_attack              INTEGER,
    base_defense             INTEGER,
    base_work_speed          INTEGER,
    base_support             INTEGER,
    capture_rate_correct     NUMERIC(6,2),
    male_probability         NUMERIC(5,2),
    combi_rank               INTEGER,
    gold_coin                INTEGER,
    egg_type                 VARCHAR(100),
    best_work_suitability_id BIGINT REFERENCES tools_palworld.work_suitability(id),
    created_at               TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at               TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE  tools_palworld.pal                          IS 'Catalogue des Pals. Entité canonique, potentiellement alimentée par plusieurs sources (cf. pal_source)';
COMMENT ON COLUMN tools_palworld.pal.tribe                    IS 'Clé métier stable : valeur native du jeu (Tribe), fiable contrairement au champ "code" scrapé (faux sur ~5% des fiches paldb.cc)';
COMMENT ON COLUMN tools_palworld.pal.paldex_index             IS 'Numéro au Paldex';
COMMENT ON COLUMN tools_palworld.pal.paldex_suffix             IS 'Suffixe de variante Paldex (ex: "B" pour une variante élémentaire régionale)';
COMMENT ON COLUMN tools_palworld.pal.name                     IS 'Nom affiché (FR), issu de la source ayant le plus de confiance au moment de l''import';
COMMENT ON COLUMN tools_palworld.pal.image_url                IS 'URL du portrait (CDN)';
COMMENT ON COLUMN tools_palworld.pal.size                     IS 'Gabarit du Pal (XS, S, M, L, XL...)';
COMMENT ON COLUMN tools_palworld.pal.rarity                   IS 'Rareté brute du jeu';
COMMENT ON COLUMN tools_palworld.pal.base_hp                  IS 'PV de base';
COMMENT ON COLUMN tools_palworld.pal.base_attack              IS 'Attaque de base';
COMMENT ON COLUMN tools_palworld.pal.base_defense             IS 'Défense de base';
COMMENT ON COLUMN tools_palworld.pal.base_work_speed          IS 'Vitesse de travail de base';
COMMENT ON COLUMN tools_palworld.pal.base_support              IS 'Support de base';
COMMENT ON COLUMN tools_palworld.pal.capture_rate_correct     IS 'Taux de capture corrigé';
COMMENT ON COLUMN tools_palworld.pal.male_probability          IS 'Probabilité (%) d''obtenir un mâle';
COMMENT ON COLUMN tools_palworld.pal.combi_rank                IS 'Rang de combinaison (breeding)';
COMMENT ON COLUMN tools_palworld.pal.gold_coin                IS 'Pièces d''or données au K.O.';
COMMENT ON COLUMN tools_palworld.pal.egg_type                  IS 'Type d''œuf dont le Pal peut éclore';
COMMENT ON COLUMN tools_palworld.pal.best_work_suitability_id  IS 'Meilleure aptitude de travail du Pal (BestWorkSuitability)';

-- =========================
-- REFERENCE : skill
-- =========================
CREATE TABLE tools_palworld.skill (
    id            BIGSERIAL PRIMARY KEY,
    slug          VARCHAR(150) NOT NULL UNIQUE,
    category      VARCHAR(20)  NOT NULL,
    name          VARCHAR(255) NOT NULL,
    icon_url      TEXT,
    element_id    BIGINT REFERENCES tools_palworld.element(id),
    cooldown      NUMERIC(8,2),
    power         INTEGER,
    status_effect TEXT,
    description   TEXT,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE  tools_palworld.skill               IS 'Catalogue des compétences actives. Entité canonique, potentiellement alimentée par plusieurs sources (cf. skill_source)';
COMMENT ON COLUMN tools_palworld.skill.slug           IS 'Identifiant technique stable de la compétence côté source';
COMMENT ON COLUMN tools_palworld.skill.category       IS 'Catégorie brute de la source (ex: Active, Boss)';
COMMENT ON COLUMN tools_palworld.skill.cooldown       IS 'Temps de recharge (secondes)';
COMMENT ON COLUMN tools_palworld.skill.power          IS 'Puissance de la compétence';
COMMENT ON COLUMN tools_palworld.skill.status_effect  IS 'Effet de statut infligé (texte libre, ex: "Accumulation : Brûlure 100")';

-- =========================
-- PROVENANCE : pal_source / skill_source
-- =========================
CREATE TABLE tools_palworld.pal_source (
    id             BIGSERIAL PRIMARY KEY,
    pal_id         BIGINT       NOT NULL REFERENCES tools_palworld.pal(id) ON DELETE CASCADE,
    source_code    VARCHAR(50)  NOT NULL DEFAULT 'paldb_cc',
    external_slug  VARCHAR(150) NOT NULL,
    external_url   TEXT,
    raw_payload    JSONB        NOT NULL,
    fetched_at     TIMESTAMPTZ  NOT NULL,
    UNIQUE (pal_id, source_code)
);

COMMENT ON TABLE  tools_palworld.pal_source               IS 'Fidélité brute + provenance d''un Pal par source externe. Permet d''ajouter/recouper une autre source sans jamais perdre le payload d''origine';
COMMENT ON COLUMN tools_palworld.pal_source.source_code   IS 'Code de la source (ex: paldb_cc). Pas de table de référence dédiée tant qu''une seule source existe';
COMMENT ON COLUMN tools_palworld.pal_source.external_slug IS 'Identifiant/slug du Pal tel qu''utilisé par la source (ex: "slug" paldb.cc)';
COMMENT ON COLUMN tools_palworld.pal_source.raw_payload   IS 'Payload JSON complet tel que scrapé, non retravaillé';
COMMENT ON COLUMN tools_palworld.pal_source.fetched_at    IS 'Date de scraping côté source';

CREATE TABLE tools_palworld.skill_source (
    id             BIGSERIAL PRIMARY KEY,
    skill_id       BIGINT       NOT NULL REFERENCES tools_palworld.skill(id) ON DELETE CASCADE,
    source_code    VARCHAR(50)  NOT NULL DEFAULT 'paldb_cc',
    external_slug  VARCHAR(150) NOT NULL,
    external_url   TEXT,
    raw_payload    JSONB        NOT NULL,
    fetched_at     TIMESTAMPTZ  NOT NULL,
    UNIQUE (skill_id, source_code)
);

COMMENT ON TABLE tools_palworld.skill_source IS 'Fidélité brute + provenance d''une compétence par source externe (même logique que pal_source)';

-- =========================
-- JOIN : pal <-> element
-- =========================
CREATE TABLE tools_palworld.pal_element (
    pal_id     BIGINT   NOT NULL REFERENCES tools_palworld.pal(id) ON DELETE CASCADE,
    element_id BIGINT   NOT NULL REFERENCES tools_palworld.element(id),
    sort_order SMALLINT NOT NULL DEFAULT 0,
    PRIMARY KEY (pal_id, element_id)
);

COMMENT ON TABLE tools_palworld.pal_element IS 'Élément(s) d''un Pal (mono ou double type), sort_order = ordre d''affichage (primaire/secondaire)';

-- =========================
-- JOIN : pal <-> work_suitability
-- =========================
CREATE TABLE tools_palworld.pal_work_suitability (
    pal_id               BIGINT   NOT NULL REFERENCES tools_palworld.pal(id) ON DELETE CASCADE,
    work_suitability_id  BIGINT   NOT NULL REFERENCES tools_palworld.work_suitability(id),
    level                SMALLINT NOT NULL,
    PRIMARY KEY (pal_id, work_suitability_id)
);

COMMENT ON TABLE  tools_palworld.pal_work_suitability       IS 'Aptitudes de travail d''un Pal et leur niveau';
COMMENT ON COLUMN tools_palworld.pal_work_suitability.level IS 'Niveau de l''aptitude (1 à 4)';

-- =========================
-- JOIN : pal <-> skill (compétences actives)
-- =========================
CREATE TABLE tools_palworld.pal_active_skill (
    pal_id       BIGINT   NOT NULL REFERENCES tools_palworld.pal(id) ON DELETE CASCADE,
    skill_id     BIGINT   NOT NULL REFERENCES tools_palworld.skill(id),
    unlock_level SMALLINT NOT NULL,
    sort_order   SMALLINT NOT NULL DEFAULT 0,
    PRIMARY KEY (pal_id, skill_id, unlock_level)
);

COMMENT ON TABLE  tools_palworld.pal_active_skill              IS 'Compétences actives apprises par un Pal et leur niveau de déblocage';
COMMENT ON COLUMN tools_palworld.pal_active_skill.unlock_level IS 'Niveau du Pal auquel la compétence est débloquée';

-- =========================
-- pal_passive_skill (pas de code stable côté source -> rattachées au Pal, pas de table de référence)
-- =========================
CREATE TABLE tools_palworld.pal_passive_skill (
    id            BIGSERIAL PRIMARY KEY,
    pal_id        BIGINT       NOT NULL REFERENCES tools_palworld.pal(id) ON DELETE CASCADE,
    name          VARCHAR(255) NOT NULL,
    rank          SMALLINT     NOT NULL,
    tooltip       TEXT,
    rank_icon_url TEXT,
    UNIQUE (pal_id, name, rank)
);

COMMENT ON TABLE  tools_palworld.pal_passive_skill      IS 'Talents passifs d''un Pal. Pas de slug stable côté source, donc pas mutualisés en table de référence';
COMMENT ON COLUMN tools_palworld.pal_passive_skill.rank IS 'Rang du talent (1 à 3)';

-- =========================
-- pal_partner_skill (1:1) + ranks
-- =========================
CREATE TABLE tools_palworld.pal_partner_skill (
    pal_id      BIGINT PRIMARY KEY REFERENCES tools_palworld.pal(id) ON DELETE CASCADE,
    title       VARCHAR(255) NOT NULL,
    description TEXT,
    icon_url    TEXT
);

COMMENT ON TABLE tools_palworld.pal_partner_skill IS 'Compétence de partenaire (équipement/monture) du Pal, une par Pal';

CREATE TABLE tools_palworld.pal_partner_skill_rank (
    pal_id      BIGINT       NOT NULL REFERENCES tools_palworld.pal_partner_skill(pal_id) ON DELETE CASCADE,
    sort_order  SMALLINT     NOT NULL,
    level_label VARCHAR(10)  NOT NULL,
    detail      TEXT         NOT NULL,
    PRIMARY KEY (pal_id, sort_order)
);

COMMENT ON TABLE  tools_palworld.pal_partner_skill_rank             IS 'Paliers de la compétence de partenaire';
COMMENT ON COLUMN tools_palworld.pal_partner_skill_rank.sort_order  IS 'Ordre réel du palier (1 à 10) — à utiliser pour trier, pas level_label';
COMMENT ON COLUMN tools_palworld.pal_partner_skill_rank.level_label IS 'Libellé de niveau brut scrapé (ex: "0" représente en réalité le palier 10 chez paldb.cc)';

-- =========================
-- pal_drop
-- =========================
CREATE TABLE tools_palworld.pal_drop (
    pal_id              BIGINT   NOT NULL REFERENCES tools_palworld.pal(id) ON DELETE CASCADE,
    item_id             BIGINT   NOT NULL REFERENCES tools_palworld.item(id),
    quantity_min        SMALLINT,
    quantity_max        SMALLINT,
    probability_percent NUMERIC(5,2),
    sort_order          SMALLINT NOT NULL DEFAULT 0,
    PRIMARY KEY (pal_id, item_id, sort_order)
);

COMMENT ON TABLE tools_palworld.pal_drop IS 'Items droppés par un Pal au K.O., avec quantité et probabilité';

-- =========================
-- pal_variant (variantes Boss/Alpha/régionales sans fiche stats dédiée)
-- =========================
CREATE TABLE tools_palworld.pal_variant (
    id         BIGSERIAL PRIMARY KEY,
    pal_id     BIGINT       NOT NULL REFERENCES tools_palworld.pal(id) ON DELETE CASCADE,
    slug       VARCHAR(150) NOT NULL,
    name       VARCHAR(255) NOT NULL,
    icon_url   TEXT,
    role       VARCHAR(50)  NOT NULL,
    sort_order SMALLINT     NOT NULL DEFAULT 0,
    UNIQUE (pal_id, slug, role)
);

COMMENT ON TABLE  tools_palworld.pal_variant      IS 'Variantes de tribu (Boss, Alpha, régionales...) réutilisant le nom/image du Pal de base, sans stats propres scrapées';
COMMENT ON COLUMN tools_palworld.pal_variant.role IS 'Rôle brut de la source (ex: "Tribe Boss", "Tribe Normal", "Variant")';

-- =========================
-- pal_spawn_zone (texte descriptif uniquement, pas de coordonnées)
-- =========================
CREATE TABLE tools_palworld.pal_spawn_zone (
    id             BIGSERIAL PRIMARY KEY,
    pal_id         BIGINT       NOT NULL REFERENCES tools_palworld.pal(id) ON DELETE CASCADE,
    level_label    VARCHAR(100),
    location_label VARCHAR(255),
    location_link  VARCHAR(150),
    sort_order     SMALLINT     NOT NULL DEFAULT 0
);

COMMENT ON TABLE tools_palworld.pal_spawn_zone IS 'Zones de spawn sauvage, en texte descriptif uniquement (pas de coordonnées carte exploitables côté source)';

-- =========================================================================
-- ACTIVITÉ SERVEUR
-- =========================================================================

-- =========================
-- guild
-- =========================
CREATE TABLE tools_palworld.guild (
    guild_id      UUID PRIMARY KEY,
    name          VARCHAR(255) NOT NULL,
    first_seen_at TIMESTAMPTZ  NOT NULL,
    last_seen_at  TIMESTAMPTZ  NOT NULL
);

COMMENT ON TABLE  tools_palworld.guild           IS 'Guilde du serveur Palworld privé';
COMMENT ON COLUMN tools_palworld.guild.guild_id  IS 'UUID natif du serveur de jeu (pas généré par nous)';

-- =========================
-- player
-- =========================
CREATE TABLE tools_palworld.player (
    player_uid             UUID PRIMARY KEY,
    name                   VARCHAR(255) NOT NULL,
    guild_id               UUID REFERENCES tools_palworld.guild(guild_id),
    last_online_real_time  BIGINT,
    first_seen_at          TIMESTAMPTZ NOT NULL,
    last_seen_at           TIMESTAMPTZ NOT NULL
);

COMMENT ON TABLE  tools_palworld.player                        IS 'Joueur du serveur Palworld privé';
COMMENT ON COLUMN tools_palworld.player.player_uid              IS 'UUID natif du serveur de jeu';
COMMENT ON COLUMN tools_palworld.player.last_online_real_time   IS 'Valeur brute du serveur de jeu (tick moteur), sémantique non convertie en date';

-- =========================
-- base
-- =========================
CREATE TABLE tools_palworld.base (
    base_id       UUID PRIMARY KEY,
    guild_id      UUID NOT NULL REFERENCES tools_palworld.guild(guild_id),
    first_seen_at TIMESTAMPTZ NOT NULL,
    last_seen_at  TIMESTAMPTZ NOT NULL
);

COMMENT ON TABLE  tools_palworld.base          IS 'Base (campement) d''une guilde';
COMMENT ON COLUMN tools_palworld.base.base_id  IS 'UUID natif du serveur de jeu';

-- =========================
-- pal_instance (état courant, upserté à chaque ingestion)
-- =========================
CREATE TABLE tools_palworld.pal_instance (
    instance_id           UUID PRIMARY KEY,
    character_id          VARCHAR(150) NOT NULL,
    pal_id                BIGINT REFERENCES tools_palworld.pal(id),
    is_alpha              BOOLEAN NOT NULL DEFAULT FALSE,
    owner_player_uid       UUID REFERENCES tools_palworld.player(player_uid),
    base_id               UUID NOT NULL REFERENCES tools_palworld.base(base_id),
    level                  SMALLINT,
    exp                    INTEGER,
    full_stomach           NUMERIC(6,3),
    is_sick                BOOLEAN,
    workable_type          VARCHAR(100),
    task_id                VARCHAR(100),
    work_state             SMALLINT,
    current_work_amount    NUMERIC,
    required_work_amount   NUMERIC,
    first_seen_at          TIMESTAMPTZ NOT NULL,
    last_seen_at           TIMESTAMPTZ NOT NULL
);

COMMENT ON TABLE  tools_palworld.pal_instance                      IS 'État courant d''un Pal posé en base (upsert à chaque ingestion de snapshot) — lecture rapide pour le dashboard live';
COMMENT ON COLUMN tools_palworld.pal_instance.instance_id          IS 'UUID natif du serveur de jeu, stable pour ce Pal';
COMMENT ON COLUMN tools_palworld.pal_instance.character_id         IS 'Identifiant brut du serveur (ex: "BOSS_Anubis"), conservé même si non résolu en pal_id';
COMMENT ON COLUMN tools_palworld.pal_instance.pal_id               IS 'Résolution vers le catalogue via tribe (character_id en retirant BOSS_/Boss_, insensible à la casse). NULL = PNJ humain capturé (hors scope Pal), ~9 cas connus';
COMMENT ON COLUMN tools_palworld.pal_instance.is_alpha             IS 'Dérivé du préfixe BOSS_/Boss_ sur character_id';
COMMENT ON COLUMN tools_palworld.pal_instance.work_state           IS 'Byte brut "state" du snapshot serveur (valeurs observées : 1/2/3), sémantique non documentée à ce jour';
COMMENT ON COLUMN tools_palworld.pal_instance.current_work_amount  IS 'Avancement du travail en cours ; le delta entre deux relevés est l''indicateur d''activité réelle';

-- =========================
-- pal_instance_snapshot (historique complet, append-only)
-- =========================
CREATE TABLE tools_palworld.pal_instance_snapshot (
    id                    BIGSERIAL PRIMARY KEY,
    instance_id           UUID        NOT NULL REFERENCES tools_palworld.pal_instance(instance_id) ON DELETE CASCADE,
    captured_at            TIMESTAMPTZ NOT NULL,
    base_id                UUID        NOT NULL REFERENCES tools_palworld.base(base_id),
    level                  SMALLINT,
    exp                    INTEGER,
    full_stomach           NUMERIC(6,3),
    is_sick                BOOLEAN,
    workable_type          VARCHAR(100),
    task_id                VARCHAR(100),
    work_state             SMALLINT,
    current_work_amount    NUMERIC,
    required_work_amount   NUMERIC,
    UNIQUE (instance_id, captured_at)
);

COMMENT ON TABLE  tools_palworld.pal_instance_snapshot             IS 'Historique complet, une ligne par instance par snapshot ingéré (~5 min). full_stomach varie en continu donc un historique "on change" n''aurait rien économisé';
COMMENT ON COLUMN tools_palworld.pal_instance_snapshot.captured_at IS 'Horodatage du snapshot source (champ extracted_at du fichier JSON)';

-- =========================
-- INDEX
-- =========================
CREATE INDEX idx_pal_paldex_index            ON tools_palworld.pal(paldex_index);
CREATE INDEX idx_pal_best_work_suitability   ON tools_palworld.pal(best_work_suitability_id);
CREATE INDEX idx_skill_element               ON tools_palworld.skill(element_id);
CREATE INDEX idx_pal_active_skill_skill      ON tools_palworld.pal_active_skill(skill_id);
CREATE INDEX idx_pal_work_suitability_ws     ON tools_palworld.pal_work_suitability(work_suitability_id);
CREATE INDEX idx_pal_drop_item               ON tools_palworld.pal_drop(item_id);

CREATE INDEX idx_player_guild                ON tools_palworld.player(guild_id);
CREATE INDEX idx_base_guild                  ON tools_palworld.base(guild_id);
CREATE INDEX idx_pal_instance_pal            ON tools_palworld.pal_instance(pal_id);
CREATE INDEX idx_pal_instance_base           ON tools_palworld.pal_instance(base_id);
CREATE INDEX idx_pal_instance_owner          ON tools_palworld.pal_instance(owner_player_uid);
CREATE INDEX idx_pal_instance_snapshot_base  ON tools_palworld.pal_instance_snapshot(base_id, captured_at);
CREATE INDEX idx_pal_instance_snapshot_time  ON tools_palworld.pal_instance_snapshot(captured_at);
