# Compte à rebours des actions serveur

## Objectif

Permettre de lancer un redémarrage ou un arrêt différé depuis l’interface des serveurs de jeu.
L’API accepte la demande, puis gère elle-même le compte à rebours en tâche de fond :

```text
Interface
  → demande restart/stop avec un délai
API
  → accepte la demande et démarre le compte à rebours
  → envoie éventuellement des annonces au serveur
  → exécute restart/stop à zéro
Docker
  → relance le conteneur si la politique de redémarrage le prévoit
```

Le serveur de jeu n’a donc pas besoin de gérer le compte à rebours. Il reçoit uniquement les
commandes d’annonce et l’action finale via son provider.

## Architecture retenue

Le use case HTTP actuel reste générique. Il ne doit pas contenir de branche spécifique comme
`if actionCode == "restart"`.

Un seul service commun sera ajouté pour tous les jeux :

```text
GameServerActionCountdownService : BackgroundService
```

Il conserve un état indépendant par serveur, et non par type de jeu. Ainsi plusieurs serveurs
Minecraft, Palworld ou ARK peuvent avoir chacun leur propre compte à rebours simultanément.

La clé de suivi est l’identifiant du serveur (`serverId`), pas uniquement `gameCode`.

La persistance en base n’est pas nécessaire pour l’instant : si l’API redémarre, un compte à
rebours en mémoire peut être perdu. Ce cas est accepté pour cette première version.

## Responsabilités

### Use case HTTP

`GetGameServerDashboardUseCase.ExecuteAction(...)` reste le point d’entrée de la route actuelle :

```text
POST /gameservers/{slug}/actions/{actionCode}
```

Il continue de gérer :

- la résolution du serveur ;
- le contrôle des droits ;
- la vérification de l’action déclarée par le provider ;
- la validation des paramètres ;
- l’exécution directe lorsqu’aucun délai n’est demandé ;
- l’enregistrement auprès du `GameServerActionCountdownService` lorsqu’un délai est demandé.

Il ne connaît pas les différences entre Minecraft, Palworld et ARK.

### BackgroundService

Le service de compte à rebours :

- reçoit une demande différée ;
- empêche plusieurs comptes à rebours concurrents pour un même serveur ;
- conserve l’échéance et l’action finale (`restart` ou `stop`) ;
- attend jusqu’aux différents points d’annonce ;
- tente d’envoyer les annonces si le provider expose l’action `announce` ;
- ignore une annonce absente ou échouée et poursuit le compte à rebours ;
- appelle l’action finale à zéro.

Le `CancellationToken` de la requête HTTP ne doit pas être utilisé pour attendre le compte à
rebours. Le service utilise le token de durée de vie de l’application (`stoppingToken`).

L’exception d’une annonce ne doit jamais interrompre le compte à rebours : elle est journalisée
en warning. L’action finale (`restart` ou `stop`) ne doit pas être silencieusement ignorée ; son
échec doit être journalisé comme une erreur.

### Providers

Les providers restent responsables uniquement de traduire les actions en commandes propres à
leur jeu. Le scheduler ne construit aucune commande RCON ou REST spécifique.

Le scheduler peut appeler le provider avec :

```text
announce { message: "Redémarrage dans 30 secondes." }
restart {}
stop {}
```

Avant chaque annonce, il teste dynamiquement si l’action `announce` est déclarée. Si elle
n’existe pas, il continue sans annonce. Il n’y a donc pas de validation spécifique dans le use
case pour chaque combinaison de jeu et d’action.

## Exemple de déroulement

Pour un redémarrage Minecraft dans 60 secondes :

```text
POST .../actions/restart { "waittime": "60" }
→ le use case enregistre la demande
→ HTTP 202 Accepted (action acceptée, pas encore terminée)
→ BackgroundService appelle announce : « Redémarrage dans 60 secondes »
→ annonce à 30, 10, 5, 4, 3, 2 et 1 seconde si possible
→ BackgroundService appelle CobblemonProvider.ExecuteAsync("restart", ...)
→ CobblemonProvider envoie stop via RCON
→ Docker relance le conteneur
```

Pour un provider sans `announce`, le compte à rebours et l’action finale sont conservés ; seules
les annonces sont omises.

## Actions actuellement connues

- Palworld expose `shutdown` avec son propre paramètre `waittime`, ainsi que `stop` immédiat.
- Cobblemon expose `restart`, actuellement traduit par la commande RCON `stop` puis relancé par
  Docker.
- ARK expose actuellement `announce`, mais pas encore `restart` dans le provider présent dans le
  dépôt. L’action devra être ajoutée avant de pouvoir planifier son redémarrage.

L’objectif du prochain travail est d’ajouter le délai commun à `restart` et `stop` pour les jeux
concernés, sans casser les actions immédiates existantes.

## Fichiers envisagés

```text
api/Modules/Core/GameServers/Application/Ports/Games/
  IGameServerActionCountdownService.cs

api/Modules/Core/GameServers/Application/Dto/Games/
  ScheduledGameServerAction.cs

api/Modules/Core/GameServers/Infrastructure/Scheduling/
  GameServerActionCountdownService.cs
```

Le use case existant sera modifié pour déléguer au service lorsqu’un délai est fourni. Les
providers seront complétés uniquement avec les actions et paramètres nécessaires.

## Statut HTTP

`204 No Content` signifie normalement que l’action est terminée. Pour une action différée,
`202 Accepted` décrit correctement la situation : la demande est acceptée et sera exécutée plus
tard. Le contrat Bruno et le frontend devront être adaptés si ce choix est retenu.
