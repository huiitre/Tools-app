-- Liens externes associés aux ateliers (Dofusbook, etc.)
CREATE TABLE tools_dofus.workshop_link (
    id BIGSERIAL PRIMARY KEY,
    workshop_id BIGINT NOT NULL REFERENCES tools_dofus.workshop(id) ON DELETE CASCADE,
    source VARCHAR(50) NOT NULL, -- Ex: 'DOFUSBOOK', 'BRUT'
    url TEXT NOT NULL,
    label TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT NOW()
);

-- Index pour la performance des jointures lors du chargement de l'atelier
CREATE INDEX idx_workshop_link_workshop_id ON tools_dofus.workshop_link(workshop_id);

COMMENT ON TABLE tools_dofus.workshop_link IS 'Table stockant les liens externes (Dofusbook, etc.) rattachés aux ateliers.';
COMMENT ON COLUMN tools_dofus.workshop_link.workshop_id IS 'Référence vers l''atelier parent.';
COMMENT ON COLUMN tools_dofus.workshop_link.source IS 'Identifiant du fansite ou type de lien (Enum côté API).';
COMMENT ON COLUMN tools_dofus.workshop_link.url IS 'URL complète du lien.';
COMMENT ON COLUMN tools_dofus.workshop_link.label IS 'Titre affiché du lien (récupéré ou saisi manuellement).';