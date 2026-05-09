INSERT INTO tools_core.version (version, module, description, requires_front_update)
VALUES
  ('1.8.0', 'Core', 'Ajout d’un service de traduction performant et auto-hébergé, avec support du batch et bascule interne entre moteur local et LLM (qwen2.5:7b).', false);

-- création de la colonne pour définir un utilisateur applicatif (easyweb / distrilog)
ALTER TABLE tools_core."user"
ADD COLUMN user_type VARCHAR(20) NOT NULL DEFAULT 'HUMAN';

ALTER TABLE tools_core."user"
ADD CONSTRAINT chk_user_user_type
CHECK (user_type IN ('HUMAN', 'APPLICATION', 'SYSTEM'));

COMMENT ON COLUMN tools_core."user".user_type IS
'Type de compte :
- HUMAN : utilisateur physique (connexion UI, actions manuelles)
- APPLICATION : compte applicatif (service-to-service, API, intégrations)
- SYSTEM : compte système interne (batch, maintenance, tâches automatiques)';

-- création du module "traduction" qui contiendra la route de traduction via le llm
insert into tools_core.module(name, is_active, code) values ('Traduction', 1, 'traduction');