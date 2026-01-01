DROP SCHEMA IF EXISTS tools_dofus CASCADE;

CREATE SCHEMA IF NOT EXISTS tools_dofus;

CREATE TABLE tools_dofus.game_version (
    id   INT4 PRIMARY KEY,
    code VARCHAR(50)  NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL
);

COMMENT ON TABLE  tools_dofus.game_version IS 'Versions majeures du jeu (ex: dofus3, retro)';
COMMENT ON COLUMN tools_dofus.game_version.id   IS 'Identifiant technique de la version';
COMMENT ON COLUMN tools_dofus.game_version.code IS 'Code métier stable de la version';
COMMENT ON COLUMN tools_dofus.game_version.name IS 'Nom lisible de la version';


CREATE TABLE tools_dofus.game_server (
    id              INT4 PRIMARY KEY,
    game_version_id INT4 NOT NULL
        REFERENCES tools_dofus.game_version(id)
);

COMMENT ON TABLE  tools_dofus.game_server IS 'Serveurs de jeu rattachés à une version';
COMMENT ON COLUMN tools_dofus.game_server.id              IS 'Identifiant technique du serveur';
COMMENT ON COLUMN tools_dofus.game_server.game_version_id IS 'Version du jeu associée au serveur';


CREATE TABLE tools_dofus.category (
    id   INT4 PRIMARY KEY,
    name VARCHAR(255) NOT NULL UNIQUE
);

COMMENT ON TABLE  tools_dofus.category IS 'Catégories fonctionnelles d’items (figées, non versionnées)';
COMMENT ON COLUMN tools_dofus.category.id   IS 'Identifiant technique de la catégorie';
COMMENT ON COLUMN tools_dofus.category.name IS 'Nom unique de la catégorie';


CREATE TABLE tools_dofus.item_type (
    id              INT4 PRIMARY KEY,
    asset_id        INT4 NOT NULL,
    game_version_id INT4 NOT NULL
        REFERENCES tools_dofus.game_version(id),
    category_id     INT4 NOT NULL
        REFERENCES tools_dofus.category(id),
    name            VARCHAR(255) NOT NULL,
    CONSTRAINT uq_item_type_version_asset UNIQUE (game_version_id, asset_id)
);

COMMENT ON TABLE  tools_dofus.item_type IS 'Types d’items issus des assets du jeu, versionnés';
COMMENT ON COLUMN tools_dofus.item_type.id              IS 'Identifiant technique du type d’item';
COMMENT ON COLUMN tools_dofus.item_type.asset_id        IS 'Identifiant du type dans les assets (doduda)';
COMMENT ON COLUMN tools_dofus.item_type.game_version_id IS 'Version du jeu du type';
COMMENT ON COLUMN tools_dofus.item_type.category_id     IS 'Catégorie fonctionnelle associée';
COMMENT ON COLUMN tools_dofus.item_type.name            IS 'Nom du type d’item';


CREATE TABLE tools_dofus.item (
    id              INT8 PRIMARY KEY,
    asset_id        INT4 NOT NULL,
    game_version_id INT4 NOT NULL
        REFERENCES tools_dofus.game_version(id),
    item_type_id    INT4 NOT NULL
        REFERENCES tools_dofus.item_type(id),
    name            VARCHAR(255) NOT NULL,
    level           INT4 NOT NULL,
    description     TEXT,
    CONSTRAINT uq_item_version_asset UNIQUE (game_version_id, asset_id)
);

COMMENT ON TABLE  tools_dofus.item IS 'Items du jeu issus des assets, versionnés';
COMMENT ON COLUMN tools_dofus.item.id              IS 'Identifiant technique interne de l’item';
COMMENT ON COLUMN tools_dofus.item.asset_id        IS 'Identifiant de l’item dans les assets (doduda)';
COMMENT ON COLUMN tools_dofus.item.game_version_id IS 'Version du jeu de l’item';
COMMENT ON COLUMN tools_dofus.item.item_type_id    IS 'Type de l’item';
COMMENT ON COLUMN tools_dofus.item.name            IS 'Nom de l’item';
COMMENT ON COLUMN tools_dofus.item.level           IS 'Niveau requis de l’item';
COMMENT ON COLUMN tools_dofus.item.description     IS 'Description textuelle de l’item';


CREATE TABLE tools_dofus.item_image (
    id         INT8 PRIMARY KEY,
    item_id    INT8 NOT NULL
        REFERENCES tools_dofus.item(id),
    icon_id    INT4 NOT NULL,
    resolution VARCHAR(2) NOT NULL,
    CONSTRAINT uq_item_image UNIQUE (item_id, resolution)
);

COMMENT ON TABLE  tools_dofus.item_image IS 'Images associées aux items (1x / 2x)';
COMMENT ON COLUMN tools_dofus.item_image.id         IS 'Identifiant technique de l’image';
COMMENT ON COLUMN tools_dofus.item_image.item_id    IS 'Item associé';
COMMENT ON COLUMN tools_dofus.item_image.icon_id    IS 'Identifiant d’icône issu des assets';
COMMENT ON COLUMN tools_dofus.item_image.resolution IS 'Résolution logique (1x, 2x)';


CREATE TABLE tools_dofus.recipe (
    id             INT8 PRIMARY KEY,
    item_id        INT8 NOT NULL
        REFERENCES tools_dofus.item(id),
    ingredient_id  INT8 NOT NULL
        REFERENCES tools_dofus.item(id),
    quantity       INT4 NOT NULL
);

COMMENT ON TABLE  tools_dofus.recipe IS 'Recettes de craft (liaison item → ingrédients)';
COMMENT ON COLUMN tools_dofus.recipe.id            IS 'Identifiant technique de la recette';
COMMENT ON COLUMN tools_dofus.recipe.item_id       IS 'Item crafté';
COMMENT ON COLUMN tools_dofus.recipe.ingredient_id IS 'Item ingrédient';
COMMENT ON COLUMN tools_dofus.recipe.quantity      IS 'Quantité requise de l’ingrédient';


CREATE TABLE tools_dofus.item_price_user (
    id             INT8 PRIMARY KEY,
    item_id        INT8 NOT NULL
        REFERENCES tools_dofus.item(id),
    game_server_id INT4 NOT NULL
        REFERENCES tools_dofus.game_server(id),
    user_id        INT4 NOT NULL,
    price          FLOAT8 NOT NULL,
    created_at     TIMESTAMP NOT NULL DEFAULT now(),
    CONSTRAINT uq_item_price_user UNIQUE (item_id, game_server_id, user_id)
);

COMMENT ON TABLE  tools_dofus.item_price_user IS 'Prix unitaires d’items saisis par les utilisateurs par serveur';
COMMENT ON COLUMN tools_dofus.item_price_user.id             IS 'Identifiant technique';
COMMENT ON COLUMN tools_dofus.item_price_user.item_id        IS 'Item concerné';
COMMENT ON COLUMN tools_dofus.item_price_user.game_server_id IS 'Serveur de jeu';
COMMENT ON COLUMN tools_dofus.item_price_user.user_id        IS 'Utilisateur ayant saisi le prix';
COMMENT ON COLUMN tools_dofus.item_price_user.price          IS 'Prix unitaire saisi';
COMMENT ON COLUMN tools_dofus.item_price_user.created_at     IS 'Date de saisie du prix';
