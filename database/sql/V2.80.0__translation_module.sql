-- Module Traduction : traduction de textes via le conteneur LibreTranslate du NAS, depuis tools et
-- depuis l'extension navigateur (clé d'API, scope translation:translate).
--
-- Aucune table métier : les textes ne sont jamais stockés. Chaque traduction laisse seulement une
-- ligne dans tools_core.application_logs (paire de langues, nombre de caractères, durée).
-- Accès réservé aux membres du module, à partir de READ_ONLY : à attribuer depuis l'administration.

INSERT INTO tools_core.module (code, name, description, is_active)
VALUES (
    'translation',
    'Traduction',
    'Traduction de textes depuis tools.',
    TRUE
);
