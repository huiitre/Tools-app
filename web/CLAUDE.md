# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Development
npm run dev                  # Vite dev server (port 5173)
npm run dev:qa               # QA mode
npm run electron:watch       # Electron + Vite dev server with live reload

# Build
npm run build                # vue-tsc type-check + Vite production build
npm run electron:build       # Build Electron distributable (Linux AppImage, Windows NSIS)

# Lint
npm run lint:script          # ESLint on .ts/.vue files
npm run lint:markup          # Type-check via vue-tsc --noEmit

# Utilities
npm run preview              # Preview production build
npm run reset                # Clean node_modules + lock file
```

There is no test framework configured in this project.

## Architecture

**Electron + Vue 3** desktop/web app. The same frontend is deployed as a Docker web app and as a packaged Electron application. Vue Router uses hash history in Electron mode and HTML5 history in web mode — the `useEnv` composable and router guards handle the difference.

### Frontend (`src/`)

Feature modules live under `src/modules/`. Each module owns its routes (`*.routes.ts`), Pinia store (`*.store.ts`), fetch functions, and components. The main modules are:

- **Auth** — login/logout, token storage, refresh flow
- **Dofus** — game tooling with sub-features: `hdv/` (market sniffer), `bankmanagement/` (bank sniffer), `workshop/`, `catalogue/`, `almanax/`, `switcher/` (account switcher)
- **Riot** — intégration Riot Games, sous-module `valorant/` (daily shop)
- **Admin** — panel d'administration réservé aux rôles ADMIN / TECH / OWNER
- **Settings**, **Downloads**, **Legal**

### Mécanisme de Mise à jour (`src/services/update/`)

L'application utilise une abstraction `IUpdateService` pour gérer les mises à jour de manière transparente entre le Web et Electron. Le composable `useAppUpdate.ts` centralise l'état `updateAvailable`.

#### Web (PWA)
1.  **Détection** : Un polling est effectué toutes les 10 secondes via `navigator.serviceWorker.getRegistration().then(r => r?.update())`.
2.  **Notification** : Le `WebUpdateService` s'interface avec l'objet `pwa` (alimenté par le plugin Vite PWA/Workbox) via `pwa.onNeedRefresh`.
3.  **Application** : `applyUpdate()` récupère le Service Worker en attente (`waiting`) et lui envoie un message `{ type: 'SKIP_WAITING' }`.
4.  **Finalisation** : Une fois le worker activé (`statechange` -> `activated`), la page est rechargée via `window.location.reload()`.

#### Electron
1.  **Détection** : Dans `electron/main.cjs`, `autoUpdater.checkForUpdates()` est appelé au démarrage (hors mode dev).
2.  **Notification** : L'événement `update-available` d'electron-updater envoie un message IPC `update-available` au renderer.
3.  **Bridge** : Le `preload.cjs` expose `onUpdateAvailable` qui relaie l'événement au `ElectronUpdateService`.
4.  **Application** : `applyUpdate()` envoie l'IPC `apply-update` au processus principal.
5.  **Finalisation** : Le processus principal appelle `autoUpdater.quitAndInstall()`.

Shared infrastructure:
- `src/services/axiosInstance.ts` — four versioned Axios clients (v1, v2, v3, v3Dofus), each with request interceptors that inject the Bearer token and response interceptors that handle 401 → token refresh → retry.
- `src/stores/` — global Pinia stores for UI state and session cleanup on logout (`resetSessionStores()`).
- `src/composables/` — Vue 3 composables: `useEnv`, `useOS`, `useDevice`, `useScreen`, `useAppUpdate`, etc.
- `src/ui/` — theme management (dark/light + PicoCSS color scheme).

### Electron Main Process (`electron/`)

The main process is split into services using Node.js EventEmitter patterns:

| Directory | Purpose |
|-----------|---------|
| `electron/main.cjs` | App lifecycle, window creation |
| `electron/preload.cjs` | Context bridge — exposes IPC API to renderer |
| `electron/ipc/` | IPC handler registration (`sniffer`, `proxy`, `switcher`, `autofocus`) |
| `electron/sniffer/` | Packet capture via tcpdump (SnifferService, BankSniffer, etc.) |
| `electron/proxy/` | MITM proxy using Node `net` module + iptables traffic redirection |
| `electron/switcher/` | Account switcher with global hotkeys (uiohook-napi) |

### IPC Pattern

All IPC goes through the preload context bridge. The renderer calls `window.electron.<method>()`, which maps to `ipcRenderer.invoke()` or `ipcRenderer.on()`. Handlers in `electron/ipc/*.ipc.cjs` register with `ipcMain.handle()`.

Key channels:
- `sniffer:*` — start/stop packet capture, detect game clients, stream captured data back via `sniffer:data`
- `proxy:*` — start/stop MITM proxy, update active modules, stream results via `proxy:hdv-prices`, `proxy:bank-items`, `proxy:scan-progress`
- `switcher:*` — open switcher window, hotkey binding
- `autofocus:*` — monitor and redirect window focus between game accounts
- `update-available` / `apply-update` — electron-updater flow

## Dofus Autofocus

Moteur de focus automatique pour le multi-compte Dofus Retro, intégré via `tshark` et `xdotool`.

- **Moteur :** `electron/sniffer/AutofocusService.cjs` (spawn `tshark`).
- **Détection :**
    - `6a626c` : Mapping ID réseau → Nom du personnage.
    - `697977` : Ordre d'initiative.
    - `6a6163` : Changement de tour (déclenche le focus xdotool après 150ms).
- **Persistance :** IndexedDB store `autofocus_mapping` (front-end) synchronisé via IPC.
- **UI :**
    - Gestion du mapping dans `Settings > Modules > Dofus`.
    - Toggle d'activation dans les paramètres du `Switcher`.
- **Dépendances système :** `tshark` (avec accès non-root aux interfaces), `xdotool`.

### API Clients

Four Axios instances are exported from `src/services/axiosInstance.ts`: `axiosV1`, `axiosV2`, `axiosV3`, `axiosV3Dofus`. The Dofus client adds `X-Game-Version-Id` and `X-Game-Server-Id` headers. All clients share the same 401-refresh-retry interceptor logic.

## Key Configuration

- `vite.config.ts` — Vue plugin, PWA (workbox), `@/` path alias
- `tsconfig.json` — ESNext, strict mode, `@/*` alias maps to `src/*`
- `.env` — `VITE_TOOLS_API_BASE_URL`, Google/GitHub OAuth client IDs
- `package.json` `build` key — electron-builder config (Linux AppImage, Windows NSIS)
- `.releaserc.json` — semantic-release for CI versioning

## Module Admin (`src/modules/Admin/`)

Panel réservé aux utilisateurs avec le rôle ADMIN, TECH ou OWNER.

### Accès et sécurité

- Bouton "Admin" dans le header principal (`src/components/Header/Header.vue`), visible uniquement si `auth.isAdmin`. Stylisé comme les boutons de thème (hauteur 2.25rem, border, sans icône).
- Route guard dans `src/router/router.ts` : `if (to.meta.requireAdmin && !auth.isAdmin) return '/'`
- `isAdmin` getter dans `src/modules/Auth/auth.store.ts` : vérifie que l'utilisateur possède un rôle actif parmi `ADMIN`, `TECH`, `OWNER`.

### Structure

```
src/modules/Admin/
├── admin.routes.ts              # /admin → redirect dashboard, children: dashboard + users + modules
├── Admin.vue                    # Layout avec AdminNav
├── shared/components/
│   └── AdminNav.vue             # Génère les onglets depuis route.matched (même pattern que RiotNav)
├── dashboard/
│   ├── fetch/adminStats.fetch.ts   # GET /api/v3/admin/stats
│   ├── types/adminStats.types.ts
│   └── views/AdminDashboard.vue    # KPIs (totalUsers, activeUsers, newUsersThisWeek) + modules
├── users/
│   ├── fetch/adminUsers.fetch.ts   # GET /users, GET /roles, PUT /users/:id/role
│   ├── types/adminUsers.types.ts   # AdminUser, AdminRole, AdminUserColumn, AdminSortDir, AdminPageSize
│   ├── store/adminUsers.store.ts   # Tri/filtre/pagination côté client
│   ├── views/AdminUsers.vue
│   └── components/
│       ├── AdminUsersHeader.vue    # En-têtes colonnes triables
│       ├── AdminUsersRow.vue       # Ligne utilisateur avec popup édition de rôle inline
│       └── AdminUsersToolbar.vue   # Recherche, sélecteur colonnes, pagination
└── modules/
    ├── fetch/adminModules.fetch.ts  # GET/POST /modules, PUT /modules/:id, GET /modules/:id/users,
    │                                # POST /modules/:id/users/:userId, PUT /modules/:id/users/:userId/role,
    │                                # DELETE /modules/:id/users/:userId
    ├── types/adminModules.types.ts  # AdminModule, ModuleUser, CreateModulePayload, UpdateModulePayload
    ├── store/adminModules.store.ts  # modules, roles, allUsers, moduleUsers, memberIds, availableUsers
    ├── views/AdminModules.vue       # Split view sidebar + DnD panneau détail
    └── components/
        ├── ModuleCreateModal.vue    # Formulaire création module (émet created)
        ├── ModuleEditModal.vue      # Formulaire édition module (prop module, émet updated)
        └── ModuleRolePickerModal.vue # Sélection de rôle après drop (props user+roles, émet confirm)
```

### Tableau utilisateurs — points clés

- **Colonnes** : avatar (fixe 36px), nom, email, rôle, statut, date d'inscription. Même système de `gridTemplateColumns` dynamique que le catalogue Dofus.
- **Avatar** : affiche `<img>` si `avatarUrl` existe et se charge correctement, sinon initiales (2 premières lettres du nom). `@error` sur l'img bascule sur les initiales (URLs Google qui expirent). Clic → `openPreview(url, name, 200)` (min 200px dans la modale).
- **Rôles** : l'API retourne `roles: number[]` (IDs). Le store charge `GET /roles` séparément. La résolution se fait dans `resolvedRoles` computed : `store.roles.find(sr => sr.code === String(r) || String(sr.id) === String(r))`. La hiérarchie `['READ_ONLY', 'USER', 'MODERATOR', 'ADMIN', 'TECH', 'OWNER']` détermine le badge affiché.
- **Édition de rôle** : clic sur la colonne rôle → popup inline (même pattern que les tags workshop). `store.editingRoleUserId` gère "un seul popup ouvert à la fois". Clic sur un rôle → `PUT /users/:id/role` + mise à jour locale + fermeture.
- **`updateUserRoleLocally`** : stocke le `roleCode` (string) dans `user.roles` après un changement.

### Page Modules — points clés

- **Layout** : sidebar 240px (liste des modules avec point actif/inactif + bouton Créer) + panneau droit (header module + zone DnD).
- **Drag & drop natif HTML5** : deux colonnes "Disponibles" / "Membres". Glisser vers Membres → `ModuleRolePickerModal` pour choisir le rôle → `POST /modules/:id/users/:userId` puis `PUT /modules/:id/users/:userId/role { roleId }`. Glisser vers Disponibles → `DELETE /modules/:id/users/:userId`.
- **Rôle inline** : clic sur le badge rôle d'un membre → popup avec la liste des rôles → `PUT /modules/:id/users/:userId/role { roleId }`. Fermeture au clic extérieur et au scroll (listeners sur `document`).
- **Types** : `ModuleUser` → `{ userId: number, name, email, roleId, roleCode }`. Les utilisateurs disponibles viennent de `GET /users` typé `AdminUser[]` (plus de `SimpleUser` — type supprimé). `AdminRole` importé depuis `users/types/adminUsers.types.ts`.
- **Création module** : `POST /modules` — toujours créé inactif. Activer via `PUT /modules/:id { active: true }`. Le code doit correspondre à l'enum `ModuleCode` côté Java.

## Module Riot (`src/modules/Riot/`)

### Valorant — refresh token

Le refresh token ne peut pas être échangé directement depuis le navigateur (CORS bloqué par Riot). Le flux passe par le backend :

```typescript
// valorantShop.fetch.ts
const { data } = await clientV3.post('/riot/valorant/refresh-token', { refreshToken })
// Réponse : { accessToken: string, refreshToken: string } (camelCase)
```

Les cookies `__Secure-access_token` et `__Secure-refresh_token` sont **HttpOnly** — non lisibles via `document.cookie`. L'aide utilisateur dans `ValorantDailyShop.vue` dirige vers DevTools → Application → Cookies.

### Valorant — architecture des fichiers

Le sous-module Valorant est découpé en 4 fichiers :

```
valorant/
  fetch/valorantShop.fetch.ts         # fonctions HTTP + interfaces RawBundle, ShopSkin, etc.
  composables/useValorantShop.ts      # toute la logique métier (état, timers, renewal, auth)
  components/ValorantAuthCard.vue     # formulaire auth (state interne : authMode, tokenInput…)
  components/ValorantBundleCard.vue   # carte pack (props: bundle, now)
  views/ValorantDailyShop.vue         # orchestrateur ~160 lignes (branche composable + composants)
```

Types exportés depuis `useValorantShop.ts` : `View`, `AuthMode`, `BundleSkin`, `ShopBundle`, `REGIONS`.

`ValorantAuthCard` gère tout son état de formulaire en interne (authMode, tokenInput, showToken, selectedRegion) et émet `submit({ token, region, mode })`. L'orchestrateur appelle simplement `handleSubmit(token, region, mode)` du composable.

`ValorantBundleCard` reçoit `bundle: ShopBundle` + `now: number` (valeur de `bundleNow` passée chaque seconde depuis le composable) et calcule le timer live en interne.

### Valorant — suppression de valorant-api.com

**valorant-api.com est entièrement supprimé du frontend.** Tous les appels passent désormais par le backend (`clientV3`) :

| Ancienne fonction | Nouvelle source |
|---|---|
| `fetchClientVersion()` | `GET /riot/valorant/version` → `data.riotClientVersion` |
| `fetchSkinsMap()` | `fetchSkinByLevelId(uuid)` → `GET /riot/valorant/skins/by-level/{uuid}` → `{ name: data.name, icon: data.iconUrl }` |
| `fetchBundleMeta(uuid)` | `GET /riot/valorant/bundles/by-asset/{uuid}` → `{ name: data.name, displayIcon: data.bannerUrl }` |

**Point critique — UUIDs de levels :** le storefront Riot retourne des **UUIDs de levels** (pas de skins racines) dans les offres quotidiennes et les items de bundle. `SKIN_TYPE_ID = 'e7c63390-eda7-46e0-bb7a-a6abdacd2433'` est l'ItemTypeID `EquippableSkinLevel`. Le backend expose `GET /riot/valorant/skins/by-level/{levelUuid}` pour faire le pont.

**Table DB `valorant_skin_levels` :** `id BIGSERIAL, skin_id BIGINT FK (→ valorant_weapon_skins, CASCADE), asset_id UUID UNIQUE, level_index INT, name VARCHAR, level_item VARCHAR, display_icon_url TEXT, streamed_video_url TEXT, created_at, updated_at`. Index sur `skin_id` (PostgreSQL ne le crée pas automatiquement sur les FK).

**Réponse `GET /riot/valorant/skins` :** inclut un tableau `levels[]` embarqué : `{ assetId, levelIndex, displayIconUrl, streamedVideoUrl }`.

### Valorant — historique du shop (Store History)

L'historique est géré en mode **bulk** pour archiver l'intégralité du shop quotidien en un seul appel.

- **Flux de Sync** : Dans `useValorantShop.ts`, l'ajout se fait via `addToStoreHistory(skinIds, shopDate)`. L'appel est `awaité` avant de déclencher un `fetchStoreHistory()` pour rafraîchir l'UI.
- **Calcul de Date stable** : Pour éviter les sauts de date à minuit UTC, la `shopDate` est calculée sur le milieu de la rotation : `expirationMs - 12h`. Cela garantit la même date pendant les 24h de validité du store.
- **Popup d'historique** : Composant `ValorantShopHistoryPopup.vue` utilisant `@floating-ui/vue`.
    - Affiche les skins groupés par date (format "J mois AAAA").
    - Grille de miniatures sans interactivité (cursor default, pas de preview).
    - Fermeture automatique au scroll ou clic extérieur.
    - Placée à gauche du bouton "Changer de token" dans `ValorantDailyShop.vue`.

### Valorant — boutique : packs en vente (FeaturedBundle)

`fetchStorefront` extrait le `FeaturedBundle` de la réponse Riot et retourne `bundles: RawBundle[]` dans `StorefrontResult`.

```typescript
// valorantShop.fetch.ts
export interface RawBundle {
  dataAssetId: string
  items: Array<{ itemId: string; cost: number }>  // skins filtrés par SKIN_TYPE_ID, avec prix unitaire
  totalBaseCost: number
  totalDiscountedCost: number
  discountPercent: number  // décimal (0.33 = -33%)
  remainingSeconds: number
}
```

- `BundleSkin.cost` permet d'afficher le prix individuel de chaque skin dans le pack (badge "OFFERT" si `cost === 0`).
- `bundleNow = ref(Date.now())` mis à jour chaque seconde dans le même `timerInterval` que le compte à rebours des skins → timer live des packs sans interval dédié.
- `useImagePreview` est utilisé sur les images de skins (boutique) et sur la bannière + miniatures des packs (clic → modale).
- Layout carte pack : bannière pleine largeur (`height: auto`, `object-fit: contain`), ligne info (nom + prix/remise à gauche, timer "Xj Xh Xmin" à droite), grille skins en bas (conteneurs 72px, `object-fit: contain`). Badge vert "OFFERT" pour `cost = 0`.
- `buildBundles(rawBundles)` résout bundle meta + skins en parallèle via `Promise.all` — plus de `skinsMap` passé en paramètre. `cachedSkinsMap` supprimé du composable.

## `useImagePreview` — taille minimale

`src/composables/useImagePreview.ts` accepte un troisième paramètre optionnel `minSize` (en px) :

```typescript
open(url: string, alt?: string, minSize?: number)
```

`ImagePreviewModal.vue` applique `min-width` et `min-height` en style inline quand `minSize` est défini. Utilisé pour les avatars admin (200px) sans impacter les autres usages (images catalogue Dofus en taille naturelle).

## Workshop — Liens et popup de visualisation

Chaque atelier peut avoir jusqu'à 3 liens (source `DOFUSBOOK` ou `CUSTOM`).

### Composants liés aux liens

- **`WorkshopLinkViewer.vue`** (`src/modules/Dofus/workshop/components/`) — popup plein écran (overlay `position:fixed`, popup 85vw×85vh) pour visualiser un lien. En Electron : balise `<webview>` (contourne `X-Frame-Options`). En web : message de fallback + lien "ouvrir dans un nouvel onglet". Se ferme au clic sur l'overlay (`@click.self`). La balise `<webview>` nécessite `webviewTag: true` dans `electron/main.cjs` et est déclarée comme custom element natif dans `vite.config.ts` (`isCustomElement: tag === 'webview'`).

- **`WorkshopList.vue`** — les liens des cartes sont des `<span>` qui ouvrent `WorkshopLinkViewer` (plus de `<a target="_blank">`).

- **`WorkshopLinksButton.vue`** (`src/modules/Dofus/workshop/components/workshopdetail/workshopsummary/`) — bouton icône dans la barre de filtres du détail atelier. Ouvre un floating panel (`@floating-ui/vue`, `placement: bottom-end`) avec la gestion complète des liens (add/edit/delete). Accède à l'atelier courant via `useWorkshopDetailStore().workshopId` + `useWorkshopStore()`. Le clic sur un lien (mode lecture) ouvre `WorkshopLinkViewer` et ferme le panel. Le scroll ferme le panel sauf si le scroll vient de l'intérieur du panel (fix coller/paste).

### Fetch liens

Les fonctions `useAddWorkshopLink`, `useUpdateWorkshopLink`, `useDeleteWorkshopLink` sont dans `src/modules/Dofus/workshop/fetch/workshopLink.fetch.ts`.

## Deployment

- **Web**: Docker image `huiitre/tools_web:latest` via `npm run deploy`
- **Electron**: GitHub releases via `electron-updater` (`.github/workflows/deploy.yml` on master)
- **QA**: separate workflow `deploy-qa.yml` on the `qa` branch
