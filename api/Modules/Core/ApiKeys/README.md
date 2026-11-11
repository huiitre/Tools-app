# Clés d'API

Authentifie les appelants de service à service (extracteurs, autres services du NAS) sans compte
utilisateur. Chaque clé est nommée, limitée à des droits précis (scopes) et révocable depuis
l'administration. Elle remplacera le secret partagé `X-Internal-Token` et les comptes TECH des
extracteurs.

## État

| Chantier | État |
|---|---|
| Migration `V2.79.0__api_keys.sql` | appliquée en local |
| Migration `V2.79.1__api_keys_description_expiration.sql` | écrite, à appliquer en local |
| US-1 — Créer une clé | API + Bruno testés ; front écrit le 11/11/2026, pas encore vérifié dans le navigateur |
| US-2 — Révoquer une clé | API + Bruno testés ; front écrit le 11/11/2026, pas encore vérifié dans le navigateur |
| US-3 — Lister les clés | API + Bruno testés ; front écrit le 11/11/2026, pas encore vérifié dans le navigateur |
| Filtre Actives / Révoquées / Toutes (front) | écrit le 11/11/2026, pas encore vérifié dans le navigateur |
| US-4 — Lister les scopes disponibles | API + Bruno testés ; front écrit le 11/11/2026, pas encore vérifié dans le navigateur |
| US-5 — Appeler l'API avec une clé | à faire |

## User stories

### US-1 — Créer une clé

**En tant qu'**admin, **je veux** créer une clé d'API en renseignant son nom, sa description, sa
date d'expiration et ses scopes, **afin de** donner à un service un accès limité à ce dont il a
besoin.

Critères d'acceptation :
- [ ] La clé en clair est renvoyée une seule fois, dans la réponse de création ; seul son hash est stocké.
- [ ] La date d'expiration est obligatoire et ne dépasse pas 3 ans à partir de la création.
- [ ] Au moins un scope est choisi, et chacun existe dans le catalogue.
- [ ] Cas d'erreur : un nom déjà porté par une clé non révoquée est refusé.
- [ ] Cas d'erreur : une date passée, au-delà de 3 ans ou un scope inconnu sont refusés.

Droits : ADMIN.

### US-2 — Révoquer une clé

**En tant qu'**admin, **je veux** révoquer une clé définitivement, **afin de** couper un accès
compromis ou devenu inutile.

Critères d'acceptation :
- [ ] Dès la révocation, tout appel avec cette clé est refusé.
- [ ] La clé quitte la liste principale ; elle reste en base et reste consultable parmi les révoquées.
- [ ] Une révocation ne s'annule pas.
- [ ] Cas d'erreur : révoquer une clé inexistante ou déjà révoquée est refusé.

Droits : ADMIN.

### US-3 — Lister les clés

**En tant qu'**admin, **je veux** voir la liste des clés, **afin de** savoir qui accède à l'API et
si chaque clé sert encore.

Critères d'acceptation :
- [ ] La liste principale ne montre que les clés non révoquées ; les révoquées s'affichent à part.
- [ ] Chaque clé affiche son nom, sa description, son préfixe, ses scopes, son créateur, sa date
      d'expiration, sa dernière utilisation et la dernière IP.
- [ ] Une clé expirée est signalée comme telle.
- [ ] Un scope qui n'existe plus dans le catalogue est signalé.
- [ ] La clé en clair et son hash ne sont jamais renvoyés.

Droits : ADMIN.

### US-4 — Lister les scopes disponibles

**En tant qu'**admin, **je veux** obtenir la liste des scopes du catalogue, **afin de** les choisir
à la création d'une clé sans les connaître par cœur.

Critères d'acceptation :
- [ ] La liste vient du catalogue défini dans le code, pas de la base.

Droits : ADMIN.

### US-5 — Appeler l'API avec une clé

**En tant que** service (extracteur), **je veux** m'authentifier avec ma clé, **afin de** lancer
les opérations auxquelles elle donne droit sans compte utilisateur.

Critères d'acceptation :
- [ ] Une clé valide permet d'exécuter un use case qui déclare l'un de ses scopes.
- [ ] Chaque utilisation met à jour la dernière utilisation et la dernière IP, au plus une fois par minute.
- [ ] Chaque utilisation est inscrite dans `application_logs`, avec l'id de la clé et jamais la clé.
- [ ] Cas d'erreur : une clé inconnue, révoquée ou expirée est refusée.
- [ ] Cas d'erreur : un use case qui ne déclare aucun des scopes de la clé est refusé.

Droits : la clé elle-même.

## Règles métier

- Une clé n'est jamais supprimée, seulement révoquée.
- Une clé est valide tant qu'elle n'est ni révoquée ni expirée.
- Refus par défaut : un use case qui ne déclare aucun scope est fermé aux clés.
- Un scope publié ne se renomme jamais : renommer revient à supprimer puis recréer.
- Si un scope disparaît du catalogue, seul ce droit cesse de fonctionner ; la clé garde les autres.

## Scopes

Catalogue : `Domain/ApiKeyScope.cs`. Seuls les besoins actuels des services du NAS y figurent.

| Code | Ouvre | Utilisé par |
|---|---|---|
| `core.mail:send` | `POST /internal/mail` | extracteurs Valorant et Temtem |
| `core.notifications:publish` | `POST /internal/notifications` | extracteurs Valorant, Temtem, Dofus, Dofus Retro ; API Java |
| `core.gameservers:sync` | `POST /internal/gameservers/sync` | extracteur des serveurs de jeux |
| `riot.valorant:sync` | `POST /internal/riot/valorant/sync` | extracteur Valorant |
| `temtem:sync` | `POST /internal/temtem/sync` | extracteur Temtem |

## Modèle de données

Migrations : `V2.79.0__api_keys.sql`, `V2.79.1__api_keys_description_expiration.sql`.

### `tools_core.api_keys`

| Colonne | Type | Null | Rôle |
|---|---|---|---|
| `id` | `BIGSERIAL` | non | Clé primaire. |
| `name` | `VARCHAR(100)` | non | Nom affiché dans l'administration. |
| `description` | `VARCHAR(500)` | non | À quoi sert la clé et qui l'utilise. |
| `key_hash` | `BYTEA` | non | SHA-256 de la clé complète, 32 octets. La clé en clair n'est jamais stockée. |
| `key_prefix` | `VARCHAR(32)` | non | Premiers caractères en clair, pour reconnaître la clé sans la révéler. |
| `created_at` | `TIMESTAMPTZ` | non | Création, `now()` par défaut. |
| `created_by` | `BIGINT` | non | Administrateur créateur → `tools_core.users`. |
| `expires_at` | `TIMESTAMPTZ` | non | Fin de validité. |
| `revoked_at` | `TIMESTAMPTZ` | oui | Révocation ; `NULL` tant que la clé est active. |
| `last_used_at` | `TIMESTAMPTZ` | oui | Dernière utilisation ; `NULL` si jamais utilisée. |
| `last_used_ip` | `INET` | oui | IP de la dernière utilisation. |

| Contrainte | Garantit |
|---|---|
| `uq_api_keys_key_hash` | Un hash ne désigne qu'une clé ; c'est l'index de recherche à chaque appel. |
| `ck_api_keys_key_hash_length` | Le hash fait exactement 32 octets. |
| `ck_api_keys_expiration_window` | `created_at < expires_at <= created_at + 3 ans`. |
| `uq_api_keys_active_name` | Nom unique parmi les clés non révoquées ; une révocation libère le nom. |
| `fk_api_keys_created_by` | Le créateur existe ; `ON DELETE RESTRICT`. |

### `tools_core.api_key_scopes`

| Colonne | Type | Null | Rôle |
|---|---|---|---|
| `api_key_id` | `BIGINT` | non | Clé concernée → `tools_core.api_keys`. |
| `scope` | `VARCHAR(100)` | non | Nom du droit, par exemple `core.mail:send`. |

| Contrainte | Garantit |
|---|---|
| `pk_api_key_scopes` | `(api_key_id, scope)` : un droit n'est accordé qu'une fois par clé. |
| `ck_api_key_scopes_format` | Forme `ressource:action` en minuscules (`^[a-z][a-z0-9_.]*:[a-z][a-z0-9_]*$`). |
| `fk_api_key_scopes_api_key` | La clé existe ; `ON DELETE CASCADE`. |

## Routes

Collection Bruno : `Tools API Core/Core/API Keys`.

| Méthode | Route | Story | Droits |
|---|---|---|---|
| `GET` | `/admin/api-keys/scopes` | US-4 | ADMIN |
| `POST` | `/admin/api-keys` | US-1 | ADMIN |
| `GET` | `/admin/api-keys` | US-3 | ADMIN |
| `POST` | `/admin/api-keys/{id}/revoke` | US-2 | ADMIN |

## Décisions

- *11/11/2026* — SHA-256 et non bcrypt : une clé aléatoire ne se devine pas, et un hash
  déterministe permet de la retrouver par index.
- *11/11/2026* — Les scopes sont stockés en texte, le catalogue vit dans le code.
- *11/11/2026* — Le contrôle des scopes se fait dans le use case, comme celui des rôles, jamais
  dans le contrôleur.
- *11/11/2026* — Le scope suffit : une clé n'a pas à appartenir à un module.
- *11/11/2026* — Format de clé : `tk_` + 32 octets aléatoires en Base64Url (46 caractères) ;
  `key_prefix` en garde les 12 premiers. Génération et hash dans `Domain/ApiKeySecret.cs`.
- *11/11/2026* — Les invariants sont vérifiés dans le constructeur, donc aussi à la relecture ;
  seule « expiration dans le futur » reste propre à `Create`, une clé relue pouvant être expirée.
- *11/11/2026* — Un scope stocké que le catalogue ne connaît plus est gardé dans `UnknownScopes`
  au lieu d'être perdu : sans effet, mais affichable. L'invariant « au moins un scope » compte
  les deux.
- *11/11/2026* — Une seule route de liste : les révoquées sont renvoyées avec `IsRevoked`, c'est
  l'interface qui les range à part.

## Hors périmètre

- Modifier les scopes ou l'expiration d'une clé existante : on en crée une nouvelle et on révoque l'ancienne.
- Restaurer une clé révoquée.
