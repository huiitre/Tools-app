# AGENTS.md

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
- `src/services/axiosInstance.ts` — four versioned Axios clients (v1, v2, v3, v3Dofus), each with request interceptors that inject the Bearer token and response interceptors that handle 401 → token refresh → retry. Exporte aussi `refreshSession()`, **seul point d'entrée du renouvellement de session** (voir ci-dessous).
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

### `clientCore` — l'API Core

Deux APIs coexistent, et la frontière est **la capacité, pas la technologie** :

| Client | Sert | Contenu |
|---|---|---|
| `clientCore` | API Core | identité, profil, administration, et à terme notifications / realtime |
| `clientV3` (et v1/v2/v3Dofus) | API Java | métier : Dofus, Palworld, Riot |

`auth.fetch.ts` et les trois `fetch` du module Admin (`adminUsers`, `adminModules`,
`adminStats`) passent par `clientCore`. Aucune vue, aucun store n'a bougé : ils ne connaissent
que les fonctions de ces fichiers.

**Le front ignore ce qui sert le Core.** En QA et en production, les deux APIs sont derrière la
même origine et le reverse proxy route par chemin (`/api/v3` → Java, `/api/core` → Core, qui
reçoit les requêtes débarrassées du préfixe). Le langage, la version ou la machine derrière
`/api/core` peuvent changer sans qu'une ligne du front bouge — c'est pour ça que le client est
nommé d'après une capacité (`Core`) et jamais d'après une implémentation.

En développement, le Core est un process séparé sur son propre port et **sans le préfixe
`/api/core`** (il n'a pas de `UsePathBase`). D'où `VITE_TOOLS_CORE_BASE_URL` :

```bash
# .env local
VITE_TOOLS_CORE_BASE_URL=http://localhost:5090
```

Variable absente → repli automatique sur `${VITE_TOOLS_API_BASE_URL}/api/core`, le cas de QA et
de production. Les workflows CI n'ont donc rien à passer.

Le realtime (notifications, et à terme tout autre événement temps réel) est passé sur le Core :
point de connexion WebSocket unique `CoreHub` (SignalR) sur `${VITE_TOOLS_CORE_BASE_URL}/hub`.
Le token voyage en query string (`access_token`) car un navigateur ne peut pas poser de header
custom sur la poignée de main WebSocket. Plus rien ne dépend du STOMP/WebSocket Java.

### `refreshSession()` — renouvellement de session

Toute reprise de session passe par cette fonction : le démarrage de l'app (`router.beforeEach`)
comme le 401 rattrapé par l'intercepteur. Elle fait **deux** choses, dans cet ordre :

1. `POST /auth/refresh` → `auth.setToken(...)`
2. `GET /users/me` → `auth.setUser(...)`

Le second appel est le point important. Les droits affichés (`isAdmin`, `hasModuleAccess`)
viennent tous de `/me`, jamais du JWT — que le front ne décode nulle part. Sans ce rappel, ils
restaient figés depuis le chargement de la page pendant que le token, lui, était réémis toutes
les 10 minutes avec des rôles relus en base : un droit accordé restait invisible jusqu'au F5, et
un droit retiré laissait une interface permissive que l'API refusait ensuite.

**Un seul refresh à la fois.** La promesse en cours est mémorisée dans `refreshPromise` : les
appels concurrents s'y raccrochent au lieu d'en déclencher un second. Cinq requêtes qui prennent
un 401 en même temps (retour d'onglet, page qui charge plusieurs ressources) ne produisent donc
qu'un `POST /auth/refresh` et un `GET /user/me`. Le verrou est relâché par
`promise.then(release, release)` — les deux handlers, pour qu'un refresh en échec le libère
aussi et que le rejet ne remonte pas en `unhandledRejection`.

Deux règles à ne pas casser en modifiant cette fonction :

- **Elle utilise `clientInit`**, le client sans intercepteur. Passer par `clientV3` ferait qu'un
  401 sur le refresh ou sur `/me` redéclencherait un refresh, en boucle — le garde-fou `_retry`
  ne protège pas, chaque tour étant une requête neuve.
- **Un `/me` en échec ne déconnecte pas.** Le token vient d'être renouvelé, la session est
  valide ; on conserve le profil connu. Seul le démarrage traite l'absence de profil comme une
  session inexploitable et repart déconnecté.

Les URLs `/auth/refresh` et `/users/me` sont deux constantes en tête de fichier. Elles sont
servies par `clientInit`, qui vise l'API Core au même titre que `clientCore`.

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
- **Création module** : `POST /modules` — toujours créé inactif. Activer via `PUT /modules/:id` en envoyant **le module complet** avec `active: true`, et non le seul champ modifié : côté API Core c'est un vrai PUT, un payload partiel écraserait les champs absents et se ferait refuser sans `code` ni `name`. `ModuleEditModal.vue` envoie déjà `{ ...props.module }`, donc l'objet entier. Le code doit correspondre à l'enum `ModuleCode` côté Java.

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

## Module Palworld — Breeding Calculator (ÉTAT RÉEL au 2026-08-05 — 3/4 vues construites, UI pas satisfaisante)

**Cette section datait du démarrage du chantier (spec pure) — elle est maintenant dépassée par l'implémentation réelle.** Avant de retoucher quoi que ce soit ici, lire la mémoire projet `palworld-breeding-frontend-spec` (état exact, fichiers touchés, ce qui reste à faire) et `palworld-breeding-engine` (décisions backend). Résumé :

- **Construit** : Calculateur d'élevage, Recherche de combinaisons (enfant/parent), Path Finder (sélection manuelle de Pals possédés + arbre récursif). Routes imbriquées sous `palworld-breeding`, store partagé `breeding.store.ts`.
- **Pas construit** : vue "Arbre d'élevage" autonome (sans contrainte de possession) — seul le composant de rendu d'arbre existe, utilisé par Path Finder.
- **L'utilisateur a explicitement dit que l'UI du Path Finder "va pas trop" après deux passes de correctifs CSS à l'aveugle (jamais vérifiées dans un vrai navigateur).** Ne pas repartir sur une 3ème passe de devinettes CSS — lancer l'app et regarder avant de retoucher, cf. mémoire `feedback-test-ui-in-browser-before-commit`.
- Backend étendu avec `GET /breeding/as-parent` et `GET /breeding/path?target=&owned=` (algorithme `BreedingPathBuilder`, point fixe ET/OU, pas d'optimisation de profondeur).

### Objectif

Nouvelle page Palworld avec plusieurs vues, accessible via une URL qui porte un Pal en paramètre (ex. `?pal={palId}`, exact nom du param au choix de l'implémenteur mais rester cohérent partout). Ça permet à **n'importe quelle page qui affiche des Pals** (Paldex, Tierlist, futures pages) de rediriger vers cette page avec le Pal cliqué déjà pré-sélectionné.

Références visuelles (structure UI à reproduire dans nos codes/conventions, pas à copier tel quel) :
- https://palworld.gg/fr/breeding-calculator (vue par défaut)
- https://palworld.gg/fr/breeding-path?own=flyingmanta,lazydragon (vue path finder, note l'URL avec `?own=` liste de Pals possédés)

### Ordre de travail demandé par l'utilisateur (ne pas paralléliser)

1. **D'abord** : construire la page avec uniquement la vue par défaut (Calculateur d'élevage).
2. **Ensuite** : ajouter le lien de redirection depuis les autres pages qui affichent des Pals (au minimum Paldex `PaldexView.vue`, Tierlist) vers `/palworld/breeding?pal={id}` — probablement un item dans un menu contextuel au clic sur une carte Pal (attention : `PalContextTrigger`/`PalContextFloating` dans `paldex/components/` sont un **tooltip au survol**, pas un menu au clic — il faudra un nouveau mécanisme, pas réutiliser ce composant tel quel).
3. **Ensuite seulement** : peaufiner/étendre aux 3 autres vues.

### Structure de la page

Nouvel onglet top-level dans la nav Palworld (même pattern que Server/Tier list/Paldex) :
- Nouveau fichier `web/src/modules/Palworld/breeding/breeding.routes.ts`, importé dans `web/src/modules/Palworld/palworld.routes.ts` (`...breedingRoutes`), route `name: 'palworld-breeding'`, `meta: { label: 'Élevage', requireAuth: true }` → apparaît automatiquement dans `PalworldNav.vue` (génère les tabs depuis `route.matched` children + `meta.label`, rien à modifier dans le nav lui-même).
- À l'intérieur de cette page, **une barre de boutons en haut** pour switcher entre 4 vues (état local `ref`, ou sous-routes enfants `palworld-breeding-calculator` etc. si on veut des URLs distinctes par vue — au choix de l'implémenteur, mais le param `?pal=` doit survivre au changement de vue) :
  1. **Calculateur d'élevage** — vue par défaut.
  2. **Trouver toutes les combinaisons pour l'enfant** — vue inverse.
  3. **Path finder** — cible + Pals possédés.
  4. **Arbre d'élevage**.

### Vue 1 — Calculateur d'élevage (à faire en premier)

Reproduit `palworld.gg/fr/breeding-calculator` :
- 3 carrés centraux : `[Parent A] + [Parent B] = [Résultat]`.
- Clic sur le carré Parent A → un panneau de sélection apparaît juste en dessous : barre de recherche + select (filtre) + bouton tri asc/desc — **copier exactement le pattern de `PaldexView.vue`** (`.paldex-toolbar`, `searchQuery`/`sortKey`/`sortDir` refs, bouton `mdi-sort-ascending`/`mdi-sort-descending`) — puis en dessous la grille de cartes Pal (`.pal-grid`/`.pal-card`, mêmes classes/structure que Paldex, réutilisables telles quelles ou en composant partagé si ça vaut le coup de factoriser).
- Clic sur une carte → remplit le carré Parent A, **avance automatiquement** la sélection sur le carré Parent B.
- Sélection de Parent B → calcule immédiatement le résultat via `GET /palworld/breeding/result` et remplit le carré résultat.
- Prévoir un petit toggle de genre optionnel par parent (Mâle/Femelle/indifférent) — n'a d'effet que sur les 2 exceptions sexe-dépendantes du jeu (Katress/Wixen), sinon ignoré par l'API.
- Si le pal est passé en URL (`?pal=`), pré-remplir le carré Parent A au chargement.
- Source des données du picker : réutiliser `usePaldexStore`/`fetchPals()` (`web/src/modules/Palworld/paldex/`) — même liste de Pals que le Paldex, pas besoin d'un nouveau fetch.

### Vue 2 — Trouver toutes les combinaisons pour l'enfant

- Sélection d'un Pal cible (même picker que la vue 1, ou pré-rempli si `?pal=` + vue active).
- Appelle `GET /palworld/breeding/parents?child={palId}`, affiche la liste des couples (avec contraintes de sexe et règle exception/formule).

### Vue 3 — Path finder

Reproduit `palworld.gg/fr/breeding-path` :
- Un Pal cible (target).
- Une liste "Pals que je possède" — **pour l'instant sélection manuelle côté front** (pas de source serveur). Le brancher plus tard sur les Pals réellement possédés (`pal_instance` via `modules/palworld/server-data` côté API) est un TODO explicite de l'utilisateur, pas à faire maintenant — `pal_instance` n'a même pas encore de colonne `gender` (cf. mémoire `palworld-breeding-engine`).
- Calcule un chemin d'élevage depuis les Pals possédés vers la cible. **Aucun endpoint API dédié n'existe pour ça** — soit ça se calcule côté front en enchaînant des appels à `/breeding/result`/`/breeding/parents`, soit ça nécessite un nouvel endpoint API (à évaluer avec l'utilisateur, ne pas décider seul si c'est gros).

### TODO Path Finder — suivi demandé le 2026-08-06

- Ajouter une étoile sur la route principale pour la distinguer rapidement des routes alternatives.
- Rafraîchir le front avec les dernières données serveur des Pals.
- Lors d'un changement d'utilisateur, réinitialiser les sélections dépendantes et retirer automatiquement les passifs indisponibles pour le nouvel utilisateur.

### Vue 4 — Arbre d'élevage

Visualisation en arbre des combinaisons menant à (ou partant de) un Pal. Pas de détail UI fourni par l'utilisateur au-delà du nom — à clarifier avec lui avant de coder cette vue (poser la question plutôt que deviner).

### Points d'architecture à respecter

- Suivre le pattern modulaire existant : `web/src/modules/Palworld/breeding/{fetch,types,components,views}/` + `breeding.store.ts` (Pinia), même sous-structure que `paldex/`.
- Types de réponse API (`BreedingResultView`, `BreedingParentPairView` côté Java) à retranspiper en interfaces TS dans `breeding/types/breeding.types.ts`, camelCase (Jackson par défaut, pas d'annotation — vérifier le JSON réel une fois l'endpoint appelé plutôt que deviner les noms de champs).
- Client HTTP : `clientV3` (`@/services/axiosInstance`), comme partout ailleurs dans Palworld.

## Module Notifications — COMPLÈTE (2026-05-10, transport SignalR depuis 2026-08-17)

- **Store** : `useNotificationStore` (Pinia) dans `src/modules/Core/Notification/store`.
- **Transport** : `SignalRNotificationTransport` (connexion à `CoreHub` sur le Core, voir section
  `clientCore` ci-dessus) gère le flux temps réel. `SseNotificationTransport` et
  `WebSocketNotificationTransport` (STOMP côté Java) restent dans `notification.transport.ts`
  mais ne sont plus instanciés — à supprimer si aucun retour arrière n'est prévu.
- **Auto-Sync** : Chargement historique au boot + connexion SignalR auto selon l'auth.
- **Types** : Utilise l'interface `AppNotification` pour éviter le conflit avec `window.Notification`.
- Batch : `markAsRead([ids])` et `remove([ids])` gèrent le mode global si tableau vide.
- OS Push : Déclenche une notif système si l'onglet n'a pas le focus.
- UI : Affichage complet des messages (multi-lignes) avec support des sauts de ligne (white-space: pre-wrap).

## Module Palworld — Marchands (Shop) — COMPLÈTE (2026-08-07)

### Objectif

Page marchands : liste des marchands à gauche (portrait + nom), clic → offres au centre, quantité + ajout au panier, panier multi-marchands persisté en localStorage (pas de DB — presets nommés type "j'ai besoin de matériaux de pal"), aide au choix du marchand le moins cher pour un item donné.

### Source des données (spec finale, différente de la version explorée le 06/08)

L'extracteur produit deux fichiers plats déjà résolus (pas de jointure brute à faire) dans
`/data/docker/tools/tools_assets/tools_palworld/palworld/` :
- `shop_items.json` (267 entrées) : `id` (StaticItemId, clé de jointure), `nameStringId` (→ `strings.json`),
  `icon` (chemin relatif ou `null` sur 88/267 — équipements/cartes de compétence sans icône dans le jeu, pas
  un bug), `price` (référence hors contexte marchand), `maxStackCount`.
- `merchants.json` (25 entrées) : `id` (externalId stable), `code`, `nameStringId` (`null` sur 22/25 — vendeurs
  génériques `male_trader_v04`→`v25` sans nom individuel réel dans le jeu, vérifié), `portrait`, `restockMinute`
  (`null` pour `bounty_trader`), `currencyItemId` (`Money` pour les 22 génériques ; `BattleTicket`/`DogCoin`/
  `BountyProof_1` pour les 3 marchands nommés — ces 3 devises n'ont **aucune** entrée dans `shop_items.json`),
  `offers[]` (`itemId`, `price` déjà résolu — peut différer du prix de référence, 4/267 items concernés —,
  `quantityPerPurchase`, `productType`: `"Normal"` | `"OnlyPurchaseOne"`, `stock` toujours à `0` donc ignoré).

Vérification empirique complète faite avant codage (comptes, jointures, cas `egg`/`Egg`, prix variables,
devises) — cf. conversation du 2026-08-07. Un seul écart trouvé dans le spec initial : `img/item/` contient
313 fichiers réellement, pas 225 (sans impact, les 267 icônes référencées sont toutes vérifiées présentes).

### Backend (Java)

- **DB** : `database/sql/V2.63.0__palworld_shop.sql` — réutilise `tools_palworld.item` existant (déjà alimenté
  par les drops de Pals, même espace d'identifiants `StaticItemId`) en lui ajoutant `price`/`max_stack_count`,
  plutôt qu'une table d'items parallèle. Nouvelles tables `tools_palworld.merchant` (`external_id` UNIQUE,
  `name` nullable, `currency_item_id` VARCHAR sans FK car 2/3 devises spéciales absentes de `item`) et
  `tools_palworld.merchant_offer` (`price`, `quantity_per_purchase`, `product_type` CHECK `NORMAL`/`ONLY_PURCHASE_ONE`,
  UNIQUE `(merchant_id, item_id)`).

- **Catalogue complet d'items — refonte architecturale du 2026-08-07 (retour utilisateur négatif sur la V1)**.
  La toute première version ne synchronisait que les 267 items vendus par un marchand (`shop_items.json`),
  laissant `tools_palworld.item` alimenté uniquement de façon incidentielle par les drops de Pals — qui,
  eux, n'ont **jamais résolu le nom via `strings.json`** (`PalworldLocalPalDataProvider.drops()` stockait le
  slug brut comme nom). Résultat concret repéré par l'utilisateur : des minerais bien réels (`IronOre`,
  `WorldTreeOre`) totalement absents, et d'autres (`Coal`, `ManganeseOre`, `SkyIslandOre`, `Sulfur`) affichés
  avec leur slug brut ("Coal") alors que la vraie traduction FR existait ("Charbon"). Consigne explicite de
  refonte : **`SyncItemsUseCase` synchronise TOUT le catalogue en premier** (source de vérité unique sur
  `name`/`icon_url`/`price`/`max_stack_count`), et les autres sync (drops, offres marchands) ne font plus
  que résoudre un lien vers ce catalogue déjà peuplé — jamais créer ni dégrader un item eux-mêmes.
  - `SyncItemsUseCase` (`modules/palworld/sync/`, tout premier appel dans `SyncPalworldUseCase.execute()`,
    avant même les éléments) : lit **`item_data.json` en entier** (2466 lignes brutes du jeu, pas
    `shop_items.json` qui n'était qu'un sous-ensemble pré-filtré par l'extracteur) via
    `PalworldLocalItemDataProvider`. Résout le nom via `languageDataProvider.getString("ITEM_NAME_" + slug)`
    (comportement standard du provider si absent : `"[missing-string:...]"`, jamais masqué — ex. `IronOre`
    n'a réellement aucune traduction FR dans les données du jeu, c'est transparent, pas une régression) et
    l'icône via `assetsReader.preferredImageFileNameByBaseName("item")` (même pattern de résolution
    case-insensitive que les images de Pals, PAS le champ `Icon.AssetPathName` brut d'`item_icon_data.json`
    qui pointe vers une texture-atlas moteur non exploitable telle quelle — seuls les fichiers réellement
    rippés dans `img/item/` sont utilisés, ~311/2466 en ont un). Aucun filtre appliqué sur les 2466 lignes
    (pas de flag fiable pour distinguer "vrai item" d'entrée technique sans deviner) : tout est synchronisé
    tel quel, upsert uniquement (jamais de suppression, un item disparu d'un extract ne doit pas casser les
    FK `pal_drop`/`merchant_offer` existantes). Retourne `Map<String, Long> itemIdBySlugUpper` (clé
    MAJUSCULE — au moins un cas de casse connu, offre marchand `"egg"` vs catalogue `"Egg"`, résolu par
    lookup insensible à la casse plutôt qu'une normalisation risquée des données sources, même pattern que
    `palIdByTribeUpper` ailleurs dans ce module).
  - `PostgresPalSyncRepository.findOrCreateItem` (drops de Pals) : changé en `INSERT ... ON CONFLICT (slug)
    DO NOTHING RETURNING id` + `SELECT id WHERE slug = ?` en repli — ne touche plus jamais `name`/`icon_url`
    d'un item déjà présent (donc jamais de régression du catalogue par un drop), l'insert ne sert qu'au cas
    résiduel (théorique) d'un slug de drop absent d'`item_data.json`.
  - `SyncMerchantsUseCase` : résout `offers[].itemId` contre `itemIdBySlugUpper` (lookup `.toUpperCase()`),
    ne crée plus jamais d'item lui-même.
  - Tables/fichiers supprimés dans cette refonte : `ShopItemSyncData`, `ShopItemDataProvider`/`Repository`,
    `PalworldLocalShopItemDataProvider`, `PostgresShopItemSyncRepository`, `SyncShopItemsUseCase` — tous
    remplacés par les équivalents génériques `Item*` ci-dessus, `shop_items.json` n'est plus lu du tout.
  - `SyncMerchantsUseCase` (upsert merchant + `deleteOffers`/réinsertion complète par marchand + suppression
    des marchands disparus de la source) tourne après. `PalworldLocalMerchantDataProvider` résout les noms via
    `PalworldLanguageDataProvider` (strings.json) et les URLs d'assets via `assetsBaseUrl`.
  - **Vérifié en conditions réelles (sync complet exécuté directement sur `tools_dev`, 2026-08-07)** :
    2466/2466 items upsertés, 311 avec icône, 1843 avec nom résolu (623 sans traduction dans le jeu lui-même,
    transparent). `IronOre` présent avec nom non résolu (vraie lacune jeu), `WorldTreeOre` présent
    ("Paloxite"), `Coal`/`ManganeseOre`/`SkyIslandOre`/`Sulfur` avec leur vraie traduction FR (plus de slug
    brut). 25 marchands / 326 offres re-synchronisés, **0 skip** (confirme le fix de casse `egg`/`Egg`).
    Pas de nouvelle migration SQL nécessaire (schéma déjà en place depuis V2.63.0) — sync à relancer sur
    QA/prod via `POST /palworld/sync` (Bruno).

- **Catalog** (lecture) : `GET /palworld/shop/merchants` (READ_ONLY) → `List<MerchantView>` avec `offers[]`
  imbriquées (1 requête groupée anti-N+1, pattern `PostgresPalCatalogRepository`, inclut `itemMaxStackCount`
  par offre pour le slider d'achat côté front). `GetMerchantsUseCase` résout le libellé de devise : pour les
  3 devises spéciales, un libellé français est **codé en dur en priorité, avant même de regarder
  `tools_palworld.item`** (`BattleTicket`→"Ticket de combat", `DogCoin`→"Médaille de chien",
  `BountyProof_1`→"Preuve de prime") — nécessaire car `DogCoin` existe par ailleurs dans le catalogue complet
  (drop de Pal réel), mais un nom de devise de marchand n'a rien à voir avec le nom d'affichage de l'item.

### Frontend (Vue 3)

`web/src/modules/Palworld/shop/` — pattern standard (`fetch/`, `types/`, `views/`, `components/`,
`shop.routes.ts` ajouté à `palworld.routes.ts`) :
- `shop.store.ts` (Pinia, `useShopStore`) : catalogue en lecture seule, `ensureLoaded()` fetch une fois,
  getter `merchantsSellingItem(itemSlug)` (triée par prix croissant, sert à l'indicateur "moins cher
  ailleurs"), favoris marchands (`favoriteMerchantIds`, `toggleFavorite`/`hydrateFavorites`, localStorage clé
  `palworld.shop.favoriteMerchants` — indépendant du panier).
- `shopCart.store.ts` (Pinia, `useShopCartStore`) : **100% localStorage, aucune DB** — `current: ShopCartLine[]`
  (`{ merchantId, itemSlug, quantity }`) + `presets: Record<string, ShopCartLine[]>`, clé `palworld.shopCart`,
  hydratation/persist manuelle (pattern `palworldConfig.store.ts`). Les prix/noms ne sont **jamais** stockés
  dans le panier — toujours résolus en live depuis `shop.store` au rendu, pour que charger un preset ancien
  reflète le prix catalogue actuel (estimation de coût à jour), avec gestion du cas "item plus vendu par ce
  marchand" (ligne marquée indisponible plutôt que crash).
- `MerchantList.vue` : portrait + nom (fallback "Marchand ambulant #NN" via le suffixe numérique de
  `externalId` pour les 22 sans nom), étoile favori à gauche (n'intercepte pas le clic de sélection, favoris
  triés en premier), devise du marchand affichée en tout petit en bas à droite de la ligne
  (`pointer-events: none`), barre de recherche fixe en haut (ne scrolle pas avec la liste) qui grise
  (opacity + grayscale) les marchands ne vendant pas l'objet recherché + compteur "X / N marchands". Recherche
  normalisée (`normalizeSearchText`) : ligatures `œ`/`æ` → `oe`/`ae` (non décomposées par `normalize('NFD')`,
  contrairement aux accents) + accents ignorés, pour que "oeuf" trouve "Œuf".
- `MerchantOffers.vue` : grille compacte façon jeu (technique `gap:1px` + fond couleur bordure = séparateurs
  fins façon tableau, pas des cartes flottantes), clic sur un item → `ItemPurchaseModal.vue` (icône, nom,
  contrôle quantité «« ‹ valeur zero-paddée › »», slider 0→`itemMaxStackCount` (ou 1 si `ONLY_PURCHASE_ONE`),
  prix total, bouton ajout panier). Badge "unique" et indicateur "moins cher ailleurs" en overlay sur la carte.
- `ShopCartPanel.vue` : lignes éditables, total groupé par devise, sauvegarde/chargement/suppression de presets.
- Tous les nombres (prix, totaux, bornes de slider) formatés via `@/utils/formatNumber` (`toLocaleString('fr-FR')`,
  espaces en milliers) — utilitaire déjà existant côté Dofus, réutilisé tel quel.
- Layout 3 colonnes à hauteur bornée (scroll interne par panneau, pas de scroll de page) : chaîne CSS
  `height:100%`/`min-height:0`/`overflow:hidden` reprise du pattern qui marche déjà dans
  `PalworldBreedingPathView.vue` (Path Finder) — un `max-height: calc(100vh - Xrem)` approximatif avait été
  essayé en premier et débordait légèrement, corrigé par retour utilisateur.
- `vue-tsc --noEmit` passe sans erreur sur tout le module.

### Fix transverse (pas spécifique au shop, découvert pendant ce module)

PicoCSS applique son anneau de focus (`box-shadow`) sur `:focus` au lieu de `:focus-visible` pour les
boutons/inputs/selects (`public/themes/pico*.min.css`, framework chargé en `<link>` runtime, pas compilé
depuis les sources Sass) : il restait visible après un clic souris jusqu'au prochain focus, sur tout le site
(pas que le shop). Corrigé globalement dans `web/src/assets/styles/main.scss` :
`*:focus:not(:focus-visible) { box-shadow: none !important; outline: none !important; }` — `!important`
nécessaire pour gagner sur des `box-shadow:focus` locaux à certains composants (même bug reproduit ailleurs,
ex. `WorkshopCreateBar.vue`) sans avoir à les corriger un par un. Ne touche pas la navigation clavier
(`:focus-visible` non affecté).

### Pas fait / hors scope

- Pas de test dans le vrai navigateur authentifié par l'agent (pas de credentials/navigateur côté agent) —
  vérifications faites en base + requêtes SQL directes + exécution directe du code de sync hors HTTP.
  L'utilisateur teste lui-même dans son navigateur (serveurs dev déjà lancés) et donne son retour au fil de
  l'eau — plusieurs itérations déjà faites sur ce retour (scroll, grille, devise, focus, recherche, catalogue).
- Pas de géolocalisation des marchands (abandonné, cf. recherche du 06/08 : position hors DataTable,
  mécanisme spawner non percé).
