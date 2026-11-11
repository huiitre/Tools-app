# Nom du module

<!--
Modèle de documentation d'un module. Copier ce fichier à la racine du dossier du module,
le renommer README.md, puis supprimer les commentaires et les sections inutiles.
Rester court : ce document doit pouvoir être mis à jour en deux minutes.
-->

Une ou deux phrases : à quoi sert le module et pour qui.

## État

| Chantier | État |
|---|---|
| Migration `V2.x.y__nom.sql` | à faire / écrite / appliquée en local / déployée |
| US-1 | à faire / en cours / livrée |

## User stories

<!--
Une story = un besoin, du point de vue de celui qui en profite (utilisateur, admin, extracteur…).
Les critères d'acceptation disent quand la story est terminée : chacun doit pouvoir se vérifier
(dans Bruno, en base ou dans le navigateur). Les cas d'erreur sont des critères à part entière.
-->

### US-1 — Titre court

**En tant que** <rôle>, **je veux** <action> **afin de** <bénéfice>.

Critères d'acceptation :
- [ ] Étant donné <contexte>, quand <action>, alors <résultat attendu>.
- [ ] Cas d'erreur : quand <situation invalide>, alors <réponse attendue>.

Droits : <rôle minimum, module ou scope exigé>.

### US-2 — Titre court

**En tant que** <rôle>, **je veux** <action> **afin de** <bénéfice>.

Critères d'acceptation :
- [ ] …

Droits : …

## Règles métier

<!-- Les invariants qui ne dépendent d'aucune story : ce qui doit rester vrai quoi qu'il arrive. -->

- …

## Modèle de données

<!-- Une section par table : ses colonnes, puis ce que garantit chaque contrainte. Pas de recopie du SQL. -->

Migrations : `V2.x.y__nom.sql`.

### `schema.table`

| Colonne | Type | Null | Rôle |
|---|---|---|---|
| `id` | `BIGSERIAL` | non | Clé primaire. |

| Contrainte | Garantit |
|---|---|
| `uq_…` | … |

## Routes

<!-- Le détail de chaque route est dans la collection Bruno : ici, seulement la liste. -->

| Méthode | Route | Story | Droits |
|---|---|---|---|
| `GET` | `/…` | US-1 | … |

## Décisions

<!-- Choix faits et leur raison, datés. C'est ce qu'on a oublié dans six mois. -->

- *JJ/MM/AAAA* — Décision, parce que …

## Hors périmètre

<!-- Ce qui a été volontairement écarté, pour ne pas le rediscuter. -->

- …
