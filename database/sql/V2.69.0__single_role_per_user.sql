-- =====================================================================================
-- Un utilisateur = un rôle global, et retrait des tables de configuration mort-nées.
-- =====================================================================================

-- -------------------------------------------------------------------------------------
-- 1. Rôle global unique
-- -------------------------------------------------------------------------------------
-- La clé primaire (user_id, role_id) autorisait le cumul, alors que rien ne l'a jamais
-- produit : `ReplaceGlobalRoleAsync` supprime avant d'insérer, le frontend n'attribue
-- qu'un rôle, et l'API Java lit le rôle global avec un LIMIT 1. Le cumul ne survivait que
-- dans le schéma, au prix d'un arbitrage « le plus permissif l'emporte » recalculé à
-- chaque lecture des deux API.
--
-- `user_module_role` avait déjà reçu ce traitement en V2.4.0, où sa clé primaire est
-- passée de (user_id, module_id, role_id) à (user_id, module_id). Cette migration termine
-- le travail pour les rôles globaux.
--
-- Elle échoue si un utilisateur porte plusieurs rôles — c'est le comportement voulu, une
-- reprise silencieuse choisirait un droit à la place d'un humain. À contrôler avant :
--   SELECT user_id, count(*) FROM tools_core.user_role GROUP BY user_id HAVING count(*) > 1;

ALTER TABLE tools_core.user_role
DROP CONSTRAINT user_role_pkey;

ALTER TABLE tools_core.user_role
ADD CONSTRAINT user_role_pkey
PRIMARY KEY (user_id);

-- La clé primaire ne couvre plus les recherches par rôle (destinataires d'une
-- notification, résolution des abonnés temps réel), qui filtraient jusqu'ici sur la
-- deuxième colonne de l'ancienne PK. Même index que celui posé sur user_module_role
-- par V2.4.0.
CREATE INDEX idx_user_role_role_id
ON tools_core.user_role (role_id);

COMMENT ON TABLE tools_core.user_role IS
'Rôle global de l''utilisateur, au plus un par utilisateur.';

-- -------------------------------------------------------------------------------------
-- 2. Suppression des tables de configuration
-- -------------------------------------------------------------------------------------
-- `config` et `user_config_override` ont été créées par V2.3.0 et n'ont jamais été lues
-- ni écrites par aucun code — ni C#, ni Java, ni frontend — et sont restées vides.
--
-- Le système de paramètres qui les remplace tient son catalogue en dur dans l'API C#
-- (code, type, module, contraintes, valeur par défaut) et ne persiste que les valeurs
-- surchargées : une table de catalogue en base n'y a plus de rôle. Les conserver
-- laisserait deux schémas concurrents pour la même intention.
--
-- L'ordre compte : user_config_override porte une clé étrangère vers config.

DROP TABLE IF EXISTS tools_core.user_config_override;
DROP TABLE IF EXISTS tools_core.config;
