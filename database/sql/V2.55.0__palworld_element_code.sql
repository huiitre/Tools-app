-- =========================================================================
-- MODULE PALWORLD : element.code
-- =========================================================================
-- Le sync résout désormais element.name via une traduction (strings.json,
-- FR) au lieu du texte anglais scrapé brut. name n'est donc plus utilisable
-- comme clé de jointure stable pal<->element / skill<->element (elle
-- dépend de la langue). code porte le code anglais stable fourni par
-- elements.json (ex: "Fire", "Grass"), alimenté par le sync, sert de
-- nouvelle clé de jointure à la place de l'ancien idByName.
-- =========================================================================

ALTER TABLE tools_palworld.element ADD COLUMN code VARCHAR(20);
