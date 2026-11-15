-- Dofus : marque les monstres boss, pour afficher une icône dans l'infobulle des ressources et à côté
-- des ressources qui ne tombent que sur des boss (atelier).
--
-- Rempli par la synchro Dofus 3 depuis monsters.json (bit 2 de m_flags). Dofus Retro n'a pas
-- d'information fiable : ses monstres restent à FALSE.

ALTER TABLE tools_dofus.monster
    ADD COLUMN is_boss BOOLEAN NOT NULL DEFAULT FALSE;
