-- Suivi des fichiers snapshot serveur déjà ingérés (tools_palworld_server_data_extractor
-- écrit un fichier data/json/<timestamp>.json toutes les 5 minutes sur le NAS).
--
-- Une ligne n'est insérée qu'après ingestion complète et réussie d'un fichier
-- (dans la même transaction que les inserts pal_instance_snapshot), donc sa
-- présence est un marqueur fiable de "fichier entièrement traité" — contrairement
-- à une déduction depuis pal_instance_snapshot.captured_at qui pourrait masquer
-- un import partiel (crash en cours de traitement).
CREATE TABLE tools_palworld.server_snapshot_import (
    id           BIGSERIAL PRIMARY KEY,
    file_name    VARCHAR(255) NOT NULL UNIQUE,
    extracted_at TIMESTAMPTZ  NOT NULL,
    imported_at  TIMESTAMPTZ  NOT NULL DEFAULT now(),
    guild_count  INTEGER      NOT NULL,
    base_count   INTEGER      NOT NULL,
    pal_count    INTEGER      NOT NULL
);

COMMENT ON TABLE  tools_palworld.server_snapshot_import              IS 'Journal des fichiers snapshot serveur déjà ingérés, pour ne jamais retraiter un fichier deux fois';
COMMENT ON COLUMN tools_palworld.server_snapshot_import.file_name    IS 'Nom du fichier JSON source (ex: 2026-07-26T09-15-01.json), clé d''idempotence pour le scheduler';
COMMENT ON COLUMN tools_palworld.server_snapshot_import.extracted_at IS 'Champ extracted_at du fichier JSON (horodatage réel du snapshot serveur)';
COMMENT ON COLUMN tools_palworld.server_snapshot_import.imported_at  IS 'Date à laquelle notre job a effectivement traité le fichier';

CREATE INDEX idx_server_snapshot_import_extracted_at ON tools_palworld.server_snapshot_import(extracted_at);
