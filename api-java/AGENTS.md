Projet : Tools API v3 (Mini-ERP Backend)

1. Mission & Stack

Rôle : Cœur logique du Mini-ERP.

Stack : Java 21, Spring Boot, JDBC (JdbcTemplate — pas d'ORM Hibernate).

Architecture : DDD Strict.

Domain : Invariants métier, Entités, Value Objects (0 dépendance externe).

Application : Orchestration des cas d'utilisation (Services applicatifs).

Infrastructure : Implémentations techniques (Persistence via PostgresXxxRepository).

API : Contrôleurs REST / DTOs.

2. Conventions absolues à respecter

- Pas de commentaires dans le code sauf si la raison est non évidente.
- Pas de Hibernate / JPA — uniquement JdbcTemplate avec des RowMapper statiques.
- Pas de Flyway/Liquibase — le schéma BDD est géré manuellement.
- Chaque use case est un @Service séparé implémentant SecuredUseCase.
- Les DTOs de réponse sont des records Java ou des classes simples (pas d'annotations Jackson).
- Les exceptions métier étendent IllegalArgumentException (→ 400 via ApiSecurityExceptionHandler).
- Ownership vérifiée côté use case (existsByIdAndUserId) ET dans le SQL (défense en profondeur).
- Utiliser RETURNING id pour récupérer l'ID généré après INSERT (pattern queryForObject).
- Les RowMapper sont des champs static final dans le repository.

3. Patterns observés

INSERT avec ID retourné :
  jdbcTemplate.queryForObject(sql_avec_RETURNING_id, Long.class, ...params)

Existence check ownership :
  workshopRepository.existsByIdAndUserId(userId, entityId)

Grouper des entités en une requête (éviter N+1) :
  jdbcTemplate.query(sql, rs -> { map.computeIfAbsent(...).add(...); }, params)

Gestion des erreurs JSON (enum invalide, etc.) :
  ApiSecurityExceptionHandler.handleNotReadable → message clair avec valeurs acceptées

4. Module Dofus — Workshop

Agrégat racine : Workshop (id, name, active, pinned, links)
Entités enfant : WorkshopItem, WorkshopItemIngredient, WorkshopTag, WorkshopLink

Table BDD cibles : tools_dofus.workshop, tools_dofus.workshop_item,
  tools_dofus.workshop_item_ingredient, tools_dofus.workshop_tag,
  tools_dofus.workshop_has_tag, tools_dofus.workshop_link

5. Feature Workshop Links — COMPLÈTE (2026-05-03)

Domaine : WorkshopLink (id, source, url, label, createdAt) dans l'agrégat Workshop.
Enum LinkSource : DOFUSBOOK, CUSTOM.

Architecture validation URL — extensible par source :
  interface LinkSourceHandler { source(), validateAndResolveLabel(url), default validate(url) }
  Implémenter @Service LinkSourceHandler → injecté automatiquement dans WorkshopLinkMetadataResolver.
  Handlers existants : DofusbookLinkSourceHandler, CustomLinkSourceHandler.

Formats Dofusbook acceptés :
  - https://d-bk.net/fr/d/{code}                              → "Dofus Book {code}"
  - https://www.dofusbook.net/fr/equipement/{id}-{slug}/objets → "Dofus Book {id|slug}"
  - https://www.dofusbook.net/fr/equipement/private/{id}-{slug}/objets → "Dofus Book {slug}"

Règles métier :
  - Max 3 liens par atelier (verified dans AddWorkshopLinkUseCase).
  - Label auto-resolved à la création, libre à l'édition (PUT envoie url + label).
  - URL validée à la création ET à l'édition selon la source du lien.
  - Table : tools_dofus.workshop_link (à créer manuellement).

Routes :
  POST   /dofus/workshops/{id}/links       → 201 WorkshopLinkDto
  PUT    /dofus/workshops/{id}/links/{id}  → 200 WorkshopLinkDto
  DELETE /dofus/workshops/{id}/links/{id}  → 204

WorkshopDto et WorkshopDetailResponse exposent la liste links.
État : Backend complet, prêt pour intégration Front.

6. Module Riot — MIGRÉ sur l'API C# (2026-08-29)

  Le module entier (Valorant + sync) vit désormais dans `api/Modules/Riot/`, et le front l'appelle
  par `clientCore`. Tout ce qui le servait ici a été supprimé : `modules/riot/`, `config/riot/`
  (RiotConfig et RiotSyncConfig) et les tests associés — 119 fichiers.

  Seul `ModuleCode.RIOT` reste : c'est l'énumération des droits, partagée avec la base et
  l'administration, elle ne décrit pas qui sert le module.

  Détail de la migration et décisions : `api/docs/ARCHITECTURE.md`, section « Riot, deuxième
  module métier ».

7. Module Admin — Gestion utilisateurs & stats

Nouvelles routes (toutes requièrent RoleCode.ADMIN minimum) :
  GET  /users                  → List<UserAdminView> (id, email, name, active, createdAt, avatarUrl, roles[id])
  GET  /users/{userId}         → UserProfileDto (id, email, name, userType, active, roles[], modules[])
  PUT  /users/{userId}/role    → 204 — body : { "roleId": Long }
  GET  /admin/stats            → AdminStatsView (totalUsers, activeUsers, newUsersThisWeek, usersPerModule[])

Architecture :
  - AdminUserController (/users) + AdminStatsController (/admin).
  - Use cases : ListUsersUseCase, GetUserDetailUseCase, SetUserGlobalRoleUseCase, GetAdminStatsUseCase.
  - AdminStatsRepository port + PostgresAdminStatsRepository (3 requêtes SQL distinctes).
  - Config : AdminConfig wire PostgresAdminStatsRepository.

Modifications des repositories existants :
  - UserRepository + PostgresUserRepository : findAllForAdmin() — JOIN users + user_role + role + user_auth_provider (avatarUrl Google) en une requête, pattern ResultSetExtractor LinkedHashMap.
  - UserRoleRepository + PostgresUserRoleRepository : deleteAllByUserId() — utilisé par SetUserGlobalRoleUseCase (delete + insert = remplacement atomique du rôle global).

SetUserGlobalRoleUseCase : valide que user ET role existent, puis replace le rôle global (deleteAll + save).
  - Prend roleId (Long) en body, pattern identique à ChangeUserModuleRoleRequest.

Notes sécurité :
  - @RequiredRole on controllers is decorative (no interceptor reads it).
  - True security is enforced by UseCaseAuthorizationAspect (intercepts execute()).
  - Spring Security blocks anonymous before reaching use cases (.anyRequest().authenticated()).

8. Sécurité — Hiérarchie des rôles (à jour)

Fichier : modules/core/security/infrastructure/RoleHierarchy.java
Ordre actuel (du plus bas au plus haut) :
  READ_ONLY (1) < USER (2) < MODERATOR (3) < TECH (4) < ADMIN (5) < OWNER (6)

ADMIN est au-dessus de TECH. OWNER n'est requis par aucun use case actuellement.
@RequiredRole sur les controllers est décoratif — seul UseCaseAuthorizationAspect enforce réellement.
Spring Security bloque les anonymes (.anyRequest().authenticated()) avant d'atteindre les use cases.

9. Module Admin — Routes complètes (à jour 2026-05-08)

  GET  /users                      → List<UserAdminView> (id, email, name, active, createdAt, avatarUrl, roles[Long])
  GET  /users/{userId}             → UserProfileDto (id, email, name, userType, active, roles[], modules[])
  PUT  /users/{userId}/role        → 204 — body : { "roleId": Long } — remplace le rôle global
  GET  /admin/stats                → AdminStatsView (totalUsers, activeUsers, newUsersThisWeek, usersPerModule[])
  GET  /modules/{moduleId}/users   → List<ModuleUserView> (userId, email, name, roleId, roleCode)

UserAdminView : classe simple, roles = List<Long> (IDs), avatarUrl via LEFT JOIN user_auth_provider GOOGLE.
ModuleUserView : classe simple, une ligne par user, RowMapper simple (pas de N+1, 1 role par user par module).
UserModuleRoleRepository.findAllByModuleId() : JOIN user_module_role + users + role WHERE module_id = ?

9a. Module Palworld — Présence des bases, guildes et joueurs (2026-08-15)

  Problème corrigé : l'import n'a jamais signalé les entités disparues du snapshot. Seuls les
  pals disposaient d'un `is_present` (`markMissingPalsAsNotPresent`) ; une base détruite en jeu,
  une guilde dissoute ou un personnage supprimé restaient affichés indéfiniment sur la carte et
  dans la liste latérale. Vérifié sur les snapshots réels : l'extracteur ne renvoie que ce qui
  existe (9 bases disparues entre le 26/07 et le 15/08), le trou était bien à l'import.

  Migration : `V2.66.0__palworld_presence_flags.sql` — colonne `is_present BOOLEAN NOT NULL
  DEFAULT TRUE` sur `base`, `guild` et `player`, index partiels, et reprise de l'existant.

  **Marquer et non supprimer** : `pal_instance_snapshot.base_id` référence les bases pour tout
  l'historique des pals, y compris ceux qui vivent aujourd'hui ailleurs. Une suppression
  effacerait leur passé sans rien changer à l'écran. Supprimer les pals d'une base disparue
  serait également faux — ils ont le plus souvent été déplacés, et l'upsert les repositionne
  seul (leur `instance_id` est la clé primaire, aucun doublon possible).

  Import (`PostgresServerDataRepository.importSnapshot`) : `markMissingBasesAsNotPresent`,
  `markMissingGuildsAsNotPresent` et `markMissingPlayersAsNotPresent` sont appelées **après**
  les upserts — les entités du snapshot portent alors `extractedAt`, celles qui gardent une date
  antérieure ont disparu. Les trois upserts remettent `is_present = TRUE` afin qu'une entité qui
  réapparaît redevienne visible.

  Reprise de l'existant : le repère est `MAX(extracted_at)` du journal d'imports, jamais `now()`.
  Si aucun snapshot n'arrive pendant plusieurs jours (serveur éteint, extracteur arrêté), la
  carte conserve le dernier état connu au lieu de se vider. `COALESCE` couvre la base neuve, sans
  aucun import.

  Lectures (`PostgresGuildQueryRepository`) : les trois requêtes filtrent sur `is_present`.
  `pal_count` était déjà calculé et exposé — le frontend l'affiche désormais dans la colonne des
  calques et dans l'infobulle des bases.

9b. Module Palworld — Breeding — COMPLÈTE (2026-08-05)

  Source de vérité : les 3 champs de breeding sur `pal` (`combi_rank`, `combi_duplicate_priority`, `ignore_combi`,
  alimentés par le sync depuis `pals.json`) + la table `tools_palworld.breeding_exception` (alimentée depuis
  `breeding.json`). **Aucune table de toutes les paires n'est persistée.**

  Routes :
    GET /palworld/breeding/result?parentA={palId}&parentB={palId}&genderA=&genderB=
      → BreedingResultView { parentA, parentB, child, rule: "exception"|"formula", formula, exception } (READ_ONLY)
    GET /palworld/breeding/parents?child={palId}
      → List<BreedingParentPairView> { parentA, parentB, parentAGender, parentBGender, rule, formula } (READ_ONLY)
    parentA/parentB/child = pal.id numérique (le même que /palworld/pals), pas la tribe interne.

  Moteur (0 dépendance, `modules/palworld/domain/breeding/`) :
    BreedingEngine.compute(parentA, genderA, parentB, genderB, exceptions, allPals) — vérifie les exceptions
    AVANT la formule (`targetRank = floor((rankA+rankB+1)/2)`, plus proche combiRank parmi les Pals
    ignoreCombi=false, départage par combiDuplicatePriority le plus élevé). Le matching d'exception teste les
    deux permutations (A,B) et (B,A) — parentA/parentB ne sont que des étiquettes d'appel, pas un ordre figé.
    BreedingIndexBuilder.buildAll(allPals, exceptions) — construit toutes les paires non-ordonnées (~48k pour
    309 espèces) à la demande, utilisé par GetBreedingParentsUseCase à chaque appel.

  **Décision importante — PAS de cache/index précalculé.** Une première version construisait un index en
  mémoire au démarrage (`ApplicationReadyEvent`) et après chaque sync — supprimée : ça faisait planter tout
  le boot de l'API si Postgres était injoignable (aucun autre composant du module ne fait ça), et le calcul
  complet des ~48k paires est sub-seconde en JVM (mesuré via les tests), donc inutile de le cacher. Si un futur
  agent est tenté de réintroduire un cache "pour la perf", vérifier d'abord que c'est réellement nécessaire.

  Sync (`modules/palworld/sync/`, chaîné dans SyncPalworldUseCase après SyncPalsUseCase) :
    SyncBreedingExceptionsUseCase lit breeding.json, résout les tribes en pal.id, **ignore+logue** (ne fait
    pas échouer le sync) les références introuvables — 3 connues sur les vrais assets : WindChimes,
    WindChimes_Ice, Blueplatypus (aucun équivalent pak). Le sync entier fait ~5900 requêtes SQL individuelles
    non-batchées (pattern préexistant, pas introduit par breeding — breeding n'ajoute que ~258 inserts, ~4%
    du total) : invisible sur le LAN maison, mais devient 10-20x plus lent via un tunnel SSH distant — comportement
    accepté par l'utilisateur, ne pas "optimiser" ça sans qu'il le demande.

  Tests : `api/src/test/java/.../palworld/domain/breeding/BreedingEngineTest.java` (260 tests, fixtures réelles
  dans `api/src/test/resources/palworld/`) — formule+départage, exceptions fixes, exceptions sexe-dépendantes,
  chaque ligne valide de breeding.json, intégrité des références invalides connues, cohérence calcul
  direct ↔ index sur l'intégralité des paires.

  **Pas encore fait** : variante "Pals possédés" — prévue par l'utilisateur mais explicitement hors
  scope pour l'instant. (La colonne `gender` de `pal_instance`, annoncée absente ici jusqu'au
  29/08/2026, existe bien : elle est lue par `PostgresServerInventoryQueryRepository` et exposée
  dans `ServerPalInventoryView.gender`.) Voir aussi la spec frontend
  dans `web/AGENTS.md` (page Breeding Calculator, pas commencée).

9c. Module Palworld — Format des guildId direct ↔ snapshot (2026-08-16, code déplacé le 29/08/2026)

  **Le direct et le persisté n'écrivaient pas le même GUID de la même façon.** `/v1/api/game-data`
  renvoie `8F05C04606C64CB3B895E84AD4E9D13D` (hexadécimal majuscule, sans tirets) là où l'extracteur
  de snapshots — donc `tools_palworld.guild.guild_id`, colonne `UUID` — porte
  `8f05c046-06c6-4cb3-b895-e84ad4e9d13d`. Même guilde, deux chaînes : tout rapprochement entre les
  deux sources échouait silencieusement.

  Symptômes observés côté carte : un joueur connecté et ses propres bases recevaient deux couleurs
  différentes (la palette est indexée par `guildId`) ; la fusion base direct / base snapshot ne
  s'accrochait jamais, donc aucune base du direct n'héritait de son `palCount` et le filtre censé
  écarter les bases détruites les supprimait toutes ; les noms de joueurs en ligne n'étaient jamais
  rattachés à une base.

  **Le dashboard serveur a quitté cette API le 29/08/2026** (voir le Discovery Log) : la
  normalisation vit désormais côté front, dans `palworldMapAdapter.ts`, qui apparie les
  identifiants du direct avec ceux du snapshot. Le piège reste entier, seul l'endroit change.

  Correction d'origine : `PalworldRestAdapter.toCanonicalGuildId` normalisait le `GuildID` en UUID
  canonique aux trois sorties du direct (joueurs, bases, pals de base). La fonction est idempotente — un identifiant
  déjà canonique ressort inchangé — et laisse passer tel quel ce qui n'est pas 32 caractères
  hexadécimaux (identifiants du `PalworldMockAdapter`). **La normalisation appartient à
  l'infrastructure** : le reste de l'application n'a pas à connaître les conventions d'écriture de
  l'API du jeu, et le frontend n'a rien eu à changer.

  Vérifié en production sur un joueur connecté : les trois bases du direct correspondent aux trois
  bases du snapshot à moins d'un centième d'unité de distance — l'appariement par coordonnées était
  correct depuis le début, seul le `guildId` bloquait.

  À retenir : `userId` souffre du même écart (le direct renvoie `gdk_...` / `steam_...`, le snapshot un
  UUID de sauvegarde) mais il s'agit là de deux identités réellement distinctes, pas d'un formatage —
  aucune conversion n'est possible, ne pas tenter de les rapprocher par ce biais.

10. Discovery Log

[Architecture] Initialisation du squelette DDD Java 21.
[Feature] Workshop Links — backend complet (voir section 5).
[Feature] Riot/Valorant refresh token — backend complet (voir section 6a).
[Feature] Admin routes (users + stats + module users) — backend complet (voir sections 7 et 9).
[Sécurité] Hiérarchie rôles inversée ADMIN/TECH — ADMIN (5) > TECH (4) (voir section 8).
[Feature] Riot/Valorant skins + my-skins + watchlist — backend complet (voir section 6b).
[Refactor] Riot/Valorant sync déplacé de modules/riot/valorant/sync/ vers modules/riot/sync/.
[Feature] Riot/Valorant sync refactorisé — source locale (assets NAS) au lieu de valorant-api.com (voir section 6e).
[Feature] Riot/Valorant sync generalisé — SyncValorantUseCase appelle skins + bundles (voir section 6e).
[Feature] Riot/Valorant skin levels — sync + exposition dans ValorantSkinView + route by-level (voir sections 6b, 6e).
[Feature] Riot/Valorant bundles — sync + routes GET (voir sections 6c, 6e).
[Feature] Riot/Valorant version — GET /riot/valorant/version depuis version.json local (voir section 6d).
[Feature] Riot/Valorant weapons — table + sync (weapons en premier, FK weapon_id sur skins) + routes GET (voir sections 6e, 6f).
[Feature] Riot/Valorant skins — routes additionnelles : by-asset, by-theme, {id} (voir section 6b).
[Feature] Riot/Valorant store history — CRUD complet & routes API (voir section 6g).
[Refactor] Réorganisation du module Valorant application en sous-packages (core, catalog, user) (2026-05-10).
- core/ : Auth et Version.
- catalog/ : Armes, Skins et Bundles (données Riot).
- user/ : Skins possédés, Watchlist et Historique boutique.
- Suppression des dossiers à plat usecase/, command/, ports/, view/.
[Feature] Palworld/Breeding — moteur de reproduction, endpoints result/parents, sync breeding.json (voir section 9b). Pas de cache précalculé (décision motivée). Frontend pas commencé, spec dans web/AGENTS.md.
[Fix] Palworld — guildId du direct normalisé en UUID canonique dans PalworldRestAdapter : le direct et les snapshots écrivaient le même GUID différemment (voir section 9c).

11. Module Notifications — MIGRÉ sur l'API Core (à jour 2026-08-17)

**Tout le module est passé côté C#** : écriture, résolution des destinataires, push temps réel
(SignalR), et depuis le 2026-08-17 la lecture et la gestion (liste, marquage lu, suppression,
envoi manuel). Les tables ne bougent pas — `tools_core.notifications` et
`tools_core.user_notifications` restent la source, le Core les lit et les écrit désormais seul.

Ce qui reste ici, et qui doit y rester : **l'émission d'événements métier**. Les use cases Java
publient toujours un `NotificationEvent` (`eventPublisher.publishEvent(...)`), et
`NotificationEventListener` le relaie à l'API Core via `ApiCoreNotificationPort` →
`POST /internal/notifications` (fail-open : une notification manquée ne fait jamais échouer le
flux métier appelant).

Sécurité (côté Core maintenant) :
- Role TECH exclu systématiquement de tous les destinataires d'un envoi.
- Lecture/marquage/suppression : READ_ONLY. Envoi manuel : TECH.

**Le code devenu mort a été supprimé (2026-08-17)** : `NotificationController` et ses quatre
routes, `/notifications/stream` (SSE) et `SseNotificationService`, les cinq use cases de
lecture/écriture, `NotificationRepository`/`PostgresNotificationRepository`, `NotificationView`,
et les entités `Notification`/`UserNotification`. Les entrées Bruno correspondantes
(`Tools API v3/Core/Notification/`) ont disparu avec elles — **l'API Java ne sert plus aucune
route `/notifications`**.

Il ne subsiste que le chemin d'émission, qui n'a pas d'équivalent ailleurs :
`NotificationEvent`, `NotificationEventListener` (`@Component` + `@EventListener` : aucun appel
direct, ne pas le croire orphelin sur la foi d'une recherche textuelle), `ApiCoreNotificationPort`
et son adaptateur HTTP, `NotificationType` (utilisé par Feedback, Almanax et Valorant) et
`NotificationConfig`, réduit au seul bean `apiCoreNotificationPort`.

Les routes du Core sont dans `bruno/Tools API Core/Notifications/`.

⚠️ **Ordre de déploiement** : l'image web (front sur `clientCore`) doit partir **avant ou en même
temps** que cette image Java. Déployée seule, l'API Java retire des routes que le front en
production appelle encore — neutraliser Watchtower sur `tools_api_java` le temps que `tools_web`
soit à jour, comme lors de la mise en production du 15/08.

## Module Riot/Valorant — retiré le 29/08/2026

Migré sur l'API C# (`api/Modules/Riot/`). Voir la section 6.

## Module Palworld — Dashboard serveur retiré (2026-08-29)

Le dashboard serveur est passé sur l'API C# (`Core/GameServers`), sous forme d'une popup ouverte
depuis le widget des serveurs de jeux. Tout ce qui le servait ici a été supprimé : les 12 routes
`/palworld/server/*` de `PalworldServerController`, leurs use cases, commandes et vues,
`PalworldServerPort`, `PalworldRestAdapter`, `PalworldMockAdapter`, `PalworldCoord`,
`PalworldConfig`, la propriété `palworld.api.base-url` des trois profils, et le dossier Bruno
`Tools API v3/Palworld/Server/`. **Cette API n'expose plus aucune route `/palworld/server/*`.**

**Ce qui reste et ne doit pas être confondu avec ça** : tout `serverdata/`
(`/palworld/server-data/*`), qui importe les snapshots de l'extracteur toutes les 5 minutes et
alimente l'élevage, le Paldex, « Mes Pals » et la carte du nouveau dashboard. Vérifié avant
suppression : aucun script du NAS n'appelle `/palworld/server/*` — les seules routes qu'ils
utilisent sont `palworld/sync`, `dofus/*/sync`, `riot/valorant/sync` et `notifications`.
