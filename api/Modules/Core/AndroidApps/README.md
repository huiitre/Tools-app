# Apps Android

Gère les apps Android tierces distribuées par **huihuistore**, le store personnel des téléviseurs
(Fire OS, Google TV) : IPTVnator, Zen IPTV, Smarters… L'administrateur importe les APK depuis un
onglet de la page d'administration et choisit, pour chaque app, la version proposée aux TV. Les TV
lisent le catalogue sans compte.

huihuistore lui-même n'est pas géré ici : voir `Core/Huihuistore`. Le contexte complet du projet est
dans `huihuistore/AGENTS.md` (dépôt à part).

## État (20/11/2026)

| Chantier | État |
|---|---|
| Migration `V2.84.0__android_apps.sql` | appliquée en local |
| Lecture d'un APK (`aapt2`, `apksigner`) | écrite, tests unitaires sur les sorties réelles des outils |
| US-1 à US-6 (API, Bruno) | écrites ; catalogue vérifié en local (`200`, admin `401` sans jeton) ; import à tester avec un compte admin |
| Front (onglet admin) | à faire |
| Image Docker : JRE headless + `aapt2`/`apksigner` dans `/opt/android-build-tools` | au déploiement |
| Montage en écriture de `tools_assets/tools_core/android_apps` dans `tools_api(_qa)` (dossier à `chown 1654:1654`) | au déploiement |

## Comment ça marche

**Import, en deux temps** — l'admin n'envoie que le fichier ; le fichier n'est envoyé qu'une fois.
1. **Analyse** (`POST …/imports`). L'API garde l'APK dans une zone d'attente et le lit avec les outils
   officiels du SDK Android :
   - `aapt2 dump badging` : nom de paquet, `versionCode`, `versionName`, `minSdk`, libellé ;
   - `apksigner verify --print-certs` : vérifie la signature, donne le SHA-256 et le nom (DN) du
     certificat, et les schémas de signature.
   Un fichier que ces outils refusent n'est pas un APK valide. Elle renvoie ces informations, l'app
   existante s'il y en a une, et la liste des **avertissements** :
   - `NEW_APP` : paquet inconnu, l'app sera créée ;
   - `CERTIFICATE_CHANGED` : certificat différent de celui de la version recommandée ;
   - `OLDER_THAN_RECOMMENDED` : `versionCode` inférieur à celui de la version recommandée (une TV qui
     l'a déjà ne pourra pas revenir en arrière) ;
   - `MIN_SDK_RAISED` : Android minimum plus élevé que celui de la version recommandée (les TV les plus
     anciennes ne pourront plus l'installer).
2. **Popup** côté front, **à chaque import** : elle affiche tout ce qui a été lu dans l'APK, le compare à
   la version recommandée (version, Android minimum, taille, certificat) et met en avant les
   avertissements ; le libellé est modifiable (prérempli avec celui de l'APK pour une nouvelle app). Rien
   n'est créé sans cette validation.
3. **Confirmation** (`POST …/imports/{importId}/confirm`). Libellé (nouvelle app), case « recommander
   cette version » **cochée par défaut** (décochée : la release est importée mais les TV ne la voient
   pas, cas d'une nightly à tester), notes facultatives. L'APK est rangé dans
   `tools_assets/tools_core/android_apps/<env>/<package>/<versionCode>.apk`, l'app créée au besoin, la
   release créée.

Un import en attente expire au bout d'une heure ; il peut aussi être annulé (`DELETE`). Les contrôles
bloquants (APK illisible, `versionCode` déjà importé) arrêtent l'import dès l'analyse.

**Catalogue** — pour chaque app active qui a une version recommandée, cette version, avec l'URL de
l'APK construite depuis `AssetsBaseUrl`. Les TV téléchargent sur `assets.tools.huiitre.fr`, pas via
l'API.

## User stories

### US-1 — Importer un APK

**En tant qu'**admin, **je veux** envoyer un APK, voir ce qu'il contient quand quelque chose est
inhabituel, puis valider **afin qu'**il soit disponible pour les TV.

Critères d'acceptation :
- [ ] Quand j'envoie un APK, alors je reçois ses informations (paquet, versions, minSdk, libellé,
      certificat, taille, empreinte), l'app existante éventuelle et les avertissements, sans que rien
      ne soit encore créé.
- [ ] Étant donné un paquet inconnu, alors l'avertissement `NEW_APP` est levé ; à la confirmation, l'app
      est créée avec le libellé saisi (prérempli par celui de l'APK).
- [ ] Étant donné un certificat différent de celui de la version recommandée, alors l'avertissement
      `CERTIFICATE_CHANGED` est levé avec les deux empreintes.
- [ ] Étant donné une version plus ancienne que la version recommandée, ou un Android minimum plus élevé,
      alors l'avertissement `OLDER_THAN_RECOMMENDED` ou `MIN_SDK_RAISED` est levé.
- [ ] La popup s'ouvre à chaque import, avec la comparaison à la version recommandée.
- [ ] Quand je confirme, alors l'APK est rangé, la release créée, et recommandée si la case est cochée.
- [ ] Quand j'annule, ou au bout d'une heure, alors le fichier en attente est supprimé.
- [ ] Cas d'erreur : fichier qui n'est pas un APK lisible ou dont la signature ne se vérifie pas →
      `400 ANDROID_APK_INVALID`.
- [ ] Cas d'erreur : `versionCode` déjà importé pour cette app → `409 ANDROID_APP_VERSION_EXISTS`, dès
      l'analyse.
- [ ] Cas d'erreur : import en attente inconnu ou expiré → `404 ANDROID_APP_IMPORT_NOT_FOUND`.
- [ ] Cas d'erreur : fichier de plus de 200 Mo → refusé avant lecture.

Droits : ADMIN.

### US-2 — Lister les apps et leurs versions

**En tant qu'**admin, **je veux** voir chaque app avec toutes ses releases **afin de** choisir la
version recommandée et faire le ménage.

Critères d'acceptation :
- [ ] Chaque app avec son libellé, son paquet, son ordre, son état actif, sa version recommandée et ses
      releases (version, minSdk, taille, empreintes, date et auteur de l'import), de la plus récente à
      la plus ancienne.

Droits : ADMIN.

### US-3 — Modifier une app

**En tant qu'**admin, **je veux** changer le libellé, l'ordre, l'état actif, la version recommandée ou
les notes d'une app **afin de** piloter ce que voient les TV et retrouver où chercher ses APK.

Critères d'acceptation :
- [ ] La version recommandée se choisit parmi les releases de l'app (select).
- [ ] Les notes (texte libre : URL du dépôt, page de téléchargement…) se modifient dans une popup ;
      des notes vides les effacent. Elles n'apparaissent jamais dans le catalogue public.
- [ ] Cas d'erreur : release d'une autre app → `400 ANDROID_APP_RELEASE_MISMATCH`.
- [ ] Cas d'erreur : notes de plus de 5000 caractères → `400 ANDROID_APP_ADMIN_NOTES_TOO_LONG`.

Droits : ADMIN.

### US-4 — Supprimer une release

**En tant qu'**admin, **je veux** supprimer une release non recommandée **afin de** faire le ménage
(nightlies).

Critères d'acceptation :
- [ ] La ligne et le fichier APK sont supprimés.
- [ ] Cas d'erreur : release recommandée → `409 ANDROID_APP_RELEASE_RECOMMENDED`.

Droits : ADMIN.

### US-5 — Supprimer une app

**En tant qu'**admin, **je veux** supprimer une app **afin de** ne plus la suivre.

Critères d'acceptation :
- [ ] L'app, toutes ses releases **et tous leurs fichiers APK** sont supprimés.
- [ ] Le front demande une confirmation qui dit explicitement que tous les APK seront supprimés. Pour
      simplement retirer l'app du catalogue : la désactiver (US-3).

Droits : ADMIN.

### US-6 — Lire le catalogue

**En tant que** TV (huihuistore), **je veux** la liste des apps et de leur version recommandée **afin
d'**afficher leur état et proposer installation ou mise à jour.

Critères d'acceptation :
- [ ] Seules les apps actives ayant une version recommandée, triées par ordre d'affichage puis libellé.
- [ ] Sans authentification.

Droits : public (la TV n'a pas de compte ; une clé embarquée dans l'APK serait extractible ; le
catalogue ne contient aucun secret).

## Règles métier

- Un `versionCode` n'est importé qu'une fois par app. **L'ordre d'import est libre** : on peut importer
  une stable plus ancienne qu'une nightly déjà importée. C'est la TV qui refuse un retour en arrière.
- Le certificat d'un import est comparé à celui de la **version recommandée** de l'app ; s'il diffère,
  il faut confirmer (les TV devront désinstaller puis réinstaller, données perdues).
- La version recommandée appartient toujours à l'app (garanti par la base).
- Une release recommandée ne se supprime pas : en recommander une autre d'abord.
- Supprimer une app supprime ses fichiers ; la désactiver ne supprime rien.
- Le dossier d'environnement (`prod`, `qa`, `dev`) vient de la configuration de l'API, jamais de
  l'appelant : la QA et la prod partagent `tools_assets`.

## Modèle de données

Migrations : `V2.84.0__android_apps.sql`, `V2.85.0__android_apps_admin_notes.sql`.

### `tools_core.android_apps`

| Colonne | Type | Null | Rôle |
|---|---|---|---|
| `id` | `BIGSERIAL` | non | Clé primaire. |
| `package_name` | `VARCHAR(255)` | non | Nom de paquet Android, lu dans l'APK. |
| `label` | `VARCHAR(100)` | non | Nom affiché sur la TV. |
| `display_order` | `INT` | non | Ordre d'affichage, croissant. |
| `is_active` | `BOOLEAN` | non | `FALSE` retire l'app du catalogue sans rien supprimer. |
| `recommended_release_id` | `BIGINT` | oui | Version proposée aux TV ; `NULL` : absente du catalogue. |
| `admin_notes` | `TEXT` | oui | Notes libres de l'admin (5000 caractères au plus), jamais exposées au catalogue. |
| `created_at` | `TIMESTAMPTZ` | non | Création. |

| Contrainte | Garantit |
|---|---|
| `uq_android_apps_package_name` | Un paquet n'est suivi qu'une fois. |
| `ck_android_apps_package_name` | Format Android : au moins deux segments commençant par une lettre. |
| `fk_android_apps_recommended_release` | Clé composite (app, release) : la version recommandée appartient à l'app ; une release recommandée ne peut pas être supprimée. |

### `tools_core.android_app_releases`

| Colonne | Type | Null | Rôle |
|---|---|---|---|
| `id` | `BIGSERIAL` | non | Clé primaire. |
| `app_id` | `BIGINT` | non | App → `android_apps` (suppression en cascade). |
| `version_code` | `BIGINT` | non | `versionCode` de l'APK. |
| `version_name` | `VARCHAR(100)` | oui | `versionName` de l'APK. |
| `min_sdk` | `INT` | non | `minSdkVersion` de l'APK. |
| `file_path` | `VARCHAR(500)` | non | Chemin relatif dans `tools_assets`. |
| `file_size` | `BIGINT` | non | Taille en octets. |
| `file_sha256` | `BYTEA` | non | SHA-256 du fichier, 32 octets. |
| `cert_sha256` | `BYTEA` | non | SHA-256 du certificat du signataire, 32 octets. |
| `release_notes` | `TEXT` | oui | Notes facultatives saisies à l'import. |
| `uploaded_at` | `TIMESTAMPTZ` | non | Import. |
| `uploaded_by` | `BIGINT` | non | Admin → `tools_core.users`. |

| Contrainte | Garantit |
|---|---|
| `uq_android_app_releases_version` | Un `versionCode` par app. |
| `uq_android_app_releases_file_path` | Un fichier par release. |
| `ck_…_sha256_length` | Empreintes de 32 octets. |

## Routes

| Méthode | Route | Story | Droits |
|---|---|---|---|
| `POST` | `/admin/android-apps/imports` (multipart : `file`) | US-1 analyse | ADMIN |
| `POST` | `/admin/android-apps/imports/{importId}/confirm` (`label`, `recommend`, `releaseNotes`) | US-1 confirmation | ADMIN |
| `DELETE` | `/admin/android-apps/imports/{importId}` | US-1 annulation | ADMIN |
| `GET` | `/admin/android-apps` | US-2 | ADMIN |
| `PATCH` | `/admin/android-apps/{appId}` | US-3 | ADMIN |
| `DELETE` | `/admin/android-apps/{appId}/releases/{releaseId}` | US-4 | ADMIN |
| `DELETE` | `/admin/android-apps/{appId}` | US-5 | ADMIN |
| `GET` | `/android-apps/catalogue` | US-6 | public |

Contrat du catalogue :

```json
{
  "apps": [
    {
      "packageName": "app.zeniptv.mobile",
      "label": "Zen Player",
      "release": {
        "versionCode": 148,
        "versionName": "2.1.0",
        "minSdk": 24,
        "url": "https://assets.tools.huiitre.fr/tools_core/android_apps/prod/app.zeniptv.mobile/148.apk",
        "size": 45000000,
        "sha256": "9f2c…",
        "certSha256": "009d14c9…",
        "releaseNotes": null
      }
    }
  ]
}
```

Empreintes en hexadécimal minuscule, le format d'`apksigner`.

## Configuration

Section `AndroidApps` (par déploiement : `appsettings.Production.json`, `appsettings.QA.json`,
`appsettings.Development.json`) :

| Clé | Rôle |
|---|---|
| `StorageRoot` | Racine de `tools_assets` vue par l'API (`/data/tools_assets` en conteneur, la copie locale en dev). |
| `Environment` | Dossier d'environnement : `prod`, `qa`, `dev`. |
| `BuildToolsPath` | Dossier contenant `aapt2` et `apksigner` (dans l'image Docker ; `build-tools/<version>` du SDK en dev). |

## Décisions

- *20/11/2026* — Tables dans `tools_core` et onglet d'administration, pas de module : seul l'admin s'en
  sert.
- *20/11/2026* — Lecture des APK par `aapt2` et `apksigner` plutôt qu'un parseur maison : outils
  officiels, toutes les versions de signature.
- *20/11/2026* — Import en deux temps (analyse puis confirmation) avec zone d'attente : la popup, ouverte à
  chaque import (décision de l'utilisateur), montre ce qui a été lu dans l'APK et ce qui change par rapport
  à la version recommandée avant toute création ; le fichier n'est envoyé qu'une fois. Un paquet inconnu
  crée l'app à la confirmation ; pas d'écran de création d'app séparé.
- *20/11/2026* — Architectures natives de l'APK (`native-code` d'`aapt2`, ex. `arm64-v8a` seul, qui ne
  s'installe pas sur une Fire TV 32 bits) : **pas pour l'instant**, à reprendre plus tard (colonne +
  état « incompatible » côté TV, comme le minSdk).
- *20/11/2026* — Ordre d'import libre (seul le doublon de `versionCode` est refusé) : une stable peut
  arriver après une nightly de numéro supérieur.
- *20/11/2026* — Pas de colonne « source » ni de suivi des releases GitHub : tout APK tiers est importé à
  la main.
- *20/11/2026* — Pas de `withdrawn_at` : retirer une version = en recommander une autre.
- *21/11/2026* — Notes libres par app (`admin_notes`) pour retrouver où télécharger ses APK ; administration
  seulement, absentes du catalogue public.
