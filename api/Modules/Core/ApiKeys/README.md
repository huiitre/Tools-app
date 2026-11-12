# Clés d'API

Authentifie les **services** (extracteurs du NAS, API Java) qui appellent l'API C# sans compte
utilisateur. Chaque clé est nommée, limitée à des droits précis (**scopes**), expire au plus tard
3 ans après sa création et se révoque depuis l'administration (onglet « Clés d'API »). Elle
remplace le secret unique `X-Internal-Token`, qui ouvrait tout à tout le monde.

## État (11/11/2026)

| Chantier | État |
|---|---|
| Migrations `V2.79.0`, `V2.79.1` | appliquées en local ; QA/prod au déploiement |
| Administration : créer, lister, révoquer, filtre actives/révoquées (US-1 à US-4) | livrée (API, front, Bruno) |
| Appeler l'API avec une clé (US-5) | livrée en C#, testée (unitaires, intégration, Postgres réel) |
| Migrer les appelants vers `X-Api-Key` | à faire, un par un (tableau « Appelants ») |
| Retirer `X-Internal-Token` et les alias `/internal/mail`, `/internal/notifications` | après le dernier appelant migré |

## Comment ça marche

**Création** — un admin choisit nom, description, scopes et expiration. L'API génère la clé
(`tk_` + 32 octets aléatoires, 46 caractères), la renvoie **une seule fois**, et ne stocke que son
SHA-256 et ses 12 premiers caractères (pour la reconnaître dans la liste).

**Appel** — le service envoie `X-Api-Key: <clé>`.
1. Le schéma d'authentification par défaut voit l'en-tête et passe la main au schéma `ApiKey`
   (sans en-tête de clé, c'est le JWT). Un appel vient d'un utilisateur **ou** d'une clé.
2. La clé est retrouvée par son hash. Inconnue, révoquée ou expirée → `401 INVALID_API_KEY`.
3. Le use case vérifie le scope, comme il vérifie le rôle d'un utilisateur :
   - il ne déclare aucun scope → `403 API_KEY_NOT_ALLOWED` (refus par défaut) ;
   - la clé ne porte pas le scope déclaré → `403 INSUFFICIENT_SCOPE`.

**Trace** — `last_used_at` / `last_used_ip` sont rafraîchis au plus une fois par minute. Seul ce qui
mérite l'attention écrit une ligne `API_KEY` dans `application_logs`, au départ de la réponse. Un appel
réussi n'en écrit pas : un extracteur lancé chaque minute noyait le journal (décision du 12/11/2026),
`last_used_at` suffit à dire qu'une clé sert.

| Action | Quand | Métadonnées |
|---|---|---|
| `USED` | clé acceptée mais réponse en erreur, `status` ≥ 400 (403 = clé valide employée hors de ses scopes) | `apiKeyId`, `apiKeyName`, `method`, `path`, `status` |
| `REJECTED` | clé refusée | `reason` (`UNKNOWN`, `REVOKED`, `EXPIRED`, `LEGACY_TOKEN_MISMATCH`), 12 premiers caractères |
| `LEGACY_USED` | ancien `X-Internal-Token` | `method`, `path`, `status` |

Jamais la clé ni son hash.

**Transition** — `X-Internal-Token` reste accepté comme une clé portant **tous** les scopes (son
pouvoir d'avant) ; `LEGACY_USED` dit qui l'emploie encore. `/internal/mail` et
`/internal/notifications` sont des alias de `POST /mail` et `POST /notifications`.
`/internal/realtime/publish` garde son ancien contrôle (`[InternalApi]`) : personne ne l'appelle.

## Scopes et appelants

Catalogue : `Security/Domain/ApiKeyScope.cs`. Chaque route ci-dessous accepte aussi un
utilisateur (même use case, jugé sur son rôle) : **TECH** pour les cinq premières.

| Scope | Route | Use case |
|---|---|---|
| `core.mail:send` | `POST /mail` (alias `/internal/mail`) | `SendMailUseCase` |
| `core.notifications:publish` | `POST /notifications` (alias `/internal/notifications`) | `SendNotificationUseCase` |
| `core.gameservers:sync` | `POST /internal/gameservers/sync` | `GameServersSyncUseCase` |
| `riot.valorant:sync` | `POST /internal/riot/valorant/sync` | `SyncValorantUseCase` |
| `temtem:sync` | `POST /internal/temtem/sync` | `SyncTemtemCatalogueUseCase` |
| `translation:translate` | `POST /translation/translate`, `GET /translation/languages`, `POST /translation/detect` | use cases du module Traduction (membres READ_ONLY côté utilisateur, pas TECH) |

### Appelants à migrer

| Appelant (NAS : `/data/docker/tools/…`) | Scopes de sa clé |
|---|---|
| `tools_riot_extractor/update_valorant.sh` | `riot.valorant:sync`, `core.notifications:publish`, `core.mail:send` |
| `tools_temtem_extractor/update_temtem.sh` | `temtem:sync`, `core.notifications:publish`, `core.mail:send` |
| `tools_gameserver_extractor/update_gameservers.sh` | `core.gameservers:sync` |
| `tools_dofus_extractor/update_data_doduda.sh` | `core.notifications:publish` |
| `tools_dofus_retro_extractor/update_data_retro.sh` | `core.notifications:publish` |
| API Java (`ApiCoreNotificationHttpAdapter`, en-tête à renommer) | `core.notifications:publish` |

Migrer un script = créer sa clé dans l'admin, la poser dans son `.env`, remplacer l'en-tête
`X-Internal-Token: $internal_api_token` par `X-Api-Key: …`, puis vérifier que `last_used_at` de
la clé avance et que plus aucun `LEGACY_USED` ne vient de lui. Les synchros Dofus et Palworld, encore
servies par l'API Java, gardent leur compte TECH : hors de ce module.

## Règles métier

- Une clé n'est jamais supprimée, seulement révoquée ; une révocation est définitive.
- Valide = ni révoquée ni expirée. Expiration obligatoire, au plus 3 ans après la création.
- Nom unique parmi les clés non révoquées.
- Un scope publié ne se renomme jamais (renommer = supprimer puis recréer, donc casser les clés).
- Un scope retiré du catalogue ne vaut plus rien ; la clé garde ses autres scopes et l'admin
  l'affiche barré (`UnknownScopes`).
- Modifier une clé n'existe pas : on en crée une nouvelle et on révoque l'ancienne.

## User stories

| Story | En tant que… | Droits |
|---|---|---|
| US-1 Créer une clé | admin : nom, description, expiration ≤ 3 ans, au moins un scope du catalogue ; clé en clair montrée une seule fois | ADMIN |
| US-2 Révoquer une clé | admin : refus immédiat de la clé, qui reste visible parmi les révoquées | ADMIN |
| US-3 Lister les clés | admin : préfixe, scopes (inconnus signalés), expiration, dernière utilisation et IP, créateur | ADMIN |
| US-4 Lister les scopes | admin : le catalogue du code, pour choisir à la création | ADMIN |
| US-5 Appeler l'API | service : exécuter les use cases dont la clé porte le scope, chaque passage tracé | la clé |

## Modèle de données

Migrations : `V2.79.0__api_keys.sql`, `V2.79.1__api_keys_description_expiration.sql`.

### `tools_core.api_keys`

| Colonne | Type | Null | Rôle |
|---|---|---|---|
| `id` | `BIGSERIAL` | non | Clé primaire. |
| `name` | `VARCHAR(100)` | non | Nom affiché dans l'administration. |
| `description` | `VARCHAR(500)` | non | À quoi sert la clé et qui l'utilise. |
| `key_hash` | `BYTEA` | non | SHA-256 de la clé complète (32 octets). |
| `key_prefix` | `VARCHAR(32)` | non | 12 premiers caractères en clair. |
| `created_at` | `TIMESTAMPTZ` | non | Création. |
| `created_by` | `BIGINT` | non | Administrateur créateur → `tools_core.users`. |
| `expires_at` | `TIMESTAMPTZ` | non | Fin de validité. |
| `revoked_at` | `TIMESTAMPTZ` | oui | Révocation ; `NULL` tant que la clé est active. |
| `last_used_at` | `TIMESTAMPTZ` | oui | Dernière utilisation (rafraîchie au plus une fois par minute). |
| `last_used_ip` | `INET` | oui | IP de la dernière utilisation. |

| Contrainte | Garantit |
|---|---|
| `uq_api_keys_key_hash` | Un hash = une clé ; c'est l'index cherché à chaque appel. |
| `ck_api_keys_key_hash_length` | Hash de 32 octets exactement. |
| `ck_api_keys_expiration_window` | `created_at < expires_at <= created_at + 3 ans`. |
| `uq_api_keys_active_name` | Nom unique parmi les clés non révoquées. |
| `fk_api_keys_created_by` | Créateur existant ; `ON DELETE RESTRICT`. |

### `tools_core.api_key_scopes`

| Colonne | Type | Null | Rôle |
|---|---|---|---|
| `api_key_id` | `BIGINT` | non | → `tools_core.api_keys`, `ON DELETE CASCADE`. |
| `scope` | `VARCHAR(100)` | non | Code du scope, forme `ressource:action` en minuscules (CHECK). |

Clé primaire `(api_key_id, scope)`.

## Routes d'administration

Bruno : `Tools API Core/Core/API Keys` (« 2. Créer une clé » pose `apiKeyValue`, que les requêtes
de service envoient dans `X-Api-Key`).

| Méthode | Route | Story |
|---|---|---|
| `GET` | `/admin/api-keys/scopes` | US-4 |
| `POST` | `/admin/api-keys` | US-1 |
| `GET` | `/admin/api-keys` | US-3 |
| `POST` | `/admin/api-keys/{id}/revoke` | US-2 |

## Où est le code

| Quoi | Fichier |
|---|---|
| Clé, invariants, validité | `Domain/ApiKey.cs` |
| Génération et hash | `Domain/ApiKeySecret.cs` |
| Catalogue des scopes | `Security/Domain/ApiKeyScope.cs` |
| Vérification, dernière utilisation, journal | `Application/Services/ApiKeyAuthenticator.cs` |
| Lecture de l'en-tête, ancien secret | `Infrastructure/ApiKeyAuthenticationHandler.cs` |
| Choix JWT ou clé | `Auth/Infrastructure/Jwt/JwtAuthenticationExtensions.cs` (`SelectScheme`) |
| Contrôle du scope | `Security/Application/Services/UseCaseAuthorizer.cs`, `SecuredUseCase.RequiredScope` |
| Front | `web/src/modules/Admin/apiKeys/` |

Règles de sécurité générales (rôles, refus par défaut, `CurrentUser` interdit aux clés) :
`api/docs/SECURITY.md`, section « Clés d'API ».

## Décisions (11/11/2026)

- **SHA-256, pas bcrypt** : une clé aléatoire ne se devine pas ; un hash déterministe se cherche par index.
- **Scopes en texte, catalogue dans le code**, sans clé étrangère : la base ne connaît pas le catalogue.
- **Le scope suffit** : une clé n'appartient à aucun module.
- **Contrôle dans le use case**, jamais dans le contrôleur, comme pour les rôles.
- **Un use case ouvert aux clés reste ouvert aux utilisateurs** : les jumeaux
  `SendInternalMailUseCase` / `PublishInternalNotificationUseCase` ont été fusionnés. Le mode
  « clé seulement » n'est pas écrit tant qu'aucun use case n'en a besoin ; ce sera une
  déclaration explicite, jamais `RequiredRole => null`.
- **Invariants dans le constructeur** (vérifiés aussi à la relecture) ; « expiration dans le
  futur » reste propre à la création, une clé relue pouvant être expirée.
- **Journal au départ de la réponse**, pour qu'un refus de scope ne soit pas compté comme un succès.
- **Ancien secret toléré** pendant la migration, avec tous les scopes, plutôt qu'une bascule
  d'un coup qui casserait tous les appelants.
- **Sans identifiant, `/internal/*` répond `401`** et non plus `404` : ces routes suivent la
  règle commune.
