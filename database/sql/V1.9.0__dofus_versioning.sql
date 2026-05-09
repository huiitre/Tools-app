CREATE TABLE tools_dofus_unity.dofus_asset_version (
  id BIGSERIAL PRIMARY KEY,
  app_code VARCHAR(32) NOT NULL,
  current_version VARCHAR(64) NOT NULL,
  previous_version VARCHAR(64),
  created_at TIMESTAMP NOT NULL DEFAULT now()
);

COMMENT ON TABLE tools_dofus_unity.dofus_asset_version IS 'Historique des versions d’assets par application (ex: dofus3), alimenté par le script doduda';

COMMENT ON COLUMN tools_dofus_unity.dofus_asset_version.id IS 'Identifiant technique';

COMMENT ON COLUMN tools_dofus_unity.dofus_asset_version.app_code IS 'Code de l’application concernée (ex: dofus3)';

COMMENT ON COLUMN tools_dofus_unity.dofus_asset_version.current_version IS 'Version courante des assets (format libre ex: 3.8.4.2)';

COMMENT ON COLUMN tools_dofus_unity.dofus_asset_version.previous_version IS 'Version précédente des assets';

COMMENT ON COLUMN tools_dofus_unity.dofus_asset_version.created_at IS 'Date et heure d’enregistrement de la version';

CREATE INDEX idx_dofus_asset_version_app_created_at ON tools_dofus_unity.dofus_asset_version (app_code, created_at DESC);

INSERT INTO tools_core.version (version, module, description, requires_front_update)
VALUES
  ('1.9.0', 'Dofus', 'Création d''une table qui permettra de stocker le numéro de la dernière version à date de chaque application ankama (actuellement dofus3)', false);