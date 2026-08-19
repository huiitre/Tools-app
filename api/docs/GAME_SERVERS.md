# Game Servers Dashboard

## Objectif

Le dashboard affiche les serveurs de jeux connus, leur dernier statut et le
nombre de joueurs. Il lit exclusivement PostgreSQL : aucun chargement du
dashboard ne contacte un serveur de jeu ou Steam.

La configuration vient des manifests scannés sur le NAS par
`tools_gameserver_extractor`. L'API C# ne lit jamais `/data/docker/games`.

## Frontières retenues

- Aucun enregistrement `access_module` et aucune entrée de menu ne sont créés.
  Le dashboard reste le seul point d'accès Web.
- Le code C# est néanmoins rangé dans `Modules/GameServers/` : c'est une
  frontière de responsabilités et de composition, pas un module fonctionnel
  visible ni soumis à un droit. Cela préserve l'organisation module-local de
  l'API sans transformer le widget en page produit.
- La synchronisation est une route service-à-service
  `POST /internal/gameservers/sync`, protégée par `[InternalApi]` :
  `X-Internal-Token` doit correspondre à `INTERNAL_API_TOKEN`. Ce n'est pas un
  JWT. Le secret est injecté dans la configuration Docker de l'API. L'extractor
  NAS reste un script lancé par cron : son shell charge un fichier `.env` local
  non versionné avant d'appeler l'API. Le secret n'est jamais commité, placé
  dans Bruno ou écrit dans un shell interactif.
- Le payload est validé entièrement avant écriture. Dans une transaction, le
  sync upsert les manifests par `slug` puis supprime les lignes absentes du
  scan, sans modifier les colonnes de statut.
- Le poll écrit uniquement `online`, `num_players`, `max_players` et
  `checked_at`. Une erreur ou un timeout est isolé par serveur et se traduit
  par `online = false` pour ce serveur seulement.

## Images et Steam

Le test du 19/08/2026 de `store.steampowered.com/api/appdetails` a réussi pour
Rust (`252490`), Palworld (`1623730`) et ARK: Survival Ascended (`2399830`).
Steam retourne notamment `name`, `header_image` et `capsule_image`.

Politique initiale : `pictureFile` reste un override local optionnel. En son
absence, le sync enrichit une fois le serveur depuis Steam et persiste l'URL
`header_image` dans `picture_url`. Le widget consomme cette URL stockée ; ni le
poll de 30-60 secondes ni le navigateur ne doivent appeler Steam pour chaque
rafraîchissement. Un échec Steam ne bloque pas le sync : `picture_url` reste
null ou conserve l'ancienne valeur. Un futur rafraîchissement de métadonnées,
peu fréquent et séparé du poll, sera décidé seulement si nécessaire.

## Découpage et avancement

| Étape | Contenu | État |
|---|---|---|
| 0 | Vérifier Steam AppDetails et la stratégie image | Fait le 19/08/2026 |
| 1 | Cadrage, contrat d'authentification interne et ce document | Fait le 19/08/2026 |
| 2 | Migration `tools_core.game_servers`, ports et adaptateur PostgreSQL/Dapper | Implémenté, migration réelle à appliquer |
| 3 | Sync interne, DTO/validation, transaction et requête Bruno | Implémenté et testé |
| 4 | `BackgroundService` de poll et résolution par `protocol_type` | À faire |
| 5 | Adapters : Steam A2S, Palworld REST, Source RCON | À faire |
| 6 | Lecture dashboard, contrat Bruno et widget Vue | À faire |
| 7 | Migration appliquée, sync réel depuis NAS, tests de pannes isolées et validation navigateur | À faire |

Les étapes 2 à 7 sont réalisées dans cet ordre. Toute route ajoutée ou modifiée
est ajoutée à `bruno/` dans le même changement.

## Contrats à préserver

- `slug` est le nom du dossier NAS et la clé d'upsert.
- `gameCode` est destiné à l'identité et l'affichage ; seul `protocolType`
  choisit l'adapter.
- `port` désigne le port de poll, jamais implicitement le port de jeu.
- `SOURCE_RCON` utilise `listplayers` et `maxPlayersOverride`; il ne tente pas
  A2S pour ARK: Survival Ascended.
- L'absence d'un manifest est autoritaire : elle entraîne le hard delete de la
  ligne correspondante.
- Un sync retourne `created`, `updated`, `unchanged` et `deleted`. Il rafraîchit
  toujours `last_synced_at`, mais ne compte pas cette date seule comme une mise à
  jour de configuration.
- `pictureFile` est l'autorité de l'extractor : non nul, il produit l'URL
  `{App:AssetsBaseUrl}/tools_core/gameservers/img/<fichier>` ; nul, Steam fournit
  l'image de repli. Une panne Steam conserve les métadonnées déjà enregistrées.
