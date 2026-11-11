-- Clés d'API : description obligatoire et durée de vie bornée.
--
-- Une clé n'est plus jamais illimitée : son expiration est obligatoire et tombe au plus tard trois
-- ans après sa création. Une clé qui ne vit pas indéfiniment limite la durée d'exploitation d'une
-- fuite passée inaperçue.

ALTER TABLE tools_core.api_keys
ADD COLUMN description VARCHAR(500) NOT NULL;

ALTER TABLE tools_core.api_keys
ALTER COLUMN expires_at SET NOT NULL;

ALTER TABLE tools_core.api_keys
DROP CONSTRAINT ck_api_keys_expires_after_creation;

ALTER TABLE tools_core.api_keys
ADD CONSTRAINT ck_api_keys_expiration_window
    CHECK (expires_at > created_at AND expires_at <= created_at + INTERVAL '3 years');

COMMENT ON COLUMN tools_core.api_keys.description IS
    'À quoi sert la clé et qui l''utilise, saisi par l''administrateur à la création.';

COMMENT ON COLUMN tools_core.api_keys.expires_at IS
    'Fin de validité, obligatoire, au plus trois ans après la création.';
