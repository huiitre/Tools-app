-- Les identifiants d'utilisateur et de module sont des références historiques, pas des liens de
-- cycle de vie. Supprimer une de ces ressources ne doit ni supprimer ses logs, ni effacer leur
-- identifiant, ni être bloqué par le journal.

ALTER TABLE tools_core.application_logs
    DROP CONSTRAINT fk_application_logs_module,
    DROP CONSTRAINT fk_application_logs_user;

COMMENT ON COLUMN tools_core.application_logs.module_id IS
    'Identifiant historique du module fonctionnel concerné, sans clé étrangère ; NULL désigne le Core.';

COMMENT ON COLUMN tools_core.application_logs.user_id IS
    'Identifiant historique de l''utilisateur à l''origine de l''événement, sans clé étrangère ; NULL pour un visiteur anonyme ou le système.';
