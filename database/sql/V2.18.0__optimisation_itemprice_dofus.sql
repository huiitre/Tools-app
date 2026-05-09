-- Index pour "dernier prix user" (ORDER BY created_at DESC LIMIT 1)
CREATE INDEX idx_item_price_user_latest 
ON tools_dofus.item_price_user (item_id, game_server_id, user_id, created_at DESC);

-- Index pour "dernier prix global" (sans user_id)
CREATE INDEX idx_item_price_user_latest_global 
ON tools_dofus.item_price_user (item_id, game_server_id, created_at DESC);

-- Index pour les recettes (lookup par item_id et ingredient_id)
CREATE INDEX idx_recipe_item_id ON tools_dofus.recipe (item_id);
CREATE INDEX idx_recipe_ingredient_id ON tools_dofus.recipe (ingredient_id);