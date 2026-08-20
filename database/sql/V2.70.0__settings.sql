-- =====================================================================================
-- Paramètres utilisateur : la table des valeurs surchargées.
-- =====================================================================================
--
-- Une seule table, et volontairement pas de table de catalogue.
--
-- Le catalogue — code du paramètre, type de valeur, module de rattachement, contraintes,
-- valeur par défaut, rôle minimum pour surcharger — est déclaré **en dur dans l'API C#**.
-- Ajouter un paramètre est donc un changement de code suivi d'un déploiement, jamais une
-- migration : c'est précisément ce qu'on cherche. La base ne retient que ce que le code ne
-- peut pas connaître, à savoir les valeurs que quelqu'un a posées.
--
-- Conséquence à ne pas perdre de vue : **le module ne se stocke pas ici**. `ui.theme` est
-- global et `autoSync` appartient à Dofus, mais c'est une propriété du paramètre, pas de la
-- valeur. La dupliquer en base créerait une seconde vérité à resynchroniser.
--
-- Les tables `tools_core.config` et `tools_core.user_config_override`, qui visaient la même
-- intention avec un catalogue en base, ont été supprimées par V2.69.0 sans avoir jamais été
-- lues ni écrites.

CREATE TABLE tools_core.setting_value (
    id          BIGSERIAL    PRIMARY KEY,
    code        VARCHAR(150) NOT NULL,
    scope       VARCHAR(10)  NOT NULL,
    role_code   VARCHAR(50),
    user_id     BIGINT,
    value       JSONB        NOT NULL,
    is_locked   BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT now(),
    updated_at  TIMESTAMPTZ  NOT NULL DEFAULT now(),
    updated_by  BIGINT,

    -- Chaque portée désigne sa cible, et une seule. Sans cette contrainte, une ligne
    -- `scope = 'GLOBAL'` portant un `user_id` serait acceptée et ne serait jamais lue par
    -- personne — une valeur invisible est pire qu'une valeur refusée.
    CONSTRAINT setting_value_target_ck CHECK (
        (scope = 'GLOBAL' AND role_code IS NULL     AND user_id IS NULL)
     OR (scope = 'ROLE'   AND role_code IS NOT NULL AND user_id IS NULL)
     OR (scope = 'USER'   AND role_code IS NULL     AND user_id IS NOT NULL)
    ),

    CONSTRAINT fk_setting_value_role
        FOREIGN KEY (role_code) REFERENCES tools_core.role (code)
        ON DELETE CASCADE DEFERRABLE INITIALLY DEFERRED,

    CONSTRAINT fk_setting_value_user
        FOREIGN KEY (user_id) REFERENCES tools_core.users (id)
        ON DELETE CASCADE DEFERRABLE INITIALLY DEFERRED,

    -- L'auteur disparaît, la valeur reste : elle continue de s'appliquer, seule sa
    -- traçabilité se perd.
    CONSTRAINT fk_setting_value_updated_by
        FOREIGN KEY (updated_by) REFERENCES tools_core.users (id)
        ON DELETE SET NULL DEFERRABLE INITIALLY DEFERRED
);

-- Une valeur au plus par (paramètre, cible). PostgreSQL ne fait pas ce travail avec une
-- contrainte UNIQUE multi-colonnes : deux lignes GLOBAL du même code passeraient, leurs
-- `role_code` et `user_id` valant NULL, et NULL n'entre pas en collision avec NULL. D'où
-- trois index partiels, un par portée.
CREATE UNIQUE INDEX setting_value_global_uk
ON tools_core.setting_value (code)
WHERE scope = 'GLOBAL';

CREATE UNIQUE INDEX setting_value_role_uk
ON tools_core.setting_value (code, role_code)
WHERE scope = 'ROLE';

CREATE UNIQUE INDEX setting_value_user_uk
ON tools_core.setting_value (code, user_id)
WHERE scope = 'USER';

-- Charger les paramètres d'un utilisateur, et surtout laisser la suppression en cascade
-- d'un compte trouver ses lignes : PostgreSQL n'indexe pas les clés étrangères tout seul.
CREATE INDEX idx_setting_value_user_id
ON tools_core.setting_value (user_id)
WHERE user_id IS NOT NULL;

COMMENT ON TABLE tools_core.setting_value IS
'Valeurs de paramètres surchargées. Le catalogue des paramètres vit dans le code de l''API, pas ici.';

COMMENT ON COLUMN tools_core.setting_value.id IS
'Identifiant technique de la valeur';

COMMENT ON COLUMN tools_core.setting_value.code IS
'Code du paramètre visé (ex. ui.theme). Aucune clé étrangère : le catalogue est en dur dans l''API. Une ligne dont le code n''y figure plus est ignorée à la lecture, jamais une erreur.';

COMMENT ON COLUMN tools_core.setting_value.scope IS
'Portée de la valeur : GLOBAL, ROLE ou USER. Priorité à la résolution : USER > ROLE > GLOBAL > valeur par défaut du catalogue.';

COMMENT ON COLUMN tools_core.setting_value.role_code IS
'Rôle visé si scope = ROLE, NULL sinon. Le code et non l''identifiant : toute l''API raisonne en RoleCode, résoudre un id imposerait une jointure à chaque lecture.';

COMMENT ON COLUMN tools_core.setting_value.user_id IS
'Utilisateur visé si scope = USER, NULL sinon';

COMMENT ON COLUMN tools_core.setting_value.value IS
'Valeur, en JSON. JSONB et non TEXT parce qu''un paramètre multi-sélection est un tableau : en TEXT il faudrait un encodage maison, qui casse dès qu''une option contient le séparateur. Un booléen, un entier ou une chaîne restent des scalaires JSON.';

COMMENT ON COLUMN tools_core.setting_value.is_locked IS
'Valeur verrouillée : elle s''impose et aucune portée plus prioritaire ne peut la remplacer. Un ui.theme verrouillé en GLOBAL écrase la valeur USER existante sans la supprimer — le déverrouillage la fait revenir. Sur une ligne USER, où rien n''est plus prioritaire, cela signifie que l''utilisateur ne peut pas modifier sa propre valeur.';

COMMENT ON COLUMN tools_core.setting_value.created_at IS
'Date de création de la valeur';

COMMENT ON COLUMN tools_core.setting_value.updated_at IS
'Date de dernière modification de la valeur';

COMMENT ON COLUMN tools_core.setting_value.updated_by IS
'Utilisateur ayant posé la valeur en dernier, NULL si son compte a été supprimé';
