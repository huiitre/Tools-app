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
  - Max 3 liens par atelier (vérifié dans AddWorkshopLinkUseCase).
  - Label auto-résolu à la création, libre à l'édition (PUT envoie url + label).
  - URL validée à la création ET à l'édition selon la source du lien.
  - Table : tools_dofus.workshop_link (à créer manuellement).

Routes :
  POST   /dofus/workshops/{id}/links       → 201 WorkshopLinkDto
  PUT    /dofus/workshops/{id}/links/{id}  → 200 WorkshopLinkDto
  DELETE /dofus/workshops/{id}/links/{id}  → 204

WorkshopDto et WorkshopDetailResponse exposent la liste links.
État : Backend complet, prêt pour intégration Front.

6. Module Riot — Valorant

6a. Auth
  POST /riot/valorant/refresh-token → 200 { accessToken, refreshToken }
  - Reçoit un refreshToken en body, appelle auth.riotgames.com/token.
  - client_id hardcodé : prod-xsso-playvalorant (public client, pas de secret).
  - Retourne le nouveau accessToken + refreshToken (null si Riot n'en émet pas de nouveau).
  - Use case requiert ModuleCode.RIOT + RoleCode.READ_ONLY.
  Adapter : RiotAuthHttpAdapter — POST form-urlencoded, ParameterizedTypeReference<Map<String,Object>>.
  Config : RiotConfig (aucune propriété externe, URL et client_id hardcodés).

6b. Skins — COMPLÈTE (2026-05-09)
  Routes :
    GET    /riot/valorant/skins                          → List<ValorantSkinView> avec levels[] (READ_ONLY)
    GET    /riot/valorant/skins/{id}                     → ValorantSkinView (READ_ONLY)
    GET    /riot/valorant/skins/by-asset/{assetId}       → ValorantSkinView (READ_ONLY)
    GET    /riot/valorant/skins/by-level/{levelAssetId}  → ValorantSkinView (READ_ONLY)
    GET    /riot/valorant/skins/by-theme/{themeUuid}     → List<ValorantSkinView> (READ_ONLY)
    GET    /riot/valorant/my-skins                       → List<ValorantUserSkinView> (READ_ONLY)
    POST   /riot/valorant/my-skins                       → 201 ValorantUserSkinView — body : { "skinId": Long } (USER)
    DELETE /riot/valorant/my-skins/{skinId}              → 204 (USER)
    GET    /riot/valorant/watchlist                      → List<ValorantWatchlistEntryView> (READ_ONLY)
    POST   /riot/valorant/watchlist                      → 201 ValorantWatchlistEntryView — body : { "skinId": Long } (USER)
    DELETE /riot/valorant/watchlist/{skinId}             → 204 (USER)

  Tables BDD :
    tools_riot.valorant_weapon_skins (id, asset_id UUID, name, icon_url, tier_uuid UUID, content_tier_uuid UUID,
                                      weapon_id FK → valorant_weapons.id)
    tools_riot.valorant_skin_levels  (id, skin_id FK, asset_id UUID, level_index INT, name, level_item,
                                      display_icon_url, streamed_video_url, created_at, updated_at)

  Note tier_uuid : stocke le themeUuid de l'API Riot (UUID du thème/collection), pas le tier de rareté.
    Le vrai tier de rareté est content_tier_uuid. Le lien skins↔bundle se fait via l'API Riot en live,
    pas en DB (Riot retourne les level UUIDs dans le storefront, on les résout via by-level).

  ValorantSkinView : (id, assetId, name, iconUrl, tierUuid, contentTierUuid, weaponId, levels[])
    - weaponId permet de relier directement un skin à son arme parente.
    - by-theme/{themeUuid} retourne tous les skins d'une même collection (themeUuid = tier_uuid en base).

  Ports : ValorantSkinRepository (findAll, findById, findByAssetId, findByLevelAssetId,
                                   findAllByWeaponId, findAllByTierUuid),
          ValorantUserSkinRepository, ValorantWatchlistRepository.
  Config : RiotConfig wire les repos Postgres.

6c. Bundles — COMPLÈTE (2026-05-09)
  Routes :
    GET /riot/valorant/bundles                    → List<ValorantBundleView> (READ_ONLY)
    GET /riot/valorant/bundles/{id}               → ValorantBundleView (READ_ONLY)
    GET /riot/valorant/bundles/by-asset/{assetId} → ValorantBundleView (READ_ONLY)

  Table BDD : tools_riot.valorant_bundles (id, asset_id UUID, name, banner_url, created_at, updated_at)
  Port : ValorantBundleRepository (findAll, findById, findByAssetId).
  Config : RiotConfig wire PostgresValorantBundleRepository.

6d. Version — COMPLÈTE (2026-05-09)
  Route :
    GET /riot/valorant/version → Map<String,Object> contenu de data dans version.json (READ_ONLY)

  Utilité : fournit riotClientVersion (ex: "release-09.08-shipping-28-2638874")
    à injecter dans le header X-Riot-ClientVersion des appels storefront Riot.
  Port : ValorantVersionProvider → ValorantLocalVersionProvider (lit version.json).
  Config : RiotConfig wire ValorantLocalVersionProvider(ValorantLocalAssetsReader).

6e. Sync — COMPLÈTE (2026-05-09)
  Route :
    POST /riot/valorant/sync → 200 ValorantGlobalSyncReport { weapons, skins, bundles } (TECH)

  Ordre d'exécution : weapons → skins (avec weaponAssetIdToDbId map) → bundles.

  Architecture (modules/riot/sync/) :
    api/         ValorantSyncController
    application/ SyncValorantUseCase          (point d'entrée — RIOT + TECH)
                 SyncValorantWeaponsUseCase    (RIOT + TECH — retourne ValorantWeaponSyncResult)
                 SyncValorantSkinsUseCase      (RIOT + TECH — prend Map<UUID,Long> weaponAssetIdToDbId)
                 SyncValorantBundlesUseCase    (RIOT + TECH)
                 ValorantWeaponDataProvider    (port — source armes)
                 ValorantSkinDataProvider      (port — source skins)
                 ValorantBundleDataProvider    (port — source bundles)
                 ValorantWeaponSyncRepository  (port DB armes)
                 ValorantSkinSyncRepository    (port DB skins — save/update prennent Long weaponId)
                 ValorantSkinLevelSyncRepository (port DB levels — deleteAll + save)
                 ValorantBundleSyncRepository  (port DB bundles)
                 ValorantWeaponSyncData
                 ValorantWeaponSyncResult      (record : ValorantSyncReport + Map<UUID,Long>)
                 ValorantSkinSyncData          (avec weaponAssetId + List<ValorantSkinLevelSyncData>)
                 ValorantBundleSyncData
                 ValorantSkinLevelSyncData
                 ValorantSyncReport / ValorantGlobalSyncReport
    infrastructure/
                 ValorantLocalAssetsReader     (@Component — lit depuis tools_riot/valorant/)
                 ValorantLocalWeaponDataProvider (lit weapons.json, itère data[], strip EEquippableCategory::)
                 ValorantLocalSkinDataProvider (lit weapons.json, itère data[].skins[], passe weaponAssetId)
                 ValorantLocalBundleDataProvider (lit bundles.json)
                 ValorantApiSkinDataProvider   (fallback — appelle valorant-api.com)
                 PostgresValorantWeaponSyncRepository
                 PostgresValorantSkinSyncRepository
                 PostgresValorantSkinLevelSyncRepository
                 PostgresValorantBundleSyncRepository

  Logique sync weapons :
    - Fetch weapons.json → itère data[] (les armes, pas les skins).
    - Compare avec DB (clé : asset_id). Crée / met à jour / supprime.
    - Retourne weaponAssetIdToDbId Map<UUID, Long> pour la sync skins.
    - category : strip préfixe "EEquippableCategory::" → stocke "Rifle", "Heavy", etc.
    - displayIconUrl : img/weapons/{uuid}/displayicon.png.

  Logique sync skins :
    - Fetch weapons.json → itère data[].skins[] (pas data[] qui sont les armes).
    - Compare avec DB (clé : asset_id). Crée / met à jour / supprime.
    - weapon_id résolu depuis weaponAssetIdToDbId à chaque skin.
    - Détection de changement inclut weaponId (null → FK = trigger update).
    - Après sync skins : deleteAll levels puis réinsère tous les levels de tous les skins.
    - skinAssetIdToDbId map trackée pendant la boucle pour éviter un round-trip DB.

  Logique sync bundles :
    - Fetch bundles.json → itère data[].
    - Compare avec DB (clé : asset_id). Crée / met à jour / supprime.

  Assets locaux (NAS) :
    Base : {tools.assets.base-path}/tools_riot/valorant/
    Fichiers JSON : weapons.json, bundles.json, version.json
    Images :
      img/weapons/{uuid}/displayicon.png
      img/weaponskins/{uuid}/displayicon.png
      img/weaponskinlevels/{uuid}/displayicon.png
      img/bundles/{uuid}/displayicon2.png
      img/weaponskinchromas/ (non utilisé actuellement)
    URL publique : {app.assets.base-url}/tools_riot/valorant/img/...
    Vidéos (streamedVideo) : URL CDN Riot conservée telle quelle, non téléchargée.

  Config : RiotSyncConfig (séparé de RiotConfig).

6f. Weapons — COMPLÈTE (2026-05-09)
  Routes :
    GET /riot/valorant/weapons              → List<ValorantWeaponView> (READ_ONLY)
    GET /riot/valorant/weapons/{id}         → ValorantWeaponView (READ_ONLY)
    GET /riot/valorant/weapons/{id}/skins   → List<ValorantSkinView> avec levels[] (READ_ONLY)

  Table BDD : tools_riot.valorant_weapons (id, asset_id UUID, name, category, default_skin_asset_id UUID,
                                           display_icon_url, created_at, updated_at)
    - category : valeur strippée du préfixe Unreal Engine (ex: "Rifle", "Heavy", "Sidearm").
    - default_skin_asset_id : UUID du skin par défaut (non FK pour éviter référence circulaire).

  ValorantWeaponView : (id, assetId, name, category, defaultSkinAssetId, displayIconUrl)
  Ports : ValorantWeaponRepository (findAll, findById). Config : RiotConfig.

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
  - @RequiredRole sur les controllers est décoratif (aucun interceptor ne le lit).
  - La sécurité réelle est assurée par UseCaseAuthorizationAspect (intercepte execute()).
  - Spring Security bloque les anonymes avant même d'atteindre les use cases (.anyRequest().authenticated()).

8. Sécurité — Hiérarchie des rôles (à jour)

Fichier : modules/core/security/infrastructure/RoleHierarchy.java
Ordre actuel (du plus bas au plus haut) :
  READ_ONLY (1) < USER (2) < MODERATOR (3) < TECH (4) < ADMIN (5) < OWNER (6)

ADMIN est au-dessus de TECH. OWNER n'est requis par aucun use case actuellement.
@RequiredRole sur les controllers est décoratif — seul UseCaseAuthorizationAspect enforce réellement.
Spring Security bloque les anonymes (.anyRequest().authenticated()) avant d'atteindre les use cases.

Routes accessibles à partir de ADMIN (minimum) :
  - Toute la gestion des modules (GET/POST/PUT/DELETE /modules, /modules/{id}/users, etc.)
  - Toute la gestion des users admin (/users, /admin/stats)
  - Synchro Dofus (TECH suffit, mais ADMIN passe aussi depuis l'inversion)

9. Module Admin — Routes complètes (à jour 2026-05-08)

  GET  /users                      → List<UserAdminView> (id, email, name, active, createdAt, avatarUrl, roles[Long])
  GET  /users/{userId}             → UserProfileDto (id, email, name, userType, active, roles[], modules[])
  PUT  /users/{userId}/role        → 204 — body : { "roleId": Long } — remplace le rôle global
  GET  /admin/stats                → AdminStatsView (totalUsers, activeUsers, newUsersThisWeek, usersPerModule[])
  GET  /modules/{moduleId}/users   → List<ModuleUserView> (userId, email, name, roleId, roleCode)

UserAdminView : classe simple, roles = List<Long> (IDs), avatarUrl via LEFT JOIN user_auth_provider GOOGLE.
ModuleUserView : classe simple, une ligne par user, RowMapper simple (pas de N+1, 1 role par user par module).
UserModuleRoleRepository.findAllByModuleId() : JOIN user_module_role + users + role WHERE module_id = ?

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
