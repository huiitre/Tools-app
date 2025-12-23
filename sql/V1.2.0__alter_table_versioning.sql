ALTER TABLE tools_core.version
ALTER COLUMN module DROP NOT NULL;

ALTER TABLE tools_core.version
ADD COLUMN requires_front_update BOOLEAN NOT NULL DEFAULT FALSE;

-- AUTO-GENERATED CHANGELOG SQL

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.1.0', 'Core', 'Ajout de Vuetify dans le projet', '2024-12-21');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.1.0', 'Core', 'Ajout de Google Oauth2 lors de la connexion et retrait du mode d''inscription classique', '2024-12-21');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.1.0', 'Core', 'Mise en place des modules par utilisateur', '2024-12-21');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.1.0', 'Core', 'Mise en place de l''inscription via Google', '2024-12-21');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.2.0', 'Dofus', 'Mise en place du module Dofus avec la recherche des objets', '2024-12-22');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.2.0', 'Dofus', 'Mise en place de la gestion des sets', '2024-12-22');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.2.1', 'Dofus', 'Changement de sens pour la flèche qui dit si c''est mieux de craft ou d''acheter directement l''objet', '2024-12-29');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.3.0', 'Dofus', 'Mise en place de l''affichage des sets partagés', '2024-12-31');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.3.0', 'Dofus', 'Ajout de la génération du lien de partage', '2024-12-31');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.3.0', 'Core', 'Affichage du numéro de version dans le header depuis le package.json', '2024-12-31');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.3.1', 'Dofus', 'Correctif dans le résumé, ajout du multiplicateur de chaque item pour chaque calcul', '2025-01-08');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.3.2', 'Dofus', 'Correctif dans l''affichage d''une carte d''un set, le champ multiplicateur et le bouton de suppression sortaient du cadre à droite', '2025-01-09');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.3.2', 'Dofus', 'Ajout de l''iditem quand on clique sur une image d''un objet pour l''agrandir dans la recherche des items', '2025-01-09');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.3.3', 'Dofus', 'Le prix total d''une ressource dans un set affiche désormais le vrai prix total et non plus le prix unitaire multiplié par 1', '2025-01-18');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.3.3', 'Dofus', 'Changement des libellés ''prix moyen'' par ''prix unitaire''', '2025-01-18');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.4.0', 'Dofus', 'On peut désormais copier le nom d''un objet en cliquant sur ce dernier, dans la recherche d''items et également dans les sets', '2025-01-20');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.5.0', 'Core', 'Ajout d''un bouton dans le header pour signaler un bug ou proposer une amélioration', '2025-01-23');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.6.0', 'Dofus', 'Ajout d''un outil d''aide pour la quête "Une âme en peine", nécessaire pour le dofus pourpre', '2025-01-26');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.6.1', 'Dofus', 'On surcharge la position de départ des minogolems quand on appuie sur le bouton Continuer', '2025-01-26');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.6.1', 'Dofus', 'On décoche les tours joués si le recalcul du résultat est effectué', '2025-01-26');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.7.0', 'Core', 'Ajout de la note de version sur la page d''accueil', '2025-02-26');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.7.0', 'Core', 'Désactivation temporaire du lien qui pointe vers les paramètres', '2025-02-26');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.7.0', 'Core', 'Mise en place de l''application en PWA et récupération interne du type de plateforme', '2025-02-26');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.8.0', 'Répartition d''épargne', 'Création du composant initial', '2025-02-26');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.8.1', 'Core', 'Suppression d''un doublon de chargement du bouton de connexion avec google', '2025-03-16');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.8.1', 'Core', 'Chargement de certains fichiers css en local afin d''éviter certains blocages liés à la latence de unpkg', '2025-03-16');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.9.0', 'Core', 'Ajout de l''appel vers l''api V2', '2025-05-17');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.10.0', 'Healthy', 'Ajout Création du module Health avec suivi sommaire', '2025-09-27');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.11.0', 'Healthy', 'Ajout de la vue calendrier et de la vue graphique', '2025-09-27');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.12.0', 'Dofus', 'Passage de l''api Dofus en V2', '2025-09-30');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.12.0', 'Dofus', 'Correctif lors de la suppression d''un set, le set sélectionné est remis à null', '2025-09-30');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.13.0', 'Todolist', 'Ajout du module TodoList avec gestion des tâches basiques (ajout, modification, suppression, priorité, complétion) V1', '2025-10-04');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.13.1', 'Todolist', 'Correctif d''affichage sur pc', '2025-10-04');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.13.1', 'Core', 'Correctif d''affichage du numéro de version dans le header', '2025-10-04');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.13.2', 'Todolist', 'Correctif d''affichage sur pc', '2025-10-04');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.13.2', 'Todolist', 'Ajout d''un bouton d''annulation et de suppression lorsqu''on modifie une liste ou une tâche', '2025-10-04');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.13.2', 'Todolist', 'Ajout d''une marge entre le nom et la desc d''une tâche', '2025-10-04');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.13.2', 'Todolist', 'Agrandissement en hauteur de l''icone qui ouvre le menu panel d''une liste de tâches', '2025-10-04');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.13.2', 'Todolist', 'Sauvegarde les modifications même après avoir appuyé à l''extérieur de l''élément, après l''édition d''une liste ou d''une tâche', '2025-10-04');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.13.2', 'Todolist', 'Ajouter un scroll une fois qu''une liste ou une tâche est ajoutée', '2025-10-04');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.13.2', 'Todolist', 'Ajout d''un message de confirmation avant la suppression d''une liste ou d''une tâche', '2025-10-04');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.14.0', 'Core', 'Ajout d''un rechargement à chaud quand une nouvelle version de l''application est disponible (PWA)', '2025-10-04');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.15.0', 'Dofus', 'Ajout d''un bouton pour copier le nom d''une ressource dans le presse-papier, dans le résumé d''un set', '2025-10-05');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.15.0', 'Dofus', 'Ajout d''un tri des ressources dans le résumé d''un set, par ordre alphabétique ou par quantité', '2025-10-05');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.15.0', 'Dofus', 'Ajout du prix unitaire de la ressource dans le résumé ainsi que son prix total restant', '2025-10-05');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.16.0', 'Dofus', 'Dans le résumé d''un set, on tri par quantité totale des ressources et non plus par la quantité restante', '2025-10-06');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.16.0', 'Dofus', 'Ajout d''un bouton pour compléter/annuler complètement une ressource, dans un set d''objet ainsi que dans le résumé à droite', '2025-10-06');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.16.1', 'Todolist', 'Gestion des sauts de ligne dans la description d''une tâche', '2025-10-06');

INSERT INTO tools_core.version (version, module, description, created_at)
VALUES ('0.17.0', 'Dofus', 'Ajout d''un bouton pour trier par prix restant asc/desc dans le résumé d''un set', '2025-10-06');

INSERT INTO tools_core.version (version, module, description, created_at, requires_front_update)
VALUES (
  '1.0.0',
  'Core',
  'Déploiement de la version 1.0.0 stable de Tools',
  NOW(),
  false
);

INSERT INTO tools_core.version (version, module, description, created_at, requires_front_update)
VALUES (
  '1.1.0',
  'Core',
  'Création de l’infrastructure de versioning : ajout de la table tools_core.version et mise en place des endpoints backend pour exposer et consommer les notes de version.',
  NOW(),
  false
);

INSERT INTO tools_core.version (version, module, description, created_at, requires_front_update)
VALUES (
  '1.2.0',
  'Core',
  'Mise en place du système intelligent de versioning du front, incluant la vérification automatique, le chargement dynamique des release notes et le tri SemVer.',
  NOW(),
  false
);