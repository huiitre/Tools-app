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
| 4 | `BackgroundService` de poll et résolution par `protocol_type` | Remplacé le 28/08/2026 par une résolution par `gameCode` (voir « Un provider par jeu ») |
| 5 | Adapters : Steam A2S, Palworld REST, Source RCON | Devenus des clients de transport, sans connaissance des jeux |
| 6 | Lecture dashboard, contrat Bruno et widget Vue | Fait le 20/08/2026 : widget Home + indicateur header, validés en navigateur |
| 7 | `client_host`/`client_port` (adresse publique affichée aux joueurs) | Fait le 20/08/2026 : migration `V2.68.0`, sync, dashboard et widget |
| 8 | Migration QA/prod, sync réel depuis NAS en continu, tests de pannes isolées | À terminer |
| 9 | Un provider par jeu, dashboard par serveur (`/details`, `/live`) | En cours depuis le 28/08/2026 : socle, Ark et Palworld faits, front à faire |
| 10 | Mods et modpack par serveur (voir « Mods et modpack ») | 11/09/2026 : extractor en production, API codée et testée (fakes), migration `V2.74.0` non appliquée, front à faire |

Les étapes 2 à 7 sont réalisées dans cet ordre. Toute route ajoutée ou modifiée
est ajoutée à `bruno/` dans le même changement.

## Contrats à préserver

- `slug` est le nom du dossier NAS et la clé d'upsert.
- `gameCode` choisit le provider, pour le scheduler comme pour le dashboard.
  `protocolType` reste descriptif et n'est plus utilisé pour router quoi que ce
  soit.
- `port` désigne le port de poll, jamais implicitement le port de jeu.
- ARK: Survival Ascended est interrogé en RCON conforme au spec Valve
  (`ListPlayers`, `maxPlayersOverride`), jamais en A2S. Un jeu dont le RCON dévie
  de ce spec reçoit son propre client de transport plutôt que des cas
  particuliers dans `SourceRconClient` (voir `HUMANITZ_RCON` ci-dessous).
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

## Un provider par jeu (28/08/2026)

Le poll résolvait ses adaptateurs par `protocol_type`, et le dashboard aurait eu
besoin des siens par jeu : le même serveur aurait été interrogé par deux fichiers
différents, avec deux fois la même connaissance du jeu. Le module n'a donc plus
qu'un seul mécanisme.

**Un fichier par jeu dans `Infrastructure/Games/`**, résolu par `gameCode` :

- `IGameServerProvider` — obligatoire, une seule méthode `FetchStatusAsync`,
  appelée par le scheduler. C'est elle qui alimente `game_servers`, donc le
  widget.
- `IGameServerDashboard` — implémentée **en plus** par les jeux qui exposent
  assez d'informations : `FetchDetailsAsync` (volet stable, chargé à l'ouverture)
  et `FetchLiveAsync` (volet rafraîchi, un seul appel quel que soit le jeu). Un
  jeu qui ne l'implémente pas n'a pas de dashboard — rien à déclarer ailleurs, et
  aucune méthode à remplir de valeurs vides.

Les protocoles vivent dans `Infrastructure/Clients/` (`SteamA2sClient`,
`SourceRconClient`, `HumanitzRconClient`) et ne connaissent aucun jeu.

| jeu | statut | dashboard |
|---|---|---|
| PALWORLD | REST `/v1/api/metrics` | oui — `/info`, `/settings`, `/metrics`, `/players`, `/game-data` |
| ARK_SA | RCON `ListPlayers` | oui — `ListPlayers` et `GetGameLog` |
| RUST, 7DTD | A2S | non |
| HUMANITZ | RCON `info` | non |
| COBBLEMON | RCON `list` (joueurs et maximum) | oui — `list uuids`, `time query day`, `tick query` (TPS dans `fps`), et par joueur `data get entity` (Pos, Dimension, Health, XpLevel) + `attribute … max_health` ; réglages : difficulté, bordure, liste blanche, bannis, 53 gamerules ; seed dans `worldId` |

Deux pièges vérifiés en direct sur les serveurs réels :

- **Ark émet des paquets `Keep Alive` non sollicités.** `SourceRconClient`
  apparie donc les réponses par identifiant ; prendre le premier paquet qui
  arrive fait lire la mauvaise réponse.
- **`GetGameLog` vide le journal à la lecture.** Le scheduler n'appelle donc
  jamais `FetchLiveAsync` : il consommerait toutes les 60 s les lignes que le
  dashboard doit afficher. C'est la raison d'être d'une méthode de statut
  séparée et minimale.
  Le front cumule donc les lignes reçues (500 au plus, bouton pour vider) :
  `Log` ne doit contenir que les lignes apparues depuis l'appel précédent,
  jamais un instantané, qui serait dupliqué à chaque rafraîchissement.

### Joindre les serveurs depuis un poste de dev

Le manifest porte les IP docker du NAS : injoignables depuis une machine de
développement. `dev-console` ouvre un tunnel SSH qui les ramène sur `127.0.0.1`
en conservant les ports (voir `AGENTS.md`), et l'option `GameServers:HostOverride`
d'`appsettings.Development.json` y redirige les cibles — un décorateur des ports
de lecture, jamais enregistré quand l'option est absente, donc invisible en QA et
en production.

Le scheduler suit la même option : il ne tourne en Development **que si**
`HostOverride` est renseigné. Sans lui, chaque passage écraserait les statuts
clonés par des « hors ligne » sans valeur.

`ssh -L` ne transportant que du TCP, les serveurs interrogés en A2S resteront
hors ligne en local quoi qu'il arrive.

L'identité affichée (nom du serveur, jeu, image) vient toujours de
`game_servers`, pour tous les jeux, afin que la popup et la carte du widget
concordent. Le provider n'ajoute que ce que la base n'a pas : version,
description, identifiant de monde.

### Actions d'administration

Troisième contrat optionnel, `IGameServerActions`, à côté de `IGameServerDashboard` :

- le jeu **déclare** ses actions (`Actions`) — code, libellé, icône, rôle exigé,
  caractère dangereux, et la liste de ses paramètres (nom, libellé, type
  `text`/`number`/`player`, obligatoire ou non) ;
- il sait les **exécuter** (`ExecuteAsync`).

Aucun code d'action n'est connu du module ni du front : celui-ci construit un
formulaire à partir de la description. Palworld en déclare sept (announce, save,
kick, ban, unban, shutdown, stop), Ark quatre — son RCON n'a ni `unban` ni arrêt
différé. Cobblemon en déclare quatre (announce, kick, ban, restart) :

- `announce` passe par `tellraw @a` et non `say`, qui préfixerait `[Rcon]` : le message
  s'affiche en message système `[Serveur]`, sérialisé en JSON pour survivre aux guillemets.
- `kick` et `ban` reçoivent l'UUID que le front envoie, **refusé par ces deux commandes**
  (vérifié) : le provider le traduit en pseudo parmi les joueurs connectés, et refuse tout le
  reste — un sélecteur comme `@a` expulserait sinon tout le serveur.
- `restart` n'existe pas en RCON : c'est `stop`, et la politique `restart: unless-stopped` du
  conteneur qui relance le serveur. Changer cette politique ferait de `restart` un arrêt définitif.
- Une réponse de refus du serveur (`No player was found`…) devient un 400
  `GAME_SERVER_ACTION_REJECTED` au lieu d'un succès silencieux.

Un jeu qui n'implémente pas ce contrat n'affiche aucune section Actions.

`GET /details` ne renvoie que les actions **autorisées par le rôle de
l'appelant** ; `POST /gameservers/{slug}/actions/{code}` revérifie ce rôle avant
d'exécuter, puis contrôle les paramètres obligatoires. Ce que le front affiche
n'autorise donc rien par lui-même. Les rôles reprennent ceux de l'API Java :
MODERATOR pour announce/save/kick, ADMIN pour ban/unban/shutdown/stop/restart.

## Mods et modpack (11/09/2026)

Afficher les mods d'un serveur et, quand les joueurs doivent les installer eux-mêmes, leur
proposer le modpack client en téléchargement. Rien n'est propre à un jeu : Minecraft Cobblemon
est le premier à s'en servir, un jeu qui installe ses mods à la connexion (Palworld, Ark) peut
déclarer une liste sans modpack.

**Le manifest pilote**, par deux champs optionnels relatifs au dossier du serveur :

- `modsFile` — un fichier JSON, tableau de mods. Seul `name` est exigé ; `version`, `url`,
  `authors`, `filename` et `icon` sont optionnels. C'est le format de l'export JSON « ModList »
  de Prism Launcher, déposé tel quel : la liste affichée est donc celle **du client**, qui peut
  différer du dossier `mods/` du serveur (choix assumé : elle décrit ce que le joueur installe).
- `modpackFile` — le fichier que les joueurs téléchargent, quel que soit son format (`.zip`,
  `.mrpack`…).

**L'extractor** (NAS) intègre la liste à `gameservers.json` (champ `mods`) et copie le modpack
vers `tools_core/gameservers/modpacks/<slug><extension>`. Il ne recopie que si le **sha256**
diffère de la copie publiée — la taille et la date ne suffisent pas — et passe par un fichier
temporaire renommé, pour qu'un téléchargement en cours ne lise jamais un fichier à moitié écrit.
Il publie `modpackFile`, `modpackSize` et `modpackSha256`, et supprime les modpacks orphelins.
Un fichier déclaré mais absent n'est qu'un avertissement ; un chemin qui sort du dossier du
serveur ou un mod sans `name` fait échouer le passage.

**Le sync** enregistre les mods dans `tools_core.game_server_mods` (migration `V2.74.0`) et le
modpack dans `game_servers.modpack_url`/`modpack_size`. L'URL porte le hash en paramètre `v` :
elle ne change pas quand le fichier est remplacé, et nginx ne renvoie aucun `Cache-Control`.
Une liste modifiée est réécrite en entier et compte comme une mise à jour ; une liste identique
n'est pas touchée.

**Les icônes** sont résolues au sync, jamais à la lecture, par un adapter par hébergeur
(`IModIconResolver`), choisi d'après l'URL du mod :

| hébergeur | URL reconnue | appel |
|---|---|---|
| Modrinth | `modrinth.com/<type>/<id ou slug>` | `GET /v2/projects?ids=[…]`, sans clé |
| CurseForge | `curseforge.com/projects/<id>` | `POST /v1/mods`, clé `GameServers:CurseForgeApiKey` |

- Une icône déjà enregistrée n'est jamais redemandée : un hébergeur n'est appelé qu'à l'arrivée
  d'un nouveau mod, en un seul appel pour tous.
- Une panne ou une réponse invalide laisse les nouveaux mods sans icône, sans faire échouer le
  sync ; le passage suivant réessaie.
- `icon` posé dans le fichier de mods l'emporte sur l'hébergeur.
- Sans clé CurseForge, son adapter n'est pas enregistré : ses mods restent sans icône. La clé est
  un secret : `appsettings.Local.json` en dev, variable `GameServers__CurseForgeApiKey` sur les
  conteneurs `tools_api` et `tools_api_qa`.

**La lecture** : `GET /gameservers` expose `modCount` et `hasModpack`, qui suffisent au widget ;
la liste est chargée à part par `GET /gameservers/{slug}/mods`, snapshot de la base comme le
reste du widget.
