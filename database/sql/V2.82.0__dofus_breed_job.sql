-- Dofus : classes et métiers du jeu, et leur lien avec les ateliers.
--
-- breed, breed_image et job sont des données du jeu, remplies par la synchro Dofus 3
-- (breeds.json, jobs.json). Une ligne par version du jeu : classes et métiers diffèrent entre
-- Retro et Dofus 3.

-- Classe
CREATE TABLE tools_dofus.breed (
    id BIGSERIAL PRIMARY KEY,
    game_version_id BIGINT NOT NULL REFERENCES tools_dofus.game_version(id),
    asset_id BIGINT NOT NULL,
    name VARCHAR(255) NOT NULL,
    UNIQUE (asset_id, game_version_id)
);

COMMENT ON TABLE tools_dofus.breed IS 'Classe jouable';
COMMENT ON COLUMN tools_dofus.breed.id IS 'Identifiant unique interne';
COMMENT ON COLUMN tools_dofus.breed.game_version_id IS 'Version du jeu';
COMMENT ON COLUMN tools_dofus.breed.asset_id IS 'ID issu de BreedData dans les assets Ankama (data.id)';
COMMENT ON COLUMN tools_dofus.breed.name IS 'Nom de la classe résolu depuis I18N (string FR)';

-- Images d'une classe, une ligne par sexe
CREATE TABLE tools_dofus.breed_image (
    id BIGSERIAL PRIMARY KEY,
    breed_id BIGINT NOT NULL REFERENCES tools_dofus.breed(id),
    sex VARCHAR(10) NOT NULL CHECK (sex IN ('MALE', 'FEMALE')),
    icon_id INT4 NOT NULL,
    UNIQUE (breed_id, sex)
);

COMMENT ON TABLE tools_dofus.breed_image IS 'Images d''une classe, par sexe';
COMMENT ON COLUMN tools_dofus.breed_image.id IS 'Identifiant unique interne';
COMMENT ON COLUMN tools_dofus.breed_image.breed_id IS 'Classe associée à cette image';
COMMENT ON COLUMN tools_dofus.breed_image.sex IS 'Sexe représenté : MALE ou FEMALE';
COMMENT ON COLUMN tools_dofus.breed_image.icon_id IS 'gfxId de la tête (Head_{icon_id}), utilisé pour construire l''URL de l''image';

-- Métier
CREATE TABLE tools_dofus.job (
    id BIGSERIAL PRIMARY KEY,
    game_version_id BIGINT NOT NULL REFERENCES tools_dofus.game_version(id),
    asset_id BIGINT NOT NULL,
    name VARCHAR(255) NOT NULL,
    icon_id INT4,
    UNIQUE (asset_id, game_version_id)
);

COMMENT ON TABLE tools_dofus.job IS 'Métier';
COMMENT ON COLUMN tools_dofus.job.id IS 'Identifiant unique interne';
COMMENT ON COLUMN tools_dofus.job.game_version_id IS 'Version du jeu';
COMMENT ON COLUMN tools_dofus.job.asset_id IS 'ID issu de JobData dans les assets Ankama (data.id)';
COMMENT ON COLUMN tools_dofus.job.name IS 'Nom du métier résolu depuis I18N (string FR)';
COMMENT ON COLUMN tools_dofus.job.icon_id IS 'iconId utilisé pour construire l''URL de l''image (NULL si le jeu n''en fournit pas)';

-- Relation workshop <-> classes (many-to-many)
CREATE TABLE tools_dofus.workshop_has_breed (
    workshop_id BIGINT NOT NULL REFERENCES tools_dofus.workshop(id) ON DELETE CASCADE,
    breed_id BIGINT NOT NULL REFERENCES tools_dofus.breed(id),
    sex VARCHAR(10) NOT NULL CHECK (sex IN ('MALE', 'FEMALE')),
    PRIMARY KEY (workshop_id, breed_id)
);

COMMENT ON TABLE tools_dofus.workshop_has_breed IS 'Classes associées à un atelier';
COMMENT ON COLUMN tools_dofus.workshop_has_breed.sex IS 'Sexe de l''image de la classe affichée sur l''atelier (breed_image)';

-- Relation workshop <-> métiers (many-to-many)
CREATE TABLE tools_dofus.workshop_has_job (
    workshop_id BIGINT NOT NULL REFERENCES tools_dofus.workshop(id) ON DELETE CASCADE,
    job_id BIGINT NOT NULL REFERENCES tools_dofus.job(id),
    PRIMARY KEY (workshop_id, job_id)
);

COMMENT ON TABLE tools_dofus.workshop_has_job IS 'Métiers associés à un atelier';
