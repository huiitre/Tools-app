-- Table pour stocker les jetons d'authentification Riot Games de manière chiffrée
CREATE TABLE tools_riot.valorant_auth (
    user_id             BIGINT PRIMARY KEY,
    puuid               VARCHAR(255) NOT NULL,
    region              VARCHAR(10) NOT NULL,
    
    -- Jetons chiffrés (AES-GCM)
    -- Le format stocké sera typiquement Base64
    encrypted_access    TEXT NOT NULL,
    encrypted_refresh   TEXT, 
    encrypted_entitlements TEXT NOT NULL,
    
    -- Vecteur d'initialisation (IV) utilisé pour le chiffrement de la ligne
    encryption_iv       VARCHAR(255) NOT NULL,
    
    expires_at          TIMESTAMP NOT NULL,
    created_at          TIMESTAMP NOT NULL DEFAULT now(),
    updated_at          TIMESTAMP NOT NULL DEFAULT now(),

    CONSTRAINT fk_valorant_auth_user 
        FOREIGN KEY (user_id) 
        REFERENCES tools_core.users (id) 
        ON DELETE CASCADE
);

COMMENT ON TABLE tools_riot.valorant_auth IS 'Identifiants Riot/Valorant chiffrés pour automatisation et sessions persistantes';
COMMENT ON COLUMN tools_riot.valorant_auth.user_id IS 'ID de l''utilisateur Tools propriétaire';
COMMENT ON COLUMN tools_riot.valorant_auth.puuid IS 'Player UUID unique de Riot Games';
COMMENT ON COLUMN tools_riot.valorant_auth.region IS 'Région Riot (eu, na, ap, latam, br, kr)';
COMMENT ON COLUMN tools_riot.valorant_auth.encrypted_access IS 'Access Token chiffré (AES-GCM)';
COMMENT ON COLUMN tools_riot.valorant_auth.encrypted_refresh IS 'Refresh Token chiffré (Optionnel, pour automatisation)';
COMMENT ON COLUMN tools_riot.valorant_auth.encrypted_entitlements IS 'Entitlements Token chiffré';
COMMENT ON COLUMN tools_riot.valorant_auth.encryption_iv IS 'IV aléatoire utilisé pour le chiffrement AES de cette ligne';
COMMENT ON COLUMN tools_riot.valorant_auth.expires_at IS 'Date d''expiration de l''access_token actuel';

-- Index pour accélérer le travail du Worker de rafraîchissement
CREATE INDEX idx_valorant_auth_automation ON tools_riot.valorant_auth (user_id) 
WHERE encrypted_refresh IS NOT NULL;

-- Suppression des colonnes inutiles pour la sécurité (Access et Entitlements ne sont pas stockés)
ALTER TABLE tools_riot.valorant_auth DROP COLUMN encrypted_access;
ALTER TABLE tools_riot.valorant_auth DROP COLUMN encrypted_entitlements;

-- Rendre le Refresh Token obligatoire pour cette table
ALTER TABLE tools_riot.valorant_auth ALTER COLUMN encrypted_refresh SET NOT NULL;

-- Mise à jour des commentaires
COMMENT ON TABLE tools_riot.valorant_auth IS 'Identifiants Riot/Valorant chiffrés. Stockage exclusif du refresh_token pour sécurité maximale.';
COMMENT ON COLUMN tools_riot.valorant_auth.encrypted_refresh IS 'Refresh Token chiffré (AES-GCM). Permet de régénérer l''access_token à la demande.';
COMMENT ON COLUMN tools_riot.valorant_auth.expires_at IS 'Date d''expiration du refresh_token actuel.';
