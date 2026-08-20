# Game Servers Dashboard

## Objectif

Le dashboard affiche les serveurs de jeux connus, leur dernier statut et le
nombre de joueurs. Il lit exclusivement PostgreSQL : aucun chargement du
dashboard ne contacte un serveur de jeu ou Steam.

La configuration vient des manifests scannés sur le NAS par
`tools_gameserver_extractor`, qui publie un fichier JSON consolidé dans les assets.
L'API C# ne lit jamais `/data/docker/games`.

## Frontières retenues

- Aucun enregistrement `access_module` et aucune entrée de menu ne sont créés.
  Le dashboard reste le seul point d'accès Web.
- Le code C# est néanmoins rangé dans `Modules/GameServers/` : c'est une
  frontière de responsabilités et de composition, pas un module fonctionnel
  visible ni soumis à un droit. Cela préserve l'organisation module-local de
  l'API sans transformer le widget en page produit.
- La synchronisation est une route service-à-service de déclenchement,
  `POST /internal/gameservers/sync`, protégée par `[InternalApi]` :
  `X-Internal-Token` doit correspondre à `INTERNAL_API_TOKEN`. Ce n'est pas un
  JWT. Le secret est injecté dans la configuration Docker de l'API. L'extractor
  NAS reste un script lancé par cron : son shell charge un fichier `.env` local
  non versionné avant d'appeler l'API. Le secret n'est jamais commité, placé
  dans Bruno ou écrit dans un shell interactif.
- La route ne reçoit aucun body. Le use case charge
  `tools_core/gameservers/gameservers.json` depuis le CDN d'assets, valide le
  tableau entier puis, dans une transaction, upsert les manifests par `slug`
  et supprime les lignes absentes du scan, sans modifier les colonnes de statut.
- Le poll écrit uniquement `online`, `num_players`, `max_players` et
  `checked_at`. Une erreur ou un timeout est isolé par serveur et se traduit
  par `online = false` pour ce serveur seulement.
- `GameServersPollingService` lance un passage immédiatement au démarrage, puis
  toutes les 60 secondes. Il crée un scope à chaque passage et n'appelle aucun
  use case sécurisé : il n'existe pas d'utilisateur HTTP dans un scheduler.
- `GET /gameservers` exige un JWT portant au moins `READ_ONLY`, lit uniquement
  les lignes `is_visible = true`, et retourne le snapshot en base. Il n'expose
  ni `host`, ni `port`, ni `protocol_config` — ce sont les coordonnées internes
  du poll, potentiellement une IP LAN et des credentials. `clientHost`/
  `clientPort`, à l'inverse, sont l'adresse publique destinée aux joueurs et
  sont volontairement exposés : c'est ce que le widget affiche et permet de
  copier.

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
| 2 | Migration `tools_core.game_servers`, ports et adaptateur PostgreSQL/Dapper | Implémenté ; migration appliquée sur `tools_dev` uniquement, QA/prod à faire |
| 3 | Sync interne, DTO/validation, transaction et requête Bruno | Implémenté et testé |
| 4 | `BackgroundService` de poll et résolution par `protocol_type` | Implémenté et testé |
| 5 | Adapters : Steam A2S, Palworld REST, Source RCON | Implémentés ; tests réseau réels à faire sur QA |
| 6 | Lecture dashboard, contrat Bruno et widget Vue | Fait le 20/08/2026 : widget Home + indicateur header, validés en navigateur |
| 7 | `client_host`/`client_port` (adresse publique affichée aux joueurs) | Fait le 20/08/2026 : migration `V2.68.0`, sync, dashboard et widget |
| 8 | Migration QA/prod, sync réel depuis NAS en continu, tests de pannes isolées | À terminer |

Les étapes 2 à 7 sont réalisées dans cet ordre. Toute route ajoutée ou modifiée
est ajoutée à `bruno/` dans le même changement.

## Contrats à préserver

- `slug` est le nom du dossier NAS et la clé d'upsert.
- `gameCode` est destiné à l'identité et l'affichage ; seul `protocolType`
  choisit l'adapter.
- `port` désigne le port de poll, jamais implicitement le port de jeu.
- `SOURCE_RCON` utilise `listplayers` et `maxPlayersOverride`; il ne tente pas
  A2S pour ARK: Survival Ascended. C'est un protocole RCON conforme au spec Valve
  standard — un jeu dont le RCON dévie de ce spec obtient son propre
  `protocolType` et son propre adapter plutôt que des cas particuliers dans
  `SourceRconStatusProvider` (voir `HUMANITZ_RCON` ci-dessous).
- `HUMANITZ_RCON` (vérifié en direct le 20/08/2026, serveur réel) : même framing
  TCP que le RCON Source, mais une auth non conforme au spec Valve — succès =
  deux paquets reçus (le premier `type=0`/body `"None"` à ignorer, le second
  `type=2`), `request_id` toujours à `0` côté serveur donc jamais comparé, échec
  = aucun paquet, juste une fermeture TCP après un délai (pas de paquet
  d'échec explicite). Aucune commande `listplayers` : la commande est `info`,
  qui renvoie un texte libre dont seule la ligne `"<N> connected."` est
  exploitée pour le nombre de joueurs ; le reste (season/weather/AI/FPS) n'est
  pas parsé. Format de la liste nominative des joueurs non vérifié (jamais eu
  de joueur connecté pendant les tests) — à faire si l'API doit un jour
  afficher les noms/SteamID plutôt que le seul total.
- `STEAM_A2S` envoie une requête A2S_INFO UDP et gère la réponse challenge ;
  `PALWORLD_REST` appelle `/v1/api/metrics` avec Basic Auth ; `SOURCE_RCON`
  s'authentifie puis exécute `listplayers` sur TCP.
- L'absence d'un manifest est autoritaire : elle entraîne le hard delete de la
  ligne correspondante.
- Un sync retourne `created`, `updated`, `unchanged` et `deleted`. Il rafraîchit
  toujours `last_synced_at`, mais ne compte pas cette date seule comme une mise à
  jour de configuration.
- `pictureFile` est l'autorité de l'extractor : il désigne le fichier copié sous
  `img/` (par exemple `img/palworld.png`) et produit l'URL
  `{App:AssetsBaseUrl}/tools_core/gameservers/<pictureFile>` ; nul, Steam fournit
  l'image de repli. Une panne Steam conserve les métadonnées déjà enregistrées.
- `clientHost`/`clientPort` sont l'adresse à laquelle un joueur se connecte
  réellement, distincte de `host`/`port` qui ne servent qu'au poll interne
  (IP LAN, port RCON/REST de statut). Le manifest les fournit toujours ; la
  validation du sync les exige (`clientHost` non vide, `clientPort` entre 1 et
  65535). Les colonnes `client_host`/`client_port` restent nullable en base
  (pas de contrainte `NOT NULL`) : le widget doit donc afficher un état "port
  inconnu" plutôt que de supposer leur présence.
