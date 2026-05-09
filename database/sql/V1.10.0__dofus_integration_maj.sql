-- ============================================================
-- tools_dofus_unity.dofus_asset_version
-- Passage en table d'état (1 ligne par app_code)
-- ============================================================

-- 1. Supprimer les doublons éventuels (on garde la plus récente)
DELETE FROM tools_dofus_unity.dofus_asset_version a
USING tools_dofus_unity.dofus_asset_version b
WHERE a.app_code = b.app_code
  AND a.created_at < b.created_at;

-- 2. Ajouter la contrainte d'unicité sur app_code
ALTER TABLE tools_dofus_unity.dofus_asset_version
ADD CONSTRAINT uq_dofus_asset_version_app_code UNIQUE (app_code);

-- 3. (Optionnel mais recommandé) Renommer created_at -> updated_at
ALTER TABLE tools_dofus_unity.dofus_asset_version
RENAME COLUMN created_at TO updated_at;

INSERT INTO tools_core.version (version, module, description, requires_front_update)
VALUES
  ('1.10.0', 'Dofus', 'Refonte de la table de version des assets Dofus Unity : passage à une table d''état avec une seule ligne par application (app_code unique), suppression des doublons existants et préparation de l''API pour une logique d''update plutôt que d''historisation.', false);