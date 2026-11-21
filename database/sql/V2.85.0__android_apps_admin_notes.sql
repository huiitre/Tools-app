-- Notes libres de l'administrateur sur une app du store huihuistore : où trouver ses APK (dépôt GitHub
-- d'IPTVnator, page de téléchargement de Zen…), remarques. Visibles seulement dans l'administration,
-- jamais dans le catalogue public lu par les appareils.

ALTER TABLE tools_core.android_apps
    ADD COLUMN admin_notes TEXT,
    ADD CONSTRAINT ck_android_apps_admin_notes_length CHECK (char_length(admin_notes) <= 5000);

COMMENT ON COLUMN tools_core.android_apps.admin_notes IS
    'Notes libres de l''administrateur (URL de téléchargement, dépôt…), 5000 caractères au plus ; NULL si aucune. Jamais exposées au catalogue public.';
