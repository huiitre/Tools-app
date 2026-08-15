# API Core — étude d’architecture et plan de migration

Ce document conserve les décisions prises pour l’extraction progressive du Core historique Java vers `api-core`, une application C# / ASP.NET Core.

> Statut : migration engagée. L’authentification complète (inscription, connexion, Google,
> session, mot de passe) et le profil utilisateur sont implémentés dans `api-core`. Le
> frontend appelle encore l’API Java : la bascule reste à faire.

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

## Contrat d'erreur HTTP

API Core utilise `application/problem+json` et le format standard `ProblemDetails` pour toutes les erreurs HTTP. Les contrôleurs et les use cases ne construisent jamais de JSON d'erreur manuellement.

```json
{
  "title": "Not Found",
  "status": 404,
  "message": "Utilisateur introuvable.",
  "instance": "/users/42",
  "code": "USER_NOT_FOUND",
  "requestId": "a1b2c3"
}
```

`code` est le contrat stable consommable par le frontend. `message` est un message public ; aucune cause technique, requête SQL, secret ou stack trace ne doit y être exposé. `requestId` correspond au header `X-Request-Id`, accepté lorsqu'il est valide ou généré par API Core, puis ajouté à la réponse et aux logs.

Les erreurs applicatives utilisent l'unique `AppException` et ne connaissent pas HTTP. Elle porte un `ErrorKind`, un `code` et un message public. Le handler global mappe ce type vers HTTP :

| ErrorKind | Statut |
|---|---:|
| Validation | 400 |
| NotFound | 404 |
| Conflict | 409 |
| Unauthorized | 401 |
| Forbidden | 403 |
| Unavailable | 503 |

La validation automatique des contrôleurs (`[ApiController]`) utilise la même fabrique et renvoie `VALIDATION_FAILED`. Les corps JSON mal formés renvoient `INVALID_REQUEST_BODY`. Les exceptions techniques inconnues renvoient `INTERNAL_ERROR` avec un 500 ; les dépendances indisponibles, notamment PostgreSQL, renvoient `DEPENDENCY_UNAVAILABLE` avec un 503.

Les réponses 401 et 403 du middleware d'authentification passent par cette même fabrique, via les callbacks `OnChallenge` et `OnForbidden` de `JwtBearerEvents`. Il est interdit d'écrire un second format JSON d'erreur ailleurs.

### Tests d'intégration du contrat HTTP

Les tests HTTP sont dans `api-core/tests/Tools.ApiCore.IntegrationTests`. Ils démarrent API Core en mémoire avec l'environnement `Testing`, sans ouvrir de port ni appeler PostgreSQL. Les endpoints `/_tests/errors/{kind}` sont mappés uniquement dans cet environnement et ne sont donc pas exposés en Development, QA ou Production.

Ils vérifient le contrat partagé pour 400, 404, 409 et 500, ainsi que la propagation ou génération de `X-Request-Id`. Ils doivent être complétés lorsqu'un nouveau `code` ou un nouveau comportement HTTP est introduit.

```bash
dotnet test api-core/tests/Tools.ApiCore.IntegrationTests/Tools.ApiCore.IntegrationTests.csproj
```

## Contrat de routes HTTP

```text
/auth/*      moyens d'identification : inscription, login, refresh, logout, Google, mot de passe
/users/me    profil de l'appelant
/users/{id}  profil d'un utilisateur (administration, à venir)
/health/*    sondes
/version     identification du déploiement
```

**`me` est un identifiant**, pas un préfixe. Il se résout au porteur du jeton et appartient
donc à la même famille que `/users/{id}` : une seule ressource, un seul préfixe. L'API Java
sépare `/user` (soi) de `/users` (administration) ; ce découpage crée deux territoires pour
la même ressource et oblige à trancher, à chaque nouvel endpoint, de quel côté il tombe.

**Le mot de passe vit sous `/auth`, pas sous `/users`.** Ce n'est pas une propriété du
profil mais un moyen de s'identifier — au même titre qu'un provider Google ou qu'une session.
Les trois flux sont donc réunis :

```text
POST  /auth/register
POST  /auth/verify-email
POST  /auth/password/reset-request
POST  /auth/password/reset
PATCH /auth/password
```

Le code le disait déjà : `SetUserPasswordUseCase` vit dans `Modules/Auth/Application/Usecases/Password/`.
L'exposer depuis `UsersController` faisait qu'un module publiait le use case d'un autre.

`PATCH /auth/password` ne porte aucun identifiant, comme `/auth/logout` : l'identité vient
toujours du jeton. Une URL qui désignerait l'utilisateur ouvrirait la porte à viser le compte
d'un autre.

## Namespaces et organisation du code

Chaque fichier déclare un **file-scoped namespace** dérivé de son chemin :

```csharp
// Modules/Auth/Application/Usecases/LoginUseCase.cs
namespace Tools.ApiCore.Modules.Auth.Application.Usecases;
```

`Program.cs` fait exception : un fichier à top-level statements ne peut pas déclarer de
namespace. Il reste dans le namespace global et importe explicitement les namespaces des
modules — c'est la racine de composition, elle câble tout, donc elle voit tout.

Les `using` entre modules sont **explicites**, jamais globaux. C'est la raison d'être des
namespaces ici : rendre le couplage inter-modules lisible en tête de fichier. Un
`using Tools.ApiCore.Modules.Mail.Application;` dans un fichier du module Auth signale
immédiatement une dépendance entre modules, qu'on peut alors questionner. Un
`GlobalUsings.cs` qui importerait les modules annulerait ce bénéfice ; il est réservé, le
cas échéant, aux namespaces techniques externes.

Ce choix rend aussi les règles de couches **vérifiables par la machine** : une bibliothèque
de tests d'architecture (NetArchTest par exemple) peut désigner « les types de
`…Auth.Domain` » et interdire leurs dépendances. Sans namespaces, ces règles ne sont que
des conventions écrites, invérifiables — les dossiers ne compilent pas.

### Découper un dossier Infrastructure

Quand l'infrastructure d'un module dépasse la dizaine de fichiers, elle se découpe en
sous-dossiers. Exemple du module Auth :

```text
Modules/Auth/Infrastructure/
  Google/       client OAuth, options, store d'état, vérification OIDC
  Jwt/          émission, validation, options, paramètres cryptographiques, cookie de refresh
  Password/     hachage, options de réinitialisation, nettoyage planifié
  Persistence/  les adaptateurs PostgreSQL des ports du module
```

Le critère est **la raison de changer**, pas la nature technique. On regroupe ce qui évolue
ensemble : `Google/` bouge quand Google change son API, `Persistence/` quand le schéma SQL
change, `Jwt/` quand la politique de jetons change.

C'est pourquoi il n'y a **pas** de dossier `Scheduler/`, contrairement à l'API Java.
Regrouper les tâches de fond par leur mécanisme de déclenchement séparerait
`PasswordResetCleanupService` de `PasswordResetOptions`, alors que la durée de rétention, la
table purgée et la raison d'être du service viennent toutes du flux de réinitialisation :
une seule évolution imposerait de toucher deux dossiers. Et le problème s'aggrave à mesure
qu'on ajoute des tâches planifiées sans rapport entre elles — un tel dossier devient le
tout-venant de ce qu'on ne sait pas classer.

Le nom décrit la responsabilité quand elle survit au choix technique : `Persistence/` plutôt
que `Postgres/`, pour qu'un changement d'implémentation ne rende pas le dossier menteur.

Une classe publique reste **un fichier portant son nom** — les dossiers ne dispensent pas de
cette règle.

### Découper les use cases et les ports

Même principe côté Application, avec un découpage par **méthode d'identification** qui
répond en miroir à celui de l'Infrastructure :

```text
Modules/Auth/Application/
  Ports/
    IAuthRepository.cs        transverse : lu par les trois flux
    Google/                   client OAuth, vérification d'identité, store d'état, comptes Google
    Password/                 hachage, jetons de réinitialisation, credentials, providers
  Usecases/
    Google/                   URL d'autorisation, callback
    Password/                 login, demande de réinitialisation, réinitialisation, définition
    Session/                  renouvellement, session Electron
```

`LoginUseCase` est dans `Password/` et non dans `Session/` : il délègue toute la création de
session à `AuthSessionService`, et ce qui lui reste en propre est la vérification d'un couple
email / mot de passe. `Session/` ne garde que ce qui gère la durée de vie d'une session
**quelle que soit** la méthode d'identification initiale.

Un port consommé par plusieurs flux reste à la racine de `Ports/`. `IAuthRepository` est lu
par le login, le renouvellement, la session Electron et les flux de mot de passe : le ranger
sous l'un d'eux laisserait croire à une appartenance qui n'existe pas. Le rangement doit
décrire les dépendances réelles, pas les suggérer.

### Racine de composition et enregistrement des modules

`Program.cs` a compté jusqu'à 265 lignes : 45 `using`, une quarantaine de `AddScoped` à la
file, la construction de la chaîne de connexion PostgreSQL et les endpoints de test du contrat
d'erreur. Rien n'y était faux, mais le fichier ne répondait plus à la seule question qu'on lui
pose — *de quoi cette application est-elle faite ?*

**Chaque module porte sa propre composition**, dans un fichier `<Module>Module.cs` à sa racine :

```text
Modules/Auth/AuthModule.cs          AddAuthModule()
Modules/Common/CommonModule.cs      AddCommonModule()
Modules/Health/HealthModule.cs      AddHealthModule()
Modules/Mail/MailModule.cs          AddMailModule()
Modules/Security/SecurityModule.cs  AddSecurityModule()
Modules/Users/UsersModule.cs        AddUsersModule()
```

Ce fichier vit **à la racine du module**, pas dans une couche : il câble `Api`, `Application`
et `Infrastructure` ensemble, donc il ne peut appartenir à aucune des trois. C'est l'équivalent
.NET idiomatique d'une classe `@Configuration` Java — même granularité, même rôle.

L'extension porte sur **`IHostApplicationBuilder`** et non sur `IServiceCollection` : les
modules ont besoin de `Configuration` pour leurs options et de `Environment` pour ce qui dépend
de l'environnement, et le type de retour rend l'enchaînement lisible. `Program.cs` retombe
alors à une trentaine de lignes, dont la partie qui compte :

```csharp
builder.AddCommonModule()
    .AddSecurityModule()
    .AddAuthModule()
    .AddMailModule()
    .AddUsersModule()
    .AddHealthModule();
```

`Common` vient en premier parce que les autres dépendent de son contrat d'erreur et de son
accès PostgreSQL. La flèche ne s'inverse jamais : un module transverse ne connaît aucun module
métier.

**Un enregistrement appartient au module sans lequel il n'aurait plus de raison d'être.**
`AddHttpContextAccessor()` est déclaré par `Security`, seul consommateur du contexte ambiant,
et non par la racine de composition — sinon un déplacement de fichier laisserait
`HttpCurrentUserProvider` sans sa dépendance. Ce qui reste vraiment à l'hôte — journalisation,
MVC, CORS, ordre du pipeline — vit dans `Composition/` :

```text
Composition/CoreHostExtensions.cs      AddCoreHost()      configuration locale, Serilog, contrôleurs, CORS
Composition/CorePipelineExtensions.cs  UseCorePipeline()  l'ordre des middlewares
```

**L'ordre du pipeline reste au même endroit.** C'est la raison de ne pas laisser chaque module
insérer ses propres middlewares : leur ordre relatif est un comportement — authentification
avant autorisation, garde 404 entre les deux — pas un détail d'organisation.

Les routes suivent la même règle que les services. `/version` est mappé par le module Health
(`MapVersionEndpoint`) et non par la racine : c'est une route de diagnostic de la même famille
que `/health/*`, appelée par les mêmes clients sans jeton, et `DiagnosticsTests` les vérifie
déjà ensemble. Les endpoints de test suivent leur module — contrat d'erreur pour `Common`,
route témoin non sécurisée pour `Auth` — et le `if (IsEnvironment("Testing"))` qui les mappe
reste visible dans `Program.cs`.

Le `public partial class Program {}` en fin de fichier a disparu au passage : il servait à
rendre le type visible depuis `WebApplicationFactory<Program>`, ce que le SDK fait désormais
seul (analyseur `ASP0027`). Les 50 tests d'intégration passent sans lui.

#### Aucune découverte de modules par réflexion

Les templates de monolithe modulaire proposent couramment une interface `IModule` découverte
par scan d'assemblies, ou un enregistrement automatique par convention (Scrutor). **Les deux
sont écartés ici**, pour la raison déjà retenue à propos du contexte ambiant : cela remplace du
bruit visible par de la magie invisible. Le graphe de composition cesse d'être lisible, et des
règles d'architecture vérifiables par la machine ne peuvent plus rien constater sur des
enregistrements qui n'existent qu'au runtime.

Le volume de `using` en tête d'un fichier reste un signal utile, pas une gêne à masquer : dans
`Program.cs` il doit désigner six modules, ce qui est exactement ce que la racine de
composition est censée voir.

### Pas de CancellationToken dans les signatures

Le projet **n'utilise pas** l'annulation coopérative. Aucun port, use case, service applicatif
ou repository ne prend de `CancellationToken`.

La convention .NET veut qu'on propage ce jeton de bout en bout pour qu'une requête abandonnée
par le client libère immédiatement ses ressources. C'est utile sur des I/O longues et à fort
trafic ; ici les requêtes durent quelques millisecondes pour une poignée d'utilisateurs. Le
bénéfice réel est nul, le coût — un paramètre supplémentaire sur chaque méthode de chaque
couche, définitivement — ne l'est pas.

Trois exceptions subsistent, toutes imposées de l'extérieur :

| Emplacement | Raison |
|---|---|
| `ApiExceptionHandler.TryHandleAsync` | Signature de l'interface `IExceptionHandler` |
| `PasswordResetCleanupService.ExecuteAsync` | Signature de `BackgroundService` |
| `GoogleOidcTokenVerifier` | `GetConfigurationAsync` n'a pas de surcharge sans jeton |

Le cas du `BackgroundService` est le seul où le jeton **sert** : `WaitForNextTickAsync`
l'utilise pour interrompre l'attente à l'arrêt de l'application. Sans lui, couper le
conteneur attendrait le prochain tick, soit jusqu'à trente minutes.

Ce qui a été écarté au passage : exposer le jeton par un contexte ambiant
(`AsyncLocal`, ou un port lisant `HttpContext.RequestAborted`) pour alléger les signatures.
Cela remplace du bruit visible par de la magie invisible, empêche toute composition
(pas de délai local via un jeton lié), et renvoie silencieusement `CancellationToken.None`
hors requête HTTP — c'est-à-dire précisément dans les tâches de fond, là où l'annulation
compte le plus.

Rien n'a été désinstallé : `CancellationToken` appartient à `System.Threading`, donc au
runtime. Le réintroduire un jour reste possible ; le faire à moitié ne l'est pas — soit les
signatures le portent partout, soit nulle part.

### Nommage des exceptions

L'exception applicative s'appelle `AppException`. Elle s'est d'abord appelée
`ApplicationException`, ce qui entrait en collision avec `System.ApplicationException` de
la BCL : sans namespace la nôtre masquait silencieusement celle de .NET, et l'introduction
des namespaces a transformé ce masquage en ambiguïté de compilation. Le préfixe `App`
s'aligne sur `AppOptions` et la section de configuration `App:`.

`BusinessException` a été écarté — la classe porte aussi `ErrorKind.Unavailable`, qui n'est
pas métier — et `DomainException` également, puisqu'elle vit dans `Common/Application`.

Règle générale : ne jamais donner à un type le nom d'un type courant de la BCL.

## Tests

Deux projets distincts, séparés par **nature de test** et non par dossier — les tests
d'intégration ont leurs propres dépendances, sont plus lents, et la CI doit pouvoir lancer
les unitaires sans eux.

```text
tests/
  Tools.ApiCore.IntegrationTests/
    Fixtures/    ApiCoreWebApplicationFactory
    Fakes/       doubles mémoire des ports, une classe par fichier
    Modules/     miroir des modules du code source
      Auth/      AuthenticationTests, PasswordTests
      Common/    ErrorContractTests
      Mail/      MailControllerTests
      Security/  AuthorizationTests
  Tools.ApiCore.UnitTests/
```

Les tests d'intégration démarrent l'application en mémoire, sans port ni PostgreSQL, et
sont organisés **par module**, pas en miroir fichier par fichier : ils ne testent pas une
classe mais un comportement traversant controller, use case et repository. Les tests
unitaires, eux, suivront le miroir strict du code source.

Une règle transverse se teste une seule fois, dans le module qui la porte, en utilisant une
route quelconque comme support :

- `AuthenticationTests` (module Auth) — absence de jeton, jeton illisible, falsifié, expiré,
  refresh token présenté comme access token, compte désactivé, et les cas qui doivent
  continuer de passer.
- `AuthorizationTests` (module Security) — règles de `UseCaseAuthorizer` : aucun rôle, code
  de rôle inconnu, cumul de rôles.

Ce qu'un module teste chez lui, c'est ce qui lui appartient : `MailControllerTests` vérifie
que `SendMailUseCase` exige `TECH`, pas que l'authentification fonctionne.

Toute suite de tests de refus doit comporter un **cas positif**. Sans lui, une route cassée
qui refuse tout le monde laisserait la suite verte.

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

### Ajouter une variable d'environnement à un conteneur existant

Watchtower met à jour l'image en **réutilisant la configuration du conteneur existant**. Une
variable ajoutée au `docker-compose.yml` n'entre donc jamais dans un conteneur déjà créé, même
après plusieurs mises à jour d'image. Il faut le recréer explicitement :

```bash
docker compose up -d --force-recreate
docker exec <conteneur> env | grep <VARIABLE>
```

Le symptôme observé pour `JWT_SECRET` : `500 INTERNAL_ERROR` sur **toutes** les routes, y
compris `/health`, avec « JWT_SECRET doit contenir au moins 32 octets UTF-8 » — la variable
valant `""`. Le healthcheck échouant toutes les 30 secondes, les logs se remplissent vite.

Deux conséquences à retenir :

- **Les routes anonymes ne contournent pas le middleware d'authentification.** Il s'exécute
  sur chaque requête et construit les options JWT ; une configuration invalide casse donc
  aussi `/health` et `/version`.
- **Les options sont construites paresseusement**, à la première requête. L'application
  démarre « saine » avec une configuration inutilisable. Ajouter `ValidateOnStart()` ferait
  échouer le démarrage, ce qui serait plus lisible en exploitation.

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
[x] health / version / déploiement
[x] configuration, logs et correlation-id
[x] PostgreSQL et schéma Core séparé
[x] mail (gateway générique)
[x] authentification, émission JWT et middleware de validation
[x] flux mot de passe (réinitialisation, définition)
[x] inscription et confirmation d'adresse
[x] profil utilisateur (/users/me)
[ ] migration frontend de l'authentification   ← prochaine étape
[ ] rôles et droits de module dans les claims
[ ] SignalR / realtime
```

La validation locale des JWT par Java n'a pas lieu d'être pour l'instant : les deux APIs
partagent le même `JWT_SECRET`, le même issuer et le même choix d'algorithme HMAC, donc un
jeton émis par le Core est déjà accepté par Java. La question se reposera le jour où le Core
passera à une paire de clés asymétrique.

## Bascule du frontend — faite côté code, à valider en QA

L'authentification du front est branchée sur l'API Core. Tout s'est passé dans `web/`, et reste
réversible en repointant `clientCore` sur `/api/v3`.

**Ce qui a été fait :**

- **`clientCore`** (`web/src/services/axiosInstance.ts`), à côté de `clientV3`, avec
  `withCredentials: true` et les mêmes intercepteurs. `auth.fetch.ts` passe entièrement par lui.
- **`refreshSession()`** — renouvellement de session centralisé, un seul refresh à la fois,
  suivi d'un `/users/me` qui remet à jour les droits affichés (ils restaient figés depuis le
  chargement de la page). `clientInit`, qu'il utilise pour éviter toute récursion, vise le Core
  lui aussi.
- **Les URLs corrigées** : `/user/me` → `/users/me`, `/user/password` → `/auth/password`, et
  suppression de `useFetchLoginWithGoogle` (aucun appelant) avec son type. Les autres gardent le
  même chemin : `login`, `refresh`, `logout`, `register`, `verify-email`, `password/reset`,
  `password/reset-request`, `google/url`, `electron/session` — toutes vérifiées présentes sur
  le Core.
- **Le store n'a pas bougé.** Le contrat de `/users/me` est identique à celui de Java — `id`,
  `email`, `name`, `userType`, `active`, `avatarUrl`, `roles[]`, `modules[]` avec leurs rôles
  imbriqués. Le sur-fetch connu (`roles[].id`, `name`, `description` transmis alors que seuls
  `code` et `active` sont lus) est conservé volontairement : on ne modifie pas la forme des
  données pendant une bascule, sinon un bug devient impossible à attribuer.

**Le développement local a demandé une variable.** Le Core y est un process séparé, sur son
propre port et **sans le préfixe `/api/core`** — celui-ci n'existe qu'en QA et en production, où
le reverse proxy le retire avant de transmettre. D'où `VITE_TOOLS_CORE_BASE_URL`
(`http://localhost:5090` en local), avec repli automatique sur
`${VITE_TOOLS_API_BASE_URL}/api/core` quand elle est absente — donc rien à changer dans les
workflows CI. Ajouter un `UsePathBase` conditionnel côté Core supprimerait ce besoin ; ça n'a
pas été fait.

**Le cookie n'était pas le risque annoncé.** Cette section prévenait que le comportement
cross-domaine du cookie `refresh_token` n'avait jamais été exercé. C'est faux : Java le pose
depuis la même origine (`qa.api.tools.huiitre.fr`), avec des attributs **strictement
identiques** — même nom, `Path=/`, `HttpOnly`, `Secure`, `SameSite=None` en HTTPS, aucun
`Domain`. Le navigateur se comporte donc exactement pareil.

Conséquence à connaître en revanche : **les deux APIs partagent physiquement le même cookie.**
Un login sur le Core écrase celui de Java. Sans danger — elles partagent secret, issuer et
algorithme, donc chacune lit le jeton de l'autre — mais on ne peut pas tenir deux sessions
distinctes dans le même navigateur.

**Ce qui reste :**

1. **Valider en QA** — login, rechargement de page, expiration à 10 minutes, Google OAuth
   (web et Electron), inscription, réinitialisation de mot de passe.
2. **Le module Admin reste sur Java** : `/users`, `/roles`, `/modules` n'existent pas encore sur
   le Core. C'est la condition pour retirer le module core de l'API Java.
3. **Le realtime des notifications reste sur Java** (STOMP), en attente de la tranche SignalR.

### Points restés en suspens

- **`ValidateOnStart()` sur les options JWT** — l'application démarre aujourd'hui avec une
  configuration invalide et n'échoue qu'à la première requête. Proposé, non fait.
- **`UseForwardedHeaders`** — nécessaire si la limitation de débit revient un jour
  (voir `REGISTRATION.md`).
- **`Modules/Users/Domain/User.cs`** est orphelin depuis la suppression du bac à sable de
  création : plus aucun appelant.
- **`SmtpMailOptions` est déclarée dans `SmtpMailSender.cs`**, contrairement à la règle « une
  classe publique est un fichier portant son nom ».
- **`docs/LEARNING.md`** mentionne encore `PATCH /users/password`, devenu `/auth/password`.

Chaque tranche doit pouvoir être testée en QA sans retirer prématurément le comportement Java existant.

### À faire lors de la migration des flux mot de passe

Reproduire le nettoyage existant des demandes de réinitialisation : un
`BackgroundService` Core exécute toutes les 30 minutes la suppression des lignes
expirées de `tools_core.user_password_reset`. Chaque environnement possède sa
propre base : le conteneur QA nettoie `tools_qa` et le conteneur Production sa
base de production ; ils sont donc totalement indépendants.
