# API Core — étude d’architecture et plan de migration

Ce document conserve les décisions prises pour l’extraction progressive du Core historique Java vers `api-core`, une application C# / ASP.NET Core.

> Statut : étude de faisabilité. Aucune migration fonctionnelle n’est engagée.

## Intention

Le monorepo conserve quatre zones principales :

```text
web/       Frontend Vue
api/       API métier Java / Spring Boot (monolithe modulaire)
api-core/  Plateforme Core C# / ASP.NET Core
database/  Migrations et éléments PostgreSQL
```

L’objectif n’est pas de créer un microservice par module métier. Les modules Health, Dofus, Todo, etc. restent ensemble dans `api/`. L’extraction concerne la plateforme transverse, stable, et sert également de projet d’apprentissage réaliste d’ASP.NET Core.

## Frontière retenue

`api-core` porte :

- authentification, émission et renouvellement des JWT ;
- utilisateurs, rôles, accès aux modules et rôles contextuels par module ;
- autorisation transverse et contrats de claims ;
- notifications persistées du Core ;
- connexion et livraison realtime ;
- mail en tant que gateway générique, sans contenu ni règles métier.

`api` porte :

- toutes les commandes, règles, données et transactions des modules métier ;
- les décisions d’autorisation métier propres à un module ;
- la persistance des objets métier (ex. messages d’un chat de jeu) ;
- le calcul des destinataires d’événements métier.

Une capacité doit rester dans un module métier lorsqu’elle dépend de ses règles ou de ses données. Par exemple, un mute dans un chat de jeu est une règle de ce chat ; un rôle `READ_ONLY` sur un module est une autorisation transverse.

## Authentification et autorisation cible

Le Core signe les JWT ; l’API Java les vérifie localement avec une clé publique ou un JWKS. Il ne doit pas y avoir d’appel HTTP au Core pour chaque requête métier.

Les claims devront inclure l’identité et les droits nécessaires à Java, notamment le rôle contextuel par module. Exemple conceptuel :

```json
{
  "sub": "42",
  "modules": {
    "chat": "READ_ONLY",
    "dofus": "USER"
  }
}
```

Un use case Java qui exige `USER` et le module `CHAT` rejette donc un utilisateur ayant `READ_ONLY` sur ce module, avant toute persistance ou diffusion.

Le délai de prise en compte d’un changement de droit est au plus la durée de vie de l’access token. Commencer avec des access tokens courts est préférable à une introspection réseau systématique ou à une infrastructure de révocation complexe.

### État actuel à prendre en compte

L’API Java actuelle fait cette vérification en base à chaque use case :

1. `ModuleAuthorizationPort.hasAccess` vérifie l’accès au module ;
2. `UserRoleProvider.getUserRole(userId, module)` lit le rôle contextuel dans `tools_core.user_module_role` ;
3. l’aspect compare ce rôle à `SecuredUseCase.requiredRole()`.

Le rôle de module remplace donc le rôle global pour l’autorisation du use case concerné. La migration devra remplacer ces lectures SQL par la lecture des claims JWT, sans modifier l’intention de la règle.

Point à clarifier avant la migration : la clé primaire actuelle de `user_module_role` autorise plusieurs rôles pour une paire `(user, module)`, mais le code récupère un seul rôle avec un `LIMIT 1`. Le modèle visé semble être un rôle contextuel unique ; cela devra être rendu explicite.

## Realtime / WebSocket

Il y a un seul point de connexion realtime côté frontend, fourni par `api-core`. Il n’y a pas de troisième service dédié au WebSocket.

Pour apprendre ASP.NET Core idiomatiquement, la cible privilégiée est SignalR plutôt qu’une reproduction serveur de STOMP. Cette migration peut être menée indépendamment du reste.

```text
Frontend ── connexion realtime unique ──> api-core

Frontend ── commande métier HTTP ───────> api Java
api Java ── événement déjà validé ───────> api-core ──> clients connectés
```

Exemple de chat de jeu :

1. le frontend envoie `POST /api/game/chat/messages` à Java ;
2. Java vérifie les droits, les règles du chat et les participants ;
3. Java persiste le message ;
4. après commit, Java demande au Core de livrer l’événement aux destinataires ;
5. Core ne fait que l’authentification de la connexion et le routage technique.

Core reçoit une enveloppe technique avec des destinataires explicites, un `eventType` et un payload JSON. Il ne doit connaître ni le métier du jeu ni la signification du payload.

Le WebSocket est une optimisation de fraîcheur, non la source de vérité : une donnée importante doit toujours être récupérable par REST après reconnexion. L’appel Java vers Core peut échouer sans faire échouer une commande métier déjà persistée ; le client récupère alors l’état manqué au prochain rattrapage.

L’implémentation actuelle des notifications est déjà proche de ce modèle : elles sont persistées et l’historique est chargé par `GET /notifications`. À améliorer lors de la migration : publier le signal temps réel après le commit et fusionner l’historique récupéré avec les événements déjà reçus afin de fermer la fenêtre entre chargement et abonnement.

## Contrats et découplage

- OpenAPI séparé par service ; version HTTP uniquement en cas de rupture de contrat, jamais pour refléter le langage ou l’implémentation.
- Contrat JWT documenté, stable et versionné si nécessaire.
- Appel interne Java → Core limité à un contrat de publication realtime.
- Aucun accès SQL direct d’un service au schéma détenu par l’autre.
- Une même instance PostgreSQL est acceptable au départ, avec des schémas et propriétaires de données clairement séparés.
- Le reverse proxy garde une origin publique unique et route par capacité, sans exposer le backend réel.
- Les appels internes propagent un `X-Request-Id`, journalisé par les deux services.

## Organisation du dépôt

La décision actuelle est de conserver le monorepo. Le bénéfice pratique est de
pouvoir faire et livrer un changement coordonné Web + API(s) + base dans un seul
commit, sans la friction de plusieurs repositories, commits et pushes.

Le monorepo n’empêche pas des cycles de version indépendants par composant. Une
éventuelle séparation future reste possible lorsque les frontières et les
contrats seront suffisamment stables ; elle ne conditionne pas le démarrage de
`api-core`.

## Versioning et identification des déploiements

### Besoin

Le Web/Electron possède déjà un versioning automatique avec Semantic Release et
des releases GitHub globales `vX.Y.Z`. Ce mécanisme ne doit pas être perturbé :
la page Downloads du Web interprète ces releases comme des téléchargements
utilisateur.

Les APIs et la base ont néanmoins besoin d’être identifiables précisément, par
leurs images déployées et, à terme, par une version calculée automatiquement.

### Principe retenu pour API Core

Le Web/Electron conserve ses tags Git et ses GitHub Releases publics. `api-core`
n'en crée aucun : son numéro SemVer est calculé par les workflows QA et
Production à partir de tous les Conventional Commits qui modifient `api-core/`,
dans leur ordre chronologique :

```text
fix:   → patch
feat:  → minor
feat!: ou BREAKING CHANGE: → major
```

Chaque `feat:` incrémente immédiatement le minor, chaque `fix:` le patch et
chaque rupture le major. Ainsi, un merge vers `master` qui contient dix
`feat:` Core produit la même version que les dix déploiements QA successifs.
Les labels OCI de l'image identifient la version calculée et le SHA source,
sans modifier l'historique Git ni publier de GitHub Release.

Les images QA publiées sont :

```text
huiitre/tools_api_core:qa
huiitre/tools_api_core:sha-a1b2c3d
```

`qa` est suivi par Watchtower ; le tag SHA est immuable et identifie le
build exact. Le numéro SemVer et le SHA sont aussi retournés par `/version`.

### Version d’exécution et endpoints de diagnostic

Les APIs exposeront à terme une route de version, alimentée par la version
calculée et le SHA Git injectés au build :

```json
{
  "version": "0.1.0",
  "gitSha": "a1b2c3d",
  "environment": "qa"
}
```

Les checks de santé sont distincts de cette version :

```text
GET /health/live  → processus vivant
GET /health/ready → application prête, dépendances nécessaires comprises
GET /version      → version et build déployés
```

La version Database ne doit pas être déduite seulement du dernier fichier dans
Git lorsqu’on veut connaître l’état d’un environnement : le fichier le plus haut
donne la version attendue par le dépôt, tandis que l’historique Flyway dans
PostgreSQL donne la version effectivement appliquée. Les migrations restent des
changements explicites ; elles ne doivent pas être numérotées automatiquement à
partir de `feat:` ou `fix:`.

## Plan de démarrage : valider le déploiement avant le métier

### Phase 1 — squelette ASP.NET Core

Créer une application ASP.NET Core minimale dans `api-core/`, sans base de données, authentification ni WebSocket. Elle expose uniquement :

```text
GET /health
GET /version
```

`/version` doit permettre d’identifier précisément l’image déployée (version ou SHA Git injecté à la build).

### Phase 2 — image et déploiement QA

- Ajouter un `Dockerfile` multi-stage .NET dans `api-core/`.
- Ajouter un workflow GitHub Actions QA, déclenché seulement pour :

  ```yaml
  - 'api-core/**'
  - '.github/workflows/api-core-deploy-qa.yml'
  ```

- Calculer la version depuis les Conventional Commits apparus après le SHA de
  l'image QA `qa` précédente.
- Construire et publier `huiitre/tools_api_core:qa`.
- Publier également un tag immuable `sha-<SHA court>`, en plus de `qa`, pour faciliter le diagnostic et un éventuel rollback.

### Phase 3 — service QA sur le NAS

Créer un container `tools-api-core-qa` avec :

- un Docker Compose dédié ;
- un healthcheck HTTP sur `/health` ;
- le réseau Docker nécessaire pour le reverse proxy ;
- Watchtower configuré pour surveiller ce seul container ;
- les secrets préparés, mais inutilisés tant que le squelette ne fait pas appel à des dépendances externes.

Ne pas dérouter l’authentification ou les routes existantes à cette étape.

### Phase 4 — test de la chaîne complète

Modifier volontairement la valeur de `/version`, puis vérifier :

```text
push GitHub → build Actions → Docker Hub → Watchtower → container QA → /version
```

Le but est de valider le build .NET, l’image, le pull automatique, le healthcheck et la visibilité depuis le NAS avant toute complexité métier.

### Phase 5 — reverse proxy QA

Ajouter une route QA sans consommateur frontend, par exemple :

```text
https://qa.tools.huiitre.fr/api/core/health → api-core QA
```

Elle permet de vérifier la chaîne proxy + container sans introduire de route fonctionnelle Core.

### Phase 6 — construction par tranches verticales

Ordre recommandé :

```text
health/version/déploiement
→ configuration, logs et correlation-id
→ PostgreSQL et schéma Core séparé
→ utilisateurs
→ authentification et émission JWT
→ validation locale des JWT C# par Java
→ migration frontend de l’authentification
→ rôles et droits de module dans les claims
→ SignalR / realtime
→ mail
```

Chaque tranche doit pouvoir être testée en QA sans retirer prématurément le comportement Java existant.
