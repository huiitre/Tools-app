-- Schema
CREATE SCHEMA tools_temtem;

-- Type (Neutre, Feu, Eau, etc.)
CREATE TABLE tools_temtem.type (
    id INT PRIMARY KEY,
    name VARCHAR(50) NOT NULL
);

COMMENT ON TABLE tools_temtem.type IS 'Types élémentaires des Temtem';
COMMENT ON COLUMN tools_temtem.type.id IS 'ID issu de l''enum ElementalType du jeu';
COMMENT ON COLUMN tools_temtem.type.name IS 'Nom du type';

-- Temtem
CREATE TABLE tools_temtem.temtem (
  id INT PRIMARY KEY,
  name VARCHAR(100) NOT NULL,
  type1_id INT NOT NULL REFERENCES tools_temtem.type(id),
  type2_id INT REFERENCES tools_temtem.type(id)
);

COMMENT ON TABLE tools_temtem.temtem IS 'Créatures du jeu Temtem';
COMMENT ON COLUMN tools_temtem.temtem.id IS 'Numéro du Temtem (monsterNumber)';
COMMENT ON COLUMN tools_temtem.temtem.name IS 'Nom français du Temtem';
COMMENT ON COLUMN tools_temtem.temtem.type1_id IS 'Type principal';
COMMENT ON COLUMN tools_temtem.temtem.type2_id IS 'Type secondaire (nullable)';

-- Technique
CREATE TABLE tools_temtem.technique (
    id INT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    type_id INT NOT NULL REFERENCES tools_temtem.type(id),
    damage INT NOT NULL DEFAULT 0,
    number_of_targets INT NOT NULL
);

COMMENT ON TABLE tools_temtem.technique IS 'Techniques offensives des Temtem';
COMMENT ON COLUMN tools_temtem.technique.id IS 'Numéro de la technique (techniqueNumber)';
COMMENT ON COLUMN tools_temtem.technique.name IS 'Nom français de la technique';
COMMENT ON COLUMN tools_temtem.technique.type_id IS 'Type élémentaire de la technique';
COMMENT ON COLUMN tools_temtem.technique.damage IS 'Dégâts de base';
COMMENT ON COLUMN tools_temtem.technique.number_of_targets IS '0=self, 1=bothEnemies, 2=singleEnemy, 3=singleTarget, 4=all, 5=unknown';

-- Temtem Technique (table pivot)
CREATE TABLE tools_temtem.temtem_technique (
    temtem_id INT NOT NULL REFERENCES tools_temtem.temtem(id),
    technique_id INT NOT NULL REFERENCES tools_temtem.technique(id),
    PRIMARY KEY (temtem_id, technique_id)
);

COMMENT ON TABLE tools_temtem.temtem_technique IS 'Techniques apprises par chaque Temtem';
COMMENT ON COLUMN tools_temtem.temtem_technique.temtem_id IS 'Temtem concerné';
COMMENT ON COLUMN tools_temtem.temtem_technique.technique_id IS 'Technique apprise';

-- Index
CREATE INDEX idx_temtem_type1 ON tools_temtem.temtem(type1_id);
CREATE INDEX idx_temtem_type2 ON tools_temtem.temtem(type2_id);
CREATE INDEX idx_technique_type ON tools_temtem.technique(type_id);
CREATE INDEX idx_temtem_technique_temtem ON tools_temtem.temtem_technique(temtem_id);
CREATE INDEX idx_temtem_technique_technique ON tools_temtem.temtem_technique(technique_id);