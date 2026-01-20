ALTER TABLE tools_dofus.almanax
ADD COLUMN item_id BIGINT NULL,
ADD COLUMN item_quantity INTEGER NULL;

COMMENT ON COLUMN tools_dofus.almanax.item_id
IS 'ID interne de l’item requis pour l’Almanax (FK vers tools_dofus.item.id)';

COMMENT ON COLUMN tools_dofus.almanax.item_quantity
IS 'Quantité de l’item requise pour l’Almanax';
