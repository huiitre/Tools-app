-- Tags pour organiser les ateliers
CREATE TABLE tools_dofus.workshop_tag (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL REFERENCES tools_core.users(id),
    name VARCHAR(100) NOT NULL,
    color VARCHAR(7), -- #FF5733
    created_at TIMESTAMP DEFAULT NOW(),
    UNIQUE (user_id, name)
);

-- Atelier
CREATE TABLE tools_dofus.workshop (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL REFERENCES tools_core.users(id),
    game_version_id BIGINT NOT NULL REFERENCES tools_dofus.game_version(id),
    name VARCHAR(255) NOT NULL,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP
);

-- Relation workshop <-> tags (many-to-many)
CREATE TABLE tools_dofus.workshop_has_tag (
    workshop_id BIGINT NOT NULL REFERENCES tools_dofus.workshop(id) ON DELETE CASCADE,
    tag_id BIGINT NOT NULL REFERENCES tools_dofus.workshop_tag(id) ON DELETE CASCADE,
    PRIMARY KEY (workshop_id, tag_id)
);

-- Items dans l'atelier
CREATE TABLE tools_dofus.workshop_item (
    id BIGSERIAL PRIMARY KEY,
    workshop_id BIGINT NOT NULL REFERENCES tools_dofus.workshop(id) ON DELETE CASCADE,
    item_id BIGINT NOT NULL REFERENCES tools_dofus.item(id),
    quantity INT NOT NULL DEFAULT 1,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP,
    UNIQUE (workshop_id, item_id)
);

-- Ingrédients avec hiérarchie de craft
CREATE TABLE tools_dofus.workshop_item_ingredient (
    id BIGSERIAL PRIMARY KEY,
    workshop_item_id BIGINT NOT NULL REFERENCES tools_dofus.workshop_item(id) ON DELETE CASCADE,
    ingredient_id BIGINT NOT NULL REFERENCES tools_dofus.item(id),
    parent_ingredient_id BIGINT REFERENCES tools_dofus.workshop_item_ingredient(id) ON DELETE CASCADE,
    quantity_obtained INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP,
    UNIQUE (workshop_item_id, ingredient_id, parent_ingredient_id)
);

-- Index pour les requêtes fréquentes
CREATE INDEX idx_workshop_user_id ON tools_dofus.workshop(user_id);
CREATE INDEX idx_workshop_item_workshop_id ON tools_dofus.workshop_item(workshop_id);
CREATE INDEX idx_workshop_item_ingredient_workshop_item_id ON tools_dofus.workshop_item_ingredient(workshop_item_id);
CREATE INDEX idx_workshop_item_ingredient_parent_id ON tools_dofus.workshop_item_ingredient(parent_ingredient_id);