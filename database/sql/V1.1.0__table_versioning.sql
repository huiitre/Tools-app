-- 1.1.0 - Création table note de version
CREATE TABLE IF NOT EXISTS tools_core.version (
  id SERIAL PRIMARY KEY,
  version VARCHAR(20) NOT NULL,
  module VARCHAR(50) NOT NULL,
  description TEXT NOT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT NOW()
);
COMMENT ON TABLE tools_core.version IS 'Table contenant le changelog global de Tools, avec version, module et description.';
COMMENT ON COLUMN tools_core.version.id IS 'Identifiant unique de la ligne de changelog.';
COMMENT ON COLUMN tools_core.version.version IS 'Numéro de version globale (SemVer) correspondant à la release Tools.';
COMMENT ON COLUMN tools_core.version.module IS 'Module interne auquel la ligne de changelog se réfère (core, dofus, websocket, service, etc.).';
COMMENT ON COLUMN tools_core.version.description IS 'Description textuelle du changement pour ce module dans cette version.';
COMMENT ON COLUMN tools_core.version.created_at IS 'Timestamp de création de la ligne de changelog.';
CREATE INDEX IF NOT EXISTS idx_version_created_at ON tools_core.version (created_at DESC);
CREATE INDEX IF NOT EXISTS idx_version_module ON tools_core.version (module);