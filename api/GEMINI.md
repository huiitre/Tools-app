Projet : Tools API (Mini-ERP Backend)

1. Mission & Stack

Rôle : Cœur logique du Mini-ERP.

Stack : Java 21, Spring Boot (ou Quarkus selon config).

Architecture : DDD Strict.

Domain : Invariants métier, Entités, Value Objects (0 dépendance externe).

Application : Orchestration des cas d'utilisation (Services applicatifs).

Infrastructure : Implémentations techniques (Persistence, Clients API).

API : Contrôleurs REST / DTOs.

Config : Configuration framework.

2. Protocole d'Initialisation (OBLIGATOIRE)

Utiliser @Google Drive pour lire le fichier INDEX_PROJETS dans le dossier Projets AI.

Scanner le contenu du sous-dossier /Tools.

Confirmer que le contexte est à jour avant toute réponse.

3. Règles de Développement

DDD : Bloquer systématiquement toute fuite de responsabilité entre les couches.

Charbon : Rigueur pédagogique absolue dans les explications techniques.

Pragmatisme : Code prêt à l'emploi pour environnement Linux/Docker.

4. Stack réelle (à jour)

Stack : Java 21, Spring Boot, JDBC (JdbcTemplate — pas d'ORM Hibernate).
Pas de Flyway/Liquibase — schéma BDD géré manuellement.
Chaque use case est un @Service implémentant SecuredUseCase (AOP intercepte execute()).
Sécurité : Spring Security (.anyRequest().authenticated()) + UseCaseAuthorizationAspect.
@RequiredRole sur les controllers est décoratif — la vraie sécurité est dans les use cases.

Hiérarchie des rôles (RoleHierarchy.java) :
  READ_ONLY (1) < USER (2) < MODERATOR (3) < TECH (4) < ADMIN (5) < OWNER (6)
  ADMIN est au-dessus de TECH. OWNER n'est utilisé dans aucun use case actuellement.

5. Discovery Log

[Architecture] Initialisation du squelette DDD Java 21.

[Feature] Workshop Links — COMPLÈTE.
- WorkshopLink (id, source, url, label, createdAt) dans l'agrégat Workshop.
- Enum LinkSource : DOFUSBOOK, CUSTOM. Extensible via @Service LinkSourceHandler.
- Validation URL par source, label auto-résolu à la création, libre à l'édition.
- Limite : 3 liens max par atelier.
- Table : tools_dofus.workshop_link.
- Routes : POST/PUT/DELETE /dofus/workshops/{id}/links[/{id}].

[Feature] Riot/Valorant refresh token — COMPLÈTE.
- Route : POST /riot/valorant/refresh-token → { accessToken, refreshToken }.
- client_id hardcodé : prod-xsso-playvalorant (pas de secret).
- Appel POST form-urlencoded vers auth.riotgames.com/token.
- ModuleCode.RIOT ajouté à l'enum ModuleCode.
- Config : RiotConfig, adapter : RiotAuthHttpAdapter.

[Feature] Riot/Valorant skins — COMPLÈTE (2026-05-09).
- GET  /riot/valorant/skins                          → List<ValorantSkinView> avec levels[] (READ_ONLY).
- GET  /riot/valorant/skins/{id}                     → ValorantSkinView (READ_ONLY).
- GET  /riot/valorant/skins/by-asset/{assetId}       → ValorantSkinView (READ_ONLY).
- GET  /riot/valorant/skins/by-level/{levelAssetId}  → ValorantSkinView (READ_ONLY).
- GET  /riot/valorant/skins/by-theme/{themeUuid}     → List<ValorantSkinView> (READ_ONLY).
- GET  /riot/valorant/my-skins                       → List<ValorantUserSkinView> (READ_ONLY).
- POST /riot/valorant/my-skins                       → 201 — body : { "skinId": Long } (USER).
- DELETE /riot/valorant/my-skins/{skinId}            → 204 (USER).
- GET  /riot/valorant/watchlist                      → List<ValorantWatchlistEntryView> (READ_ONLY).
- POST /riot/valorant/watchlist                      → 201 — body : { "skinId": Long } (USER).
- DELETE /riot/valorant/watchlist/{skinId}           → 204 (USER).
- Tables BDD : tools_riot.valorant_weapon_skins (+ weapon_id FK) + tools_riot.valorant_skin_levels.
- ValorantSkinView : (id, assetId, name, iconUrl, tierUuid, contentTierUuid, weaponId, levels[]).
- by-theme/{themeUuid} = tous les skins d'une collection (tier_uuid en base = themeUuid Riot).
- Ports : ValorantSkinRepository (findAll, findById, findByAssetId, findByLevelAssetId,
    findAllByWeaponId, findAllByTierUuid), ValorantUserSkinRepository, ValorantWatchlistRepository.

[Feature] Riot/Valorant sync — COMPLÈTE (2026-05-09, étendu armes).
- Route : POST /riot/valorant/sync → ValorantGlobalSyncReport { weapons, skins, bundles } (TECH + RIOT).
- Ordre : weapons → skins (avec weaponAssetIdToDbId map) → bundles.
- SyncValorantWeaponsUseCase retourne ValorantWeaponSyncResult (report + Map<UUID,Long>).
- SyncValorantSkinsUseCase.execute(Map<UUID,Long>) résout weapon_id par skin, inclus dans détection changement.
- Sources locales :
    ValorantLocalWeaponDataProvider lit weapons.json → itère data[] (strip EEquippableCategory::).
    ValorantLocalSkinDataProvider lit weapons.json → itère data[].skins[] (passe weaponAssetId).
    ValorantLocalBundleDataProvider lit bundles.json.
    ValorantLocalAssetsReader (@Component) lit depuis {assets.base-path}/tools_riot/valorant/.
  Fallback skins : ValorantApiSkinDataProvider (pointer RiotSyncConfig pour switcher).
- Assets locaux :
    img/weapons/{uuid}/displayicon.png
    img/weaponskins/{uuid}/displayicon.png
    img/weaponskinlevels/{uuid}/displayicon.png
    img/bundles/{uuid}/displayicon2.png
- Config : RiotSyncConfig (séparé de RiotConfig).

[Feature] Riot/Valorant weapons — COMPLÈTE (2026-05-09).
- GET /riot/valorant/weapons            → List<ValorantWeaponView> (READ_ONLY).
- GET /riot/valorant/weapons/{id}       → ValorantWeaponView (READ_ONLY).
- GET /riot/valorant/weapons/{id}/skins → List<ValorantSkinView> avec levels[] (READ_ONLY).
- Table : tools_riot.valorant_weapons (id, asset_id UUID, name, category, default_skin_asset_id UUID,
    display_icon_url, created_at, updated_at).
  category strippée du préfixe Unreal Engine (ex: "Rifle", "Heavy", "Sidearm").
  default_skin_asset_id : non FK pour éviter référence circulaire.
- Port : ValorantWeaponRepository (findAll, findById). Config : RiotConfig.

[Feature] Riot/Valorant bundles — COMPLÈTE (2026-05-09).
- GET /riot/valorant/bundles                    → List<ValorantBundleView> (READ_ONLY).
- GET /riot/valorant/bundles/{id}               → ValorantBundleView (READ_ONLY).
- GET /riot/valorant/bundles/by-asset/{assetId} → ValorantBundleView (READ_ONLY).
- Table : tools_riot.valorant_bundles (id, asset_id UUID, name, banner_url, created_at, updated_at).
- Port : ValorantBundleRepository (findAll, findById, findByAssetId). Config : RiotConfig.

[Feature] Riot/Valorant version — COMPLÈTE (2026-05-09).
- GET /riot/valorant/version → Map<String,Object> contenu de data dans version.json (READ_ONLY).
- Fournit riotClientVersion à injecter dans X-Riot-ClientVersion des appels storefront.
- Port : ValorantVersionProvider → ValorantLocalVersionProvider (lit version.json, extrait data).

[Feature] Admin — gestion utilisateurs, stats & module users — COMPLÈTE.
- GET  /users                    → List<UserAdminView> (id, email, name, active, createdAt, avatarUrl, roles[Long]).
- GET  /users/{userId}           → UserProfileDto complet (roles + modules).
- PUT  /users/{userId}/role      → body { "roleId": Long } — remplace le rôle global.
- GET  /admin/stats              → { totalUsers, activeUsers, newUsersThisWeek, usersPerModule[] }.
- GET  /modules/{moduleId}/users → List<ModuleUserView> (userId, email, name, roleId, roleCode).
- UserRepository.findAllForAdmin() : JOIN users + user_role + role + user_auth_provider en 1 requête.
- UserRoleRepository.deleteAllByUserId() : remplacement atomique du rôle global.
- UserModuleRoleRepository.findAllByModuleId() : inverse de findAllByUserId.
- Config : AdminConfig wire PostgresAdminStatsRepository.

[Feature] Riot/Valorant store history — Archivage en batch (2026-05-10).
- GET  /riot/valorant/store-history  → List<ValorantStoreHistoryView> (READ_ONLY).
- POST /riot/valorant/store-history  → 201 — body : { "skinIds": List<Long>, "seenAt": LocalDate } (USER).
- Logique Batch : Archivage atomique des 4 skins quotidiens.
- Stabilisation Date : Calcul basé sur le midpoint de la rotation (Expiration - 12h) côté client.
- Restructuration API : Le UseCase agrège désormais les objets `ValorantSkinView` complets et groupe le retour par date décroissante.
- Table BDD : tools_riot.valorant_store_history (id, user_id, skin_id, seen_at).
- Repository : findAllRawByUserId retourne une Map d'IDs par date pour agrégation optimisée.

[Refactor] Réorganisation du module Valorant application en sous-packages (core, catalog, user) (2026-05-10).
- Structure par domaine fonctionnel au lieu de type technique.
- Mise à jour des packages et imports dans tout le module API.

[Sécurité] Inversion hiérarchie ADMIN/TECH — ADMIN (5) > TECH (4).
- Use cases modules (GET/POST/PUT/DELETE) abaissés de TECH à ADMIN.
- GetAllRolesUseCase abaissé de TECH à ADMIN.