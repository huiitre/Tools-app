-- Recherche floue sur les noms : pg_trgm (similarité par trigrammes) et unaccent (accents ignorés).
-- Préparent l'ajout d'objets en vrac à un atelier Dofus (web/AGENTS.md, « ajout d'objets en vrac »).
--
-- unaccent existait déjà sur certaines bases, créée à la main hors migration : IF NOT EXISTS la laisse en place.
-- Les deux sont des extensions « trusted » : le propriétaire de la base peut les créer sans être superutilisateur.

CREATE EXTENSION IF NOT EXISTS pg_trgm WITH SCHEMA public;
CREATE EXTENSION IF NOT EXISTS unaccent WITH SCHEMA public;
