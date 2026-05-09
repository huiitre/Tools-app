CREATE TABLE tools_core.user_password_reset (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL,
    token VARCHAR(255) NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT now(),

    CONSTRAINT fk_user_password_reset_user
        FOREIGN KEY (user_id)
        REFERENCES tools_core.users(id)
        ON DELETE CASCADE,

    CONSTRAINT uq_user_password_reset_token
        UNIQUE (token),

    CONSTRAINT uq_user_password_reset_user
        UNIQUE (user_id)
);

COMMENT ON TABLE tools_core.user_password_reset IS
'Jetons temporaires de réinitialisation de mot de passe (PASSWORD only)';

COMMENT ON COLUMN tools_core.user_password_reset.id IS
'Identifiant interne du jeton de réinitialisation';

COMMENT ON COLUMN tools_core.user_password_reset.user_id IS
'Utilisateur associé au jeton de réinitialisation de mot de passe';

COMMENT ON COLUMN tools_core.user_password_reset.token IS
'Jeton unique envoyé par email pour réinitialiser le mot de passe';

COMMENT ON COLUMN tools_core.user_password_reset.expires_at IS
'Date d’expiration du jeton (TTL court, ex: 30 minutes)';

COMMENT ON COLUMN tools_core.user_password_reset.created_at IS
'Date de création du jeton';