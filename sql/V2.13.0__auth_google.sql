CREATE TABLE tools_core.user_email_verification (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL,
    token VARCHAR(255) NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT now(),

    CONSTRAINT fk_user_email_verification_user
        FOREIGN KEY (user_id)
        REFERENCES tools_core.users(id)
        ON DELETE CASCADE,

    CONSTRAINT uq_user_email_verification_token
        UNIQUE (token),

    CONSTRAINT uq_user_email_verification_user
        UNIQUE (user_id)
);

COMMENT ON TABLE tools_core.user_email_verification IS
'Jetons temporaires de validation d’email pour l’activation des comptes utilisateurs (PASSWORD only)';

COMMENT ON COLUMN tools_core.user_email_verification.id IS
'Identifiant interne du jeton de validation email';

COMMENT ON COLUMN tools_core.user_email_verification.user_id IS
'Utilisateur associé au jeton de validation email';

COMMENT ON COLUMN tools_core.user_email_verification.token IS
'Jeton unique envoyé par email pour valider le compte';

COMMENT ON COLUMN tools_core.user_email_verification.expires_at IS
'Date d’expiration du jeton (TTL court, ex: 30 minutes)';

COMMENT ON COLUMN tools_core.user_email_verification.created_at IS
'Date de création du jeton de validation';
