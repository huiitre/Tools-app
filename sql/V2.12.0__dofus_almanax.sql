CREATE TABLE tools_dofus.almanax (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    asset_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    description TEXT NOT NULL,
    dates JSONB NOT NULL DEFAULT '[]'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
COMMENT ON TABLE tools_dofus.almanax IS
'Raw Almanax asset data.';

COMMENT ON COLUMN tools_dofus.almanax.id IS
'Clé technique interne (INT8).';

COMMENT ON COLUMN tools_dofus.almanax.asset_id IS
'Identifiant Almanax provenant des assets (AlmanaxCalendarData.id).';

COMMENT ON COLUMN tools_dofus.almanax.name IS
'Nom résolu à la synchro (string finale).';

COMMENT ON COLUMN tools_dofus.almanax.description IS
'Description résolue à la synchro (string finale).';

COMMENT ON COLUMN tools_dofus.almanax.dates IS
'Patterns bruts des dates (DD/MM/*, */MM/*, DD/MM/YYYY).';

COMMENT ON COLUMN tools_dofus.almanax.created_at IS
'Horodatage technique de création.';