# huihuistore (mise à jour de l'app)

huihuistore est le store personnel des appareils Android (Fire OS, Google TV, téléphones) : il
installe et met à jour les apps tierces gérées par `Core/AndroidApps`, et **se met à jour lui-même**
grâce à ce sous-module. Sa CI publie chaque nouvel APK ici ; l'app interroge la version publiée au
lancement puis régulièrement, et propose la mise à jour.

Pas de base, pas d'écran d'administration pour l'instant (décision du 20/11/2026) : la version publiée
est décrite par un fichier, et une version se teste en déployant la branche `qa` de huihuistore (autre
paquet, autre clé) sur un appareil de test. Le contexte complet du projet est dans
`huihuistore/AGENTS.md` (dépôt à part).

## État (20/11/2026)

| Chantier | État |
|---|---|
| Publication (US-1), lecture de la version publiée (US-2), Bruno | écrites, règles couvertes par des tests unitaires |
| Scope `huihuistore:publish` | ajouté au catalogue ; clés prod et QA à créer dans l'admin |
| `Huihuistore:CertificateSha256` prod et QA | à remplir quand les keystores existeront (vide = publication refusée) |
| Montage en écriture de `tools_assets/tools_core/huihuistore` dans `tools_api(_qa)` (dossier à `chown 1654:1654`) | au déploiement |

## Comment ça marche

**Publication** — la CI envoie l'APK signé (multipart, `X-Api-Key`).
1. L'APK est reçu hors de `tools_assets` (zone d'attente de `AndroidApps`) puis lu par `aapt2` et
   `apksigner`, comme un import du store. Un fichier illisible ou mal signé est refusé.
2. Contrôles, tous sur ce qui est lu dans l'APK :
   - nom de paquet = `Huihuistore:PackageName` du déploiement (le flavor QA n'est pas publiable en prod) ;
   - certificat = `Huihuistore:CertificateSha256` du déploiement ;
   - `versionCode` strictement supérieur à la version publiée.
3. L'APK est rangé sous `tools_core/huihuistore/<env>/<versionCode>.apk`, puis `version.json` est
   réécrit **en dernier** (fichier temporaire renommé). Les 5 dernières versions sont gardées.

**Lecture** — l'app appelle `GET /huihuistore/update` sans compte, compare `versionCode` au sien et
télécharge l'APK sur `assets.tools.huiitre.fr`.

Le dossier d'environnement (`prod`, `qa`, `dev`) vient de `AndroidApps:Environment` : la QA et la prod
partagent `tools_assets`.

## User stories

### US-1 — Publier une version

**En tant que** CI de huihuistore, **je veux** envoyer l'APK signé d'un build **afin que** les appareils
de cet environnement se mettent à jour.

Critères d'acceptation :
- [ ] Étant donné un APK du bon paquet, signé par la bonne clé, de `versionCode` supérieur, quand je
      l'envoie, alors il est rangé, `version.json` le désigne et la réponse décrit la version publiée.
- [ ] Cas d'erreur : APK illisible ou signature invalide → `400 ANDROID_APK_INVALID`.
- [ ] Cas d'erreur : autre paquet → `400 HUIHUISTORE_PACKAGE_MISMATCH`.
- [ ] Cas d'erreur : autre clé → `400 HUIHUISTORE_CERTIFICATE_MISMATCH`.
- [ ] Cas d'erreur : `versionCode` inférieur ou égal à la version publiée → `409 HUIHUISTORE_VERSION_NOT_NEWER`.
- [ ] Cas d'erreur : paquet ou certificat attendus non configurés → `503 HUIHUISTORE_NOT_CONFIGURED`.

Droits : clé d'API avec le scope `huihuistore:publish`, ou utilisateur ADMIN.

### US-2 — Connaître la version publiée

**En tant que** huihuistore sur un appareil, **je veux** connaître la version publiée **afin de**
proposer la mise à jour si elle est plus récente que la mienne.

Critères d'acceptation :
- [ ] Rend paquet, `versionCode`, `versionName`, `minSdk`, URL de l'APK, taille, empreintes, notes, date.
- [ ] Cas d'erreur : rien de publié → `404 HUIHUISTORE_NOT_PUBLISHED`.

Droits : public.

## Règles métier

- Tout vient de l'APK : la CI ne déclare ni version ni paquet.
- Chaque environnement ne publie que son app (paquet + clé). Une clé d'API qui fuiterait ne permet de
  publier que le vrai huihuistore, déjà signé.
- L'ordre compte (contrairement au store) : une version publiée ne peut être remplacée que par une plus
  récente. Android refuse de toute façon les retours en arrière sur un appareil.
- Le dossier vient de la configuration de l'API, jamais de l'appelant ni de la clé : un clonage de la
  base prod vers la QA copie les clés d'API, la QA ne doit quand même écrire que dans `qa/`.

## Fichiers

`tools_assets/tools_core/huihuistore/<env>/` :

| Fichier | Rôle |
|---|---|
| `<versionCode>.apk` | Une par version, les 5 dernières (`Huihuistore:KeptReleases`). |
| `version.json` | La version publiée (contenu de `HuihuistoreRelease`). |

Un nom par version plutôt qu'un `huihuistore.apk` écrasé : l'APK est en place avant que `version.json`
ne le désigne, un appareil qui télécharge pendant une publication obtient toujours le bon fichier.

## Routes

| Méthode | Route | Story | Droits |
|---|---|---|---|
| `POST` | `/huihuistore/releases` (multipart : `File`, `ReleaseNotes`) | US-1 | scope `huihuistore:publish` ou ADMIN |
| `GET` | `/huihuistore/update` | US-2 | public |

## Configuration

Section `Huihuistore`, par déploiement :

| Clé | Prod | QA | Dev |
|---|---|---|---|
| `PackageName` | `fr.huiitre.huihuistore` | `fr.huiitre.huihuistore.qa` | `fr.huiitre.huihuistore.qa` |
| `CertificateSha256` | empreinte de la clé prod | empreinte de la clé QA | au choix |
| `KeptReleases` | 5 (défaut) | | |

Stockage et outils : section `AndroidApps` (`StorageRoot`, `Environment`, `BuildToolsPath`).

## Décisions

- *20/11/2026* — Upload par la CI avec une clé d'API par environnement plutôt qu'un conteneur nginx
  dédié : la lecture d'APK et l'écriture dans `tools_assets` existent déjà pour le store, et aucune
  entrée de reverse proxy n'est ajoutée.
- *20/11/2026* — Pas de table : `version.json` suffit à dire ce qui est publié.
- *20/11/2026* — Pas d'écran d'administration pour l'instant ; on teste une version via la branche `qa`.
- *20/11/2026* — Un fichier par version (et non `current/` + `archive/`), pour que la publication soit
  sans fenêtre où le manifeste désigne un fichier absent.
