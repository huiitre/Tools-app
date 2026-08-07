-- =========================================================================
-- Module Palworld — Catégorie d'item, pour la nouvelle page "Objets"
-- (recherche + filtre par catégorie). Source: item_data.json[].TypeA
-- (EPalItemTypeA::*, préfixe retiré à la sync), 13 valeurs nettes dans le jeu
-- (Weapon, Armor, Material, Food, Blueprint, Consume, Essential, Accessory,
-- Ammo, SpecialWeapon, Glider, CaptureItemModifier, MonsterEquipWeapon).
-- =========================================================================

ALTER TABLE tools_palworld.item
    ADD COLUMN category VARCHAR(50);

COMMENT ON COLUMN tools_palworld.item.category IS 'EPalItemTypeA:: du jeu, préfixe retiré (ex: "Weapon", "Material"), alimenté par la sync du catalogue complet';
