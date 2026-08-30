-- Le réordonnancement d'une équipe échange des slots déjà occupés. PostgreSQL vérifie une
-- contrainte UNIQUE non différable ligne à ligne et refuserait donc un simple échange 1 <-> 2.
-- La rendre différable permet au use case de remplacer l'ordre complet dans sa transaction,
-- tout en le vérifiant au commit.
ALTER TABLE tools_temtem.team_member
    DROP CONSTRAINT uq_temtem_team_member_slot,
    ADD CONSTRAINT uq_temtem_team_member_slot UNIQUE (team_id, slot) DEFERRABLE INITIALLY IMMEDIATE;
