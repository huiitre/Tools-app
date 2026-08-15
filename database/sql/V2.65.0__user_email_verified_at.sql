-- Sépare « email jamais vérifié » de « compte suspendu ».
--
-- Jusqu'ici, is_active portait les deux sens à la fois. Le nettoyage planifié supprimait
-- donc tout compte inactif sans jeton de vérification — y compris un compte suspendu
-- administrativement dont le jeton avait expiré depuis longtemps.
--
-- is_active garde son rôle actuel : le compte est-il autorisé à se connecter.
-- email_verified_at répond à une autre question : l'adresse a-t-elle été confirmée un jour.

ALTER TABLE tools_core.users
ADD COLUMN email_verified_at TIMESTAMP DEFAULT NULL;

COMMENT ON COLUMN tools_core.users.email_verified_at IS
'Date de confirmation de l''adresse email. NULL = jamais confirmée. Seuls ces comptes sont éligibles au nettoyage automatique des inscriptions abandonnées.';

-- Les comptes existants ont tous une adresse confirmée : soit par validation email,
-- soit par un provider externe (Google) qui la garantit. Sans cette reprise, le premier
-- passage du nettoyage les considérerait comme des inscriptions abandonnées.
UPDATE tools_core.users
SET email_verified_at = COALESCE(updated_at, created_at, now())
WHERE email_verified_at IS NULL;

-- Le nettoyage cible les inscriptions jamais confirmées : cet index lui évite un scan complet.
CREATE INDEX idx_users_email_verified_at_null
ON tools_core.users (id)
WHERE email_verified_at IS NULL;
