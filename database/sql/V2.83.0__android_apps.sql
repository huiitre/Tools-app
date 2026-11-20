-- Apps Android distribuées par huihuistore, le store personnel des téléviseurs (Fire OS, Google TV).
--
-- Ce n'est pas un module : seul l'administrateur importe les APK et choisit la version proposée aux
-- TV, depuis un onglet de la page d'administration. D'où les tables dans tools_core et aucune ligne
-- dans tools_core.module.
--
-- Ne concerne que les apps tierces (IPTVnator, Zen IPTV, Smarters…), toujours importées à la main.
-- huihuistore lui-même n'apparaît pas ici : sa CI dépose son APK par une route dédiée, sans base.
--
-- Les fichiers vivent dans tools_assets (tools_core/android_apps/<env>/<package>/<versionCode>.apk) ;
-- la base ne garde que leur chemin relatif, l'URL se construit depuis AssetsBaseUrl.

-- ---------------------------------------------------------------------------
-- android_apps : une ligne par app suivie
-- ---------------------------------------------------------------------------
-- La clé étrangère vers la version recommandée est ajoutée après la création des releases : les
-- deux tables se référencent mutuellement.
CREATE TABLE tools_core.android_apps (
    id                      BIGSERIAL    PRIMARY KEY,
    package_name            VARCHAR(255) NOT NULL,
    label                   VARCHAR(100) NOT NULL,
    display_order           INT          NOT NULL DEFAULT 0,
    is_active               BOOLEAN      NOT NULL DEFAULT TRUE,
    recommended_release_id  BIGINT,
    created_at              TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT uq_android_apps_package_name UNIQUE (package_name),

    -- Au moins deux segments, chacun commençant par une lettre : la règle d'Android pour un nom de
    -- paquet (com.exemple.app).
    CONSTRAINT ck_android_apps_package_name
        CHECK (package_name ~ '^[A-Za-z][A-Za-z0-9_]*(\.[A-Za-z][A-Za-z0-9_]*)+$')
);

COMMENT ON TABLE tools_core.android_apps IS
    'Apps Android tierces distribuées par huihuistore ; huihuistore lui-même n''y figure pas.';

COMMENT ON COLUMN tools_core.android_apps.package_name IS
    'Nom de paquet Android (applicationId), lu dans l''APK au premier import ; identifie l''app sur l''appareil.';

COMMENT ON COLUMN tools_core.android_apps.label IS
    'Nom affiché sur la TV.';

COMMENT ON COLUMN tools_core.android_apps.display_order IS
    'Ordre d''affichage dans le catalogue, croissant.';

COMMENT ON COLUMN tools_core.android_apps.is_active IS
    'FALSE retire l''app du catalogue sans rien supprimer.';

COMMENT ON COLUMN tools_core.android_apps.recommended_release_id IS
    'Version proposée aux TV, choisie à la main parmi les releases de l''app ; NULL : app absente du catalogue. Une nightly importée pour test reste ainsi invisible.';

-- ---------------------------------------------------------------------------
-- android_app_releases : une ligne par APK importé
-- ---------------------------------------------------------------------------
-- Toutes les informations viennent de l'APK lui-même, lues par l'API à l'import : rien n'est saisi.
CREATE TABLE tools_core.android_app_releases (
    id             BIGSERIAL    PRIMARY KEY,
    app_id         BIGINT       NOT NULL,

    version_code   BIGINT       NOT NULL,
    version_name   VARCHAR(100),
    min_sdk        INT          NOT NULL,

    file_path      VARCHAR(500) NOT NULL,
    file_size      BIGINT       NOT NULL,
    file_sha256    BYTEA        NOT NULL,
    cert_sha256    BYTEA        NOT NULL,

    release_notes  TEXT,

    uploaded_at    TIMESTAMPTZ  NOT NULL DEFAULT now(),
    uploaded_by    BIGINT       NOT NULL,

    CONSTRAINT uq_android_app_releases_version UNIQUE (app_id, version_code),
    CONSTRAINT uq_android_app_releases_file_path UNIQUE (file_path),

    -- Cible de la clé composite posée par android_apps : elle garantit que la version recommandée
    -- appartient bien à l'app qui la recommande.
    CONSTRAINT uq_android_app_releases_app_id_id UNIQUE (app_id, id),

    CONSTRAINT ck_android_app_releases_version_code CHECK (version_code > 0),
    CONSTRAINT ck_android_app_releases_min_sdk CHECK (min_sdk > 0),
    CONSTRAINT ck_android_app_releases_file_size CHECK (file_size > 0),
    CONSTRAINT ck_android_app_releases_file_sha256_length CHECK (octet_length(file_sha256) = 32),
    CONSTRAINT ck_android_app_releases_cert_sha256_length CHECK (octet_length(cert_sha256) = 32),

    CONSTRAINT fk_android_app_releases_app
        FOREIGN KEY (app_id) REFERENCES tools_core.android_apps (id) ON DELETE CASCADE,

    CONSTRAINT fk_android_app_releases_uploaded_by
        FOREIGN KEY (uploaded_by) REFERENCES tools_core.users (id) ON DELETE RESTRICT
);

COMMENT ON TABLE tools_core.android_app_releases IS
    'APK importés pour chaque app ; l''historique permet de revenir à une version et de comparer les certificats à l''import.';

COMMENT ON COLUMN tools_core.android_app_releases.version_code IS
    'versionCode lu dans l''APK ; Android compare ce nombre, et refuse d''installer une valeur inférieure à celle en place.';

COMMENT ON COLUMN tools_core.android_app_releases.version_name IS
    'versionName lu dans l''APK, affiché ; NULL si l''APK n''en déclare pas.';

COMMENT ON COLUMN tools_core.android_app_releases.min_sdk IS
    'minSdkVersion lu dans l''APK ; une TV d''API inférieure affiche l''app comme incompatible.';

COMMENT ON COLUMN tools_core.android_app_releases.file_path IS
    'Chemin relatif dans tools_assets, préfixé par l''environnement : tools_core/android_apps/<prod|qa>/<package>/<versionCode>.apk.';

COMMENT ON COLUMN tools_core.android_app_releases.file_size IS
    'Taille de l''APK en octets, affichée avant le téléchargement.';

COMMENT ON COLUMN tools_core.android_app_releases.file_sha256 IS
    'SHA-256 du fichier (32 octets), vérifié par la TV avant l''installation.';

COMMENT ON COLUMN tools_core.android_app_releases.cert_sha256 IS
    'SHA-256 du certificat du signataire actuel (32 octets). Un changement entre deux releases impose une désinstallation sur la TV. La rotation de clé (schéma v3) n''est pas suivie.';

COMMENT ON COLUMN tools_core.android_app_releases.uploaded_by IS
    'Administrateur qui a importé l''APK.';

-- ---------------------------------------------------------------------------
-- Version recommandée
-- ---------------------------------------------------------------------------
-- Clé composite : une app ne peut recommander qu'une de ses propres releases. Laissée en NO ACTION
-- (et non RESTRICT) : la suppression d'une app supprime ses releases en cascade dans la même
-- instruction, et seul NO ACTION attend la fin de l'instruction pour vérifier. Hors de ce cas, une
-- release recommandée ne peut pas être supprimée : il faut d'abord en recommander une autre.
ALTER TABLE tools_core.android_apps
    ADD CONSTRAINT fk_android_apps_recommended_release
        FOREIGN KEY (id, recommended_release_id)
        REFERENCES tools_core.android_app_releases (app_id, id);
