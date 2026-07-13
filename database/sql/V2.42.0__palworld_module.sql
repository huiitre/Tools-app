CREATE SCHEMA IF NOT EXISTS tools_palworld;

COMMENT ON SCHEMA tools_palworld IS 'Schéma du module Palworld : administration du serveur via la REST API Palworld';

-- =========================
-- MODULE
-- =========================
INSERT INTO tools_core.module (code, name, description, is_active)
VALUES ('TOOLS_PALWORLD', 'Palworld', 'Outils pour le jeux Palworld', TRUE);

-- Correctif : aligner le code sur la convention ModuleCode.name().toLowerCase() (cf. riot, dofus, elite_dangerous)
UPDATE tools_core.module SET code = 'palworld' WHERE code = 'TOOLS_PALWORLD';