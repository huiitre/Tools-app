# API — étude d’architecture et plan de migration

Ce document conserve les décisions prises pour l’extraction progressive du Core historique Java vers une application C# / ASP.NET Core.

> ### ⚠️ Renommage des dossiers, 17/08/2026
>
> `api-core/` est devenu **`api/`**, et l'ancienne API Java **`api-java/`**. Les sections
> historiques ci-dessous parlent encore de `api-core/` (le C#) et de `api/` (le Java) : c'est
> volontaire, elles racontent des décisions prises avant le renommage et les réécrire les
> falsifierait. **Dans le code d'aujourd'hui, `api/` = C# et `api-java/` = Java.** Les chemins
> exécutables (commandes, workflows) ont, eux, été mis à jour.
>
> `api-java` plutôt que `api-legacy` : la migration s'étalera, et « legacy » est une prophétie
> qui vieillit mal. Un dossier de code peut porter le nom de sa technologie — il ne changera
> jamais de langage — là où une URL publique ne le doit pas.

> Statut : migration engagée. L’authentification complète (inscription, connexion, Google,
> session, mot de passe), le profil utilisateur, l'administration et les notifications sont
> implémentés côté C# et servis en production.

## Intention

Le monorepo conserve quatre zones principales :

```text
web/       Frontend Vue
api/       API C# / ASP.NET Core — plateforme transverse et modules métier migrés
api-java/  API métier Java / Spring Boot, figée et vidée au fil des migrations
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

Les claims incluent l’identité et les droits nécessaires, dont le rôle contextuel par module :

```json
{
  "sub": "42",
  "roles": ["ADMIN"],
  "modules": {
    "codename": ["READ_ONLY"],
    "dofus": ["USER"]
  }
}
```

Un use case qui exige `USER` et le module `CODENAME` rejette donc un utilisateur ayant `READ_ONLY` sur ce module, avant toute persistance ou diffusion — **y compris l’administrateur du site ci-dessus**, dont le rôle global ne relève pas son niveau à l’intérieur d’un module.

Le délai de prise en compte d’un changement de droit est au plus la durée de vie de l’access token. Commencer avec des access tokens courts est préférable à une introspection réseau systématique ou à une infrastructure de révocation complexe.

### Ce que fait l’API Java, et ce que le Core en a repris

L’API Java fait cette vérification en base à chaque use case :

1. `ModuleAuthorizationPort.hasAccess` vérifie l’accès au module ;
2. `UserRoleProvider.getUserRole(userId, module)` lit le rôle contextuel dans `tools_core.user_module_role` ;
3. l’aspect compare ce rôle à `SecuredUseCase.requiredRole()`.

**L’intention est conservée à l’identique côté Core : le rôle de module remplace le rôle global, il ne s’y ajoute pas.** Seule la source change — les claims du jeton au lieu de deux requêtes SQL par appel. Le déroulé complet est dans `SECURITY.md` (« Use case appartenant à un module »).

Deux détails du Java n’ont pas été repris, parce qu’ils tiennent à sa manière de lire et non à la règle :

- l’étape 1 est presque redondante avec l’étape 2, qui échoue déjà en l’absence de ligne ; côté Core, un module absent des claims est un refus, sans lecture séparée ;
- le `LIMIT 1` sans tri du Java rendait le rôle retenu arbitraire du temps où `user_module_role` pouvait en contenir plusieurs pour une même paire `(user, module)`. Ce n’est plus le cas : la clé primaire est `(user_id, module_id)` depuis `V2.4.0`, et `LIMIT 1` a été retiré côté Java — la requête ne peut plus ramener qu’une ligne.

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

Les tests HTTP sont dans `api/tests/Tools.Api.IntegrationTests`. Ils démarrent API Core en mémoire avec l'environnement `Testing`, sans ouvrir de port ni appeler PostgreSQL. Les endpoints `/_tests/errors/{kind}` sont mappés uniquement dans cet environnement et ne sont donc pas exposés en Development, QA ou Production.

Ils vérifient le contrat partagé pour 400, 404, 409 et 500, ainsi que la propagation ou génération de `X-Request-Id`. Ils doivent être complétés lorsqu'un nouveau `code` ou un nouveau comportement HTTP est introduit.

```bash
dotnet test api/tests/Tools.Api.IntegrationTests/Tools.Api.IntegrationTests.csproj
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

Le code le disait déjà : `SetUserPasswordUseCase` vit dans `Modules/Core/Auth/Application/Usecases/Password/`.
L'exposer depuis `UsersController` faisait qu'un module publiait le use case d'un autre.

`PATCH /auth/password` ne porte aucun identifiant, comme `/auth/logout` : l'identité vient
toujours du jeton. Une URL qui désignerait l'utilisateur ouvrirait la porte à viser le compte
d'un autre.

## Namespaces et organisation du code

### `Modules/Core/` et les modules métier (2026-08-20)

`Modules/` porte deux natures de modules qui ne se ressemblent pas, et un seul niveau les
mélangeait :

```text
Modules/
  Core/           la plateforme : Access, Admin, Auth, Common, GameServers, Health,
                  Mail, Notifications, Realtime, Security, Users
  <Métier>/       EliteDangerous ; Dofus, Riot, Palworld à venir, à mesure qu'ils sont
                  repris de l'API Java
```

Le critère est **la dépendance, pas le schéma SQL** : un module du Core ne dépend d'aucun
métier, alors que tout métier dépend du Core. Dofus peut disparaître, Security non.

C'est le découpage de l'API Java (`modules/core/…` d'un côté, `modules/dofus`,
`modules/palworld` de l'autre), repris pour que les deux se lisent pareil. Il a été fait
**avant** le premier module métier, précisément parce que chaque module ajouté ensuite aurait
renchéri le déplacement.

Deux cas valent d'être notés, parce que le nom seul induit en erreur :

- **`Health`** n'est pas le module fonctionnel « santé » de l'API Java. C'est la sonde de
  readiness (`SELECT 1`), de l'infrastructure — d'où sa place dans le Core, à l'inverse de
  `modules/health` côté Java. Si le module métier arrive un jour, il prendra
  `Modules/Health/`, et celui-ci devra être renommé.
- **`GameServers`** est un cas limite assumé : c'est une fonctionnalité visible, mais
  transverse à plusieurs jeux et adossée à `tools_core.game_servers`. Le mettre dans un module
  métier obligerait à choisir lequel. S'il devient un module à part entière, il se déplacera
  seul — il ne dépend d'aucun autre module que Common et Security.


### Elite Dangerous, premier module métier (2026-08-24)

`Modules/EliteDangerous/RoadToRiches/` est le premier module repris de l'API Java, dont le module
correspondant a été supprimé dans la foulée. Il a été choisi pour sa taille — une table, sept
routes, aucun ordonnanceur, aucun appel sortant — afin que ce soit la **forme** de la migration qui
soit éprouvée, pas son volume.

Trois décisions valent pour les modules suivants :

- **La route ne reprend pas les abréviations internes.** `/elite-dangerous/expeditions`, et non
  `/elite-dangerous/r2r/expeditions` : la ressource, ce sont les expéditions. La table conserve son
  nom (`tools_elite_dangerous.r2r_expedition`) — renommer une URL ne renomme pas un schéma.
- **Le module métier déclare son `RequiredModule`.** Les sept use cases exigent `RoleCode.User`
  **dans** `ModuleCode.EliteDangerous`, jamais dans le rôle global : un administrateur du site
  absent du module n'y entre pas.
- **Les statuts HTTP sont ceux du sens, pas ceux de l'API Java.** Une expédition introuvable rend
  404 là où le Java rendait 400 — ses `*NotFoundException` étendent `IllegalArgumentException`, que
  son gestionnaire mappe uniformément sur 400. La migration est l'occasion de retrouver les statuts
  justes, `AppException` les portant déjà.

Le domaine lève `AppException.Validation(...)` et dépend donc de
`Core.Common.Application.Exceptions`. C'est une dépendance vers un type d'exception, pas vers de la
logique : l'alternative — une `ArgumentException` de la BCL — finirait en 500 avec son message
masqué, `ApiExceptionHandler` ne connaissant qu'`AppException`.

### Riot, deuxième module métier (2026-08-29)

`Modules/Riot/` reprend le module Riot de l'API Java : deux sous-modules, `Valorant` (catalogue,
comptes liés, boutique, skins possédés, liste de suivi, historique) et `Sync` (rafraîchissement du
catalogue depuis les données de l'extracteur). 31 use cases, 28 routes, deux services, une passe de
fond quotidienne. Là où Elite éprouvait la *forme* de la migration, Riot en éprouve le *volume* :
appels sortants, chiffrement, ordonnanceur, transactions.

Le module Java a été supprimé dans la foulée (119 fichiers), et le front bascule sur
`clientCore` dans le même mouvement : le schéma `tools_riot` ne connaît plus qu'un seul écrivain.
Seul `ModuleCode.RIOT` reste côté Java — c'est l'énumération des droits, partagée avec la base.

Six décisions valent pour les modules suivants :

- **Un rôle porté par la route Java doit être retrouvé, pas perdu.** L'API Java cumulait
  `@RequiredRole` sur la route *et* un rôle dans le use case, parfois différents. Le C# n'a que le
  second : c'est le plus strict des deux qui a été repris. Là où seule la route en portait un —
  `POST /watchlist/admin/sync`, réservé aux administrateurs, dont le notifieur n'exige rien —, il a
  fallu **créer** un use case sécurisé (`TriggerValorantWatchlistSyncUseCase`) ; sans lui l'action
  serait devenue ouverte à tous. À vérifier systématiquement en migrant un contrôleur Java.
- **Les données de l'extracteur sont lues en HTTP, plus sur disque.** L'API Java montait le NAS
  (`tools.assets.base-path`) ; `ValorantAssetsReader` lit `AssetsBaseUrl`, comme
  `GameServersManifestProvider`. Aucun montage à prévoir, et le poste de développement lit la même
  source que la production.
- **Le chiffrement des jetons est compatible octet pour octet avec Java.** `Cipher.doFinal` colle
  le tag d'authentification GCM à la fin du chiffré ; `AesGcm` en .NET veut deux tampons séparés.
  `AesGcmValorantTokenCipher` détache les 16 derniers octets au déchiffrement et les recolle au
  chiffrement. Sans cela, aucun compte déjà lié n'est relisible. La clé est la même
  (`TOOLS_ENCRYPTION_KEY`), exposée sous `Riot__EncryptionMasterKey`.
- **Une passe de fond ne construit jamais un `SecuredUseCase`.**
  `ValorantWatchlistSchedulerService` résout le notifieur, pas le use case de déclenchement : aucun
  utilisateur n'étant authentifié sur ce thread, l'autorisation échouerait dès la construction.
- **Un adaptateur PostgreSQL du module passe par `RiotDatabase`**, qui rejoint la transaction
  ouverte par le use case s'il y en a une et ouvre sa propre connexion sinon. Les deux cas
  existent : l'historique de boutique s'écrit par lot sous transaction depuis le front, et une
  ligne à la fois depuis la passe de fond.
- **Un module métier peut avoir besoin d'une capacité que le Core n'a pas encore.** Le chiffrement
  est déclaré ici comme un port (`IValorantTokenCipher`) plutôt qu'ajouté au Core au passage : il
  remontera le jour où un deuxième module en aura besoin, avec deux usages réels pour en dessiner
  le contrat.

Chaque fichier déclare un **file-scoped namespace** dérivé de son chemin :

```csharp
// Modules/Core/Auth/Application/Usecases/LoginUseCase.cs
namespace Tools.Api.Modules.Core.Auth.Application.Usecases;
```

`Program.cs` fait exception : un fichier à top-level statements ne peut pas déclarer de
namespace. Il reste dans le namespace global et importe explicitement les namespaces des
modules — c'est la racine de composition, elle câble tout, donc elle voit tout.

Les `using` entre modules sont **explicites**, jamais globaux. C'est la raison d'être des
namespaces ici : rendre le couplage inter-modules lisible en tête de fichier. Un
`using Tools.Api.Modules.Core.Mail.Application;` dans un fichier du module Auth signale
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
Modules/Core/Auth/Infrastructure/
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

### Un environnement sans accès à la ressource (2026-08-23)

Certaines ressources ne sont joignables que depuis l'endroit où elles tournent. Le service
WireGuard du module Vpn en est le premier cas : il vit dans la pile réseau du conteneur
WireGuard, sur le NAS. Aucun poste de développement ne peut l'atteindre, et le rapatrier
localement n'a pas de sens — contrairement aux assets, ce n'est pas un fichier mais une
interface réseau.

La réponse est le port lui-même. `IVpnGateway` a deux implémentations, et **c'est
`VpnModule` qui tranche**, sur `builder.Environment` : un adapter en mémoire en Development
et en QA, l'adapter HTTP réel ailleurs. Le use case ne sait rien de ce choix.

Deux conséquences assumées :

- **la sélection se fait sur l'environnement, pas sur la présence de la configuration.** Une
  variable manquante en production fait donc échouer le module au lieu de le faire basculer
  silencieusement sur des données inventées. C'est le comportement voulu.
- **le vrai adapter n'est exécuté dans aucun environnement de test.** Le premier passage réel
  a lieu au déploiement. Le fake déplace ce risque, il ne le supprime pas : la validation de
  l'adapter reste manuelle.

Enfin, un adapter de ce type **échoue bruyamment**. Rendre une liste vide quand le service
est injoignable afficherait « aucun peer » à un administrateur, ce qui est faux et alarmant —
même raisonnement que le manifest des serveurs de jeux.

### Découper les use cases et les ports

Même principe côté Application, avec un découpage par **méthode d'identification** qui
répond en miroir à celui de l'Infrastructure :

```text
Modules/Core/Auth/Application/
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
Modules/Core/Auth/AuthModule.cs          AddAuthModule()
Modules/Core/Common/CommonModule.cs      AddCommonModule()
Modules/Core/Health/HealthModule.cs      AddHealthModule()
Modules/Core/Mail/MailModule.cs          AddMailModule()
Modules/Core/Security/SecurityModule.cs  AddSecurityModule()
Modules/Core/Users/UsersModule.cs        AddUsersModule()
```

Ce fichier vit **à la racine du module**, pas dans une couche : il câble `Api`, `Application`
et `Infrastructure` ensemble, donc il ne peut appartenir à aucune des trois. C'est l'équivalent
.NET idiomatique d'une classe `@Configuration` Java — même granularité, même rôle.

L'extension porte sur **`IHostApplicationBuilder`** et non sur `IServiceCollection` : les
modules ont besoin de `Configuration` pour leurs options et de `Environment` pour ce qui dépend
de l'environnement, et le type de retour rend l'enchaînement lisible. `Program.cs` retombe
alors à une trentaine de lignes, dont la partie qui compte :

```csharp
builder.AddCoreModules();
```

`Modules/Core/CoreModule.cs` enchaîne les onze modules de plateforme dans l'ordre qu'ils
exigent — Common en premier, les autres dépendant de son contrat d'erreur et de son accès
PostgreSQL. Ce n'est pas une couche supplémentaire : chaque module garde son
`<Module>Module.cs` et reste enregistrable seul. Les modules métier s'ajouteront à la suite,
un appel par module :

```csharp
builder.AddCoreModules()
    .AddDofusModule();
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
  Tools.Api.IntegrationTests/
    Fixtures/         ApiWebApplicationFactory
    Fakes/            doubles mémoire des ports, une classe par fichier
    Modules/          miroir des modules du code source, Core/ compris
      Core/
        Auth/         AuthenticationTests, PasswordTests
        Common/       ErrorContractTests
        Mail/         MailControllerTests
        Security/     AuthorizationTests, ModuleAuthorizationTests
  Tools.Api.UnitTests/
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

### Temtem, premier module écrit directement en C# (2026-08-30)

`Modules/Temtem/` n'est pas une migration : le module n'a jamais existé côté Java, seul son
schéma `tools_temtem` avait été créé en V2.24.0 puis laissé vide. La première livraison ne
contient que la synchronisation du catalogue, alimentée par l'extracteur du NAS.

Deux décisions le distinguent des autres modules :

- **Sa sync est une route `internal/`**, pas une route TECH. Les autres extracteurs
  (`update_palworld.sh`, `update_data_doduda.sh`) se connectent sur `/auth/login` avec un compte
  TECH avant d'appeler leur sync ; celui de Temtem présente le secret partagé, comme pour
  `/internal/mail`. Un extracteur n'agit au nom d'aucun utilisateur : lui faire porter un mot de
  passe de compte était le vrai écart. `SyncTemtemCatalogueUseCase` n'est donc pas un
  `SecuredUseCase`, pour la même raison que `SendInternalMailUseCase`.
- **Un fichier source vide interrompt la synchronisation.** Le catalogue se recharge par upsert
  puis suppression de ce qui a disparu de la source : une extraction ratée qui publierait un
  tableau vide viderait la table, et emporterait le reste par cascade. Le garde-fou est dans le
  use case, avant l'ouverture de la transaction.

L'upsert distingue créé, modifié et **inchangé** : `ON CONFLICT DO UPDATE ... WHERE la ligne
IS DISTINCT FROM excluded` empêche PostgreSQL de réécrire une ligne identique, et `RETURNING
(xmax = 0)` dit lequel des trois s'est produit. Une synchronisation horaire sans patch du jeu
rend donc des compteurs à zéro au lieu de prétendre avoir tout réécrit.

## Cible : une seule API C# et des satellites polyglottes

> **Statut : décision prise le 16/08/2026, non planifiée.** Rien de ce qui suit n'est engagé ;
> la section existe pour que l'intention ne se reperde pas.

### Ce qui change par rapport à l'intention initiale

Le découpage `api/` (Java) et `api-core/` (C#) n'a jamais eu de motif d'exploitation — ni
blast radius, ni cadence de déploiement, ni montée en charge. L'intention réelle était
d'isoler **un périmètre assez petit pour être réécrit dans un autre langage**, comme
exercice.

Le Core ne remplit pas ce critère : 136 fichiers, 5 581 lignes, et surtout tout le reste en
dépend. C'était le bon geste appliqué au mauvais morceau — le Core est précisément la partie
qu'on ne réécrit pas.

La cible devient donc :

```text
api/           une application C# modulaire — plateforme transverse ET modules métier
api-java/      l'API Java, figée, vidée au fil des migrations
api-<module>/  satellites optionnels, un par module réécrit dans un autre langage
web/
database/
```

**Fait le 17/08/2026** : l'ancienne `api/` Java est renommée `api-java/` et `api-core/` reprend le
nom `api/`. Elle absorbera les modules métier au fil de l'eau. La frontière Core / métier reste décrite dans ce document — elle
devient une frontière **entre modules d'une même application**, plus entre deux services.

Conséquence assumée : un déploiement raté emporte l'authentification **et** le métier. À
l'échelle du projet c'est acceptable, et l'incident `JWT_SECRET` du 15/08/2026 avait déjà
montré qu'une configuration invalide cassait `/health` sur le Core seul.

### Ce qu'est un satellite

Un satellite sert **un module et un seul**, dans le langage de son choix, et n'implémente
jamais l'authentification : il ne fait que **vérifier** un jeton émis par l'API principale.
Aucun appel au Core par requête métier — la règle posée plus haut ne change pas. Le contrat
exact de cette vérification est dans `SECURITY.md`.

**Le prérequis dur est levé : les rôles de module sont dans les claims** et le Core décide
désormais sans lire `tools_core.user_module_role`. C'était ce qui bloquait tout le reste — un
satellite aurait dû taper le schéma du Core, ce qu'interdit la règle « aucun accès SQL direct
d'un service au schéma détenu par l'autre ». Il lit maintenant un champ du jeton et n'a besoin
d'aucun accès.

Ce que cela ne rend pas fait pour autant : le coût opérationnel décrit plus bas reste entier,
et aucun module métier n'est encore porté par le Core. Le prérequis technique tombe, la
décision de démarrer un satellite reste à prendre.

### Module pilote retenu : todolist

```text
22 fichiers, 1 353 lignes, 7 endpoints
/todolists  et  /todolists/{id}/todos
schéma propre : tools_todolist.todolist, tools_todolist.todo
```

C'est le seul module de cette taille dans le dépôt — assez gros pour être un vrai exercice
(couches, ports, SQL, autorisation, erreurs, déploiement), assez petit pour être fini. Et
surtout : **il possède son propre schéma**, aucune table partagée avec `tools_core`. La
propriété des données se transfère donc proprement avec le module.

Langage envisagé : Rust. Il n'est pas dans le nom du dossier — `api-todolist/`, jamais
`api-todolist-rust/`. Un nom décrit une capacité, pas une implémentation ; sinon il ment le
jour où le module change de langage.

### Bascule et retour arrière

Il n'existe **jamais deux routes publiques** pour le même module : une seule route, deux
implémentations, une seule branchée par le reverse proxy.

```text
/api/todolist/*  ──>  satellite        (routé)
                      module de api/   (présent, non routé)
```

Le frontend ne change pas d'une ligne, et le retour arrière tient dans une règle de proxy —
le même mécanisme qui a permis la bascule de l'authentification.

Deux disciplines qui vont avec :

- **La propriété des données se transfère, elle ne se partage pas.** Jamais deux écrivains
  sur `tools_todolist` en même temps. L'implémentation non routée est du code mort gardé
  comme filet, puis supprimée.
- **Une seule collection Bruno reste à jour**, celle de l'implémentation branchée. La règle
  « toute route dans Bruno » devient sinon ambiguë dès qu'il existe deux implémentations.

### Ce qui a été écarté : une route `/internal/` d'introspection de jeton

L'idée était de faire vérifier le jeton par l'API principale, via un appel Docker à Docker,
pour éviter de réécrire la validation dans chaque langage. Écartée :

- Elle ne supprime pas l'accord à tenir, elle le déplace. Un satellite qui tague ses use
  cases par rôle et par module doit de toute façon comprendre la sémantique des claims ; il
  les recevrait en JSON au lieu de les décoder.
- Elle ajoute un secret d'en-tête `/internal/` à aligner entre conteneurs — exactement le
  type de désalignement qui a coûté la mise en production du 15/08/2026.
- La logique réellement à reproduire est courte : six paramètres de validation et deux
  comparaisons de claims (voir `SECURITY.md`). `isActive` étant un claim et non une lecture
  en base, la vérification locale ne demande **rien** au Core.

L'argument de disponibilité, en revanche, ne tient pas et n'a pas servi à trancher : avec un
access token de dix minutes, une panne du Core tue toutes les sessions de toute façon.
L'indépendance d'un satellite vaut dix minutes.

`/internal/` garde son usage actuel — la publication d'événements vers le Core, **une fois
par action métier**, pas une fois par requête.

### Le coût réel

Écrire le module dans un autre langage n'est pas le morceau difficile. Le coût est
opérationnel, et il est déjà connu pour l'avoir payé une fois avec `api-core` : une image,
un conteneur, un healthcheck, une entrée Watchtower, une règle de reverse proxy, un workflow,
un calcul de version, des secrets alignés. C'est la raison de n'avoir **qu'un** satellite à la
fois, et de vivre plusieurs mois avec le premier avant d'en envisager un second.

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
huiitre/tools_api:qa
huiitre/tools_api:sha-a1b2c3d
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
  - 'api/**'
  - '.github/workflows/api-deploy-qa.yml'
  ```

- Calculer la version depuis les Conventional Commits apparus après le SHA de
  l'image QA `qa` précédente.
- Construire et publier `huiitre/tools_api:qa`.
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
[x] migration frontend de l'authentification
[x] administration sur le Core (/users, /roles, /modules, /admin/stats) — côté API
[x] bascule du module Admin du frontend
[x] rôles et droits de module dans les claims
[ ] SignalR / realtime   ← dernier morceau du Core encore servi par Java
```

La validation locale des JWT par Java n'a pas lieu d'être pour l'instant : les deux APIs
partagent le même `JWT_SECRET`, le même issuer et le même choix d'algorithme HMAC, donc un
jeton émis par le Core est déjà accepté par Java. La question se reposera le jour où le Core
passera à une paire de clés asymétrique.

## Bascule du frontend — faite et validée en QA

L'authentification du front est branchée sur l'API Core. Tout s'est passé dans `web/`, et reste
réversible en repointant `clientCore` sur `/api/v3`.

Vérifié en QA : login, refresh, Google OAuth (web), et — le point qui compte — **le métier Java
répond normalement à un access token émis par le Core**. Le mode mixte tient donc : l'identité
vient du Core, les modules métier restent sur Java. Le reste (inscription, réinitialisation,
Google sous Electron) se validera à l'usage.

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

## Administration portée sur le Core

Les onze routes que consomme le module Admin du frontend existent désormais sur le Core :

```text
GET    /users                                Users
PUT    /users/{id}/role                      Users
GET    /roles                                Security
GET    /modules                              Access
POST   /modules                              Access
PUT    /modules/{id}                         Access
GET    /modules/{id}/users                   Access
POST   /modules/{id}/users/{userId}          Access
PUT    /modules/{id}/users/{userId}/role     Access
DELETE /modules/{id}/users/{userId}          Access
GET    /admin/stats                          Admin
```

**Deux nouveaux modules.** `Access` porte les modules *fonctionnels* de l'application (Dofus,
Palworld…) et les accès des utilisateurs à ces modules. Il ne s'appelle pas « Modules » pour ne
pas produire `Modules/Core/Modules` et un namespace `Tools.Api.Modules.Core.Modules`, où le même mot
désignerait deux choses sans rapport ; `Access` nomme la responsabilité réelle. `Admin` ne
détient aucune ressource — il agrège en lecture ce que possèdent Users et Access, d'où un
module qui ne fait que lire.

**`/roles` est servi par Security**, et non par Users. La table `tools_core.role` est la
contrepartie persistée de `RoleCode`, qui vit déjà là ; et deux modules la consomment — Users
pour le rôle global, Access pour le rôle contextuel. La ranger sous l'un des deux laisserait
croire à une appartenance qui n'existe pas, comme pour un port partagé.

### Le rôle exigé est ADMIN partout — y compris là où Java affiche TECH

L'API Java porte **deux** déclarations de rôle : `@RequiredRole` sur la méthode du contrôleur,
et `requiredRole()` dans le use case. Seule la seconde est appliquée — `UseCaseAuthorizationAspect`
n'intercepte que les implémentations de `SecuredUseCase`, et **aucun aspect ne lit
`@RequiredRole`**. L'annotation est décorative.

L'écart est visible : `RoleController`, `ModuleController` et leurs use cases se contredisent
(`TECH` sur le contrôleur, `ADMIN` dans le use case). C'est ADMIN qui s'applique réellement, et
c'est donc ADMIN qui a été reproduit. Porter les annotations aurait ouvert `/roles` et
`/modules` à des comptes TECH qui n'y ont aujourd'hui pas accès.

**ADMIN est au-dessus de TECH.** L'énumération Java déclare `ADMIN` avant `TECH`, ce qui suggère
l'inverse, mais l'ordre de déclaration ne sert à rien : `RoleHierarchy` donne `TECH = 4` et
`ADMIN = 5`, et c'est lui qui décide. Le `RoleCode` du Core est fidèle à cette hiérarchie. Un
test vérifie explicitement qu'un compte TECH est refusé sur chacune de ces routes.

> Le frontend ordonnait `[…, ADMIN, TECH, OWNER]` dans `auth.store.ts`, plaçant TECH
> au-dessus d'ADMIN à l'inverse des deux APIs. Corrigé avec le passage au rôle unique : la
> hiérarchie vit désormais dans une constante unique, `ROLE_HIERARCHY` de
> `Auth/types/auth.types.ts`, et toute comparaison passe par `roleRank` / `hasAtLeast`. Les
> copies locales du tableau ont disparu — c'étaient elles qui rendaient la divergence
> possible sans que rien ne la signale.

### Un utilisateur = un rôle (2026-08-20)

Un utilisateur détient **au plus un rôle global et au plus un rôle par module**. Ce n'est pas
une convention applicative, c'est la base : `(user_id)` est la clé primaire de `user_role`
depuis `V2.69.0__single_role_per_user.sql`, et `(user_id, module_id)` celle de
`user_module_role` depuis `V2.4.0`.

**La décision était déjà prise, seule la contrainte manquait.** Rien n'a jamais attribué
plusieurs rôles : `ReplaceGlobalRoleAsync` supprimait avant d'insérer, le frontend ne proposait
qu'un rôle, l'API Java lisait avec un `LIMIT 1`. Le cumul ne survivait que dans le schéma des
rôles globaux — et son coût était partout ailleurs : `GroupBy` défensifs à chaque lecture de
profil et de membres de module, arbitrage « le plus permissif l'emporte » recalculé à chaque
autorisation, listes de rôles dans les DTO et dans le claim `modules`. Un arbitrage payé à
chaque requête pour départager des lignes que rien ne créait.

Ce que la contrainte a permis de supprimer :

- `CurrentUser.HighestRole` / `HighestRoleIn` → `Role` et `RoleIn(module)`, de simples lectures ;
- les `GroupBy` de `PostgresUserRepository`, `PostgresAuthRepository` et
  `PostgresModuleMembershipRepository` ;
- les couples `DELETE` + `INSERT` d'attribution de rôle, devenus des upserts `ON CONFLICT` ;
- `UserProfileDto.Roles` / `UserModuleDto.Roles` / `UserAdminDto.Roles` → une valeur unique ;
- `AccessTokenData.Roles`, qui n'était lu par personne ;
- le `LIMIT 1` sans `ORDER BY` de `PostgresUserRoleProvider` côté Java, qui laissait Postgres
  choisir le droit accordé ;
- les trois copies de la hiérarchie des rôles côté frontend, dont une était mal ordonnée.

**Le claim `role` remplace `roles`.** Le tableau n'existait que parce que le cumul était
possible ; sa raison d'être disparaissant, le claim devient une chaîne, et les valeurs de
`modules` aussi. Le Core étant le seul émetteur et le seul lecteur (Java relit la base, le
frontend lit `/users/me`), le changement n'a demandé aucune coordination — seuls les jetons en
vol comptaient, couverts par une tolérance en lecture décrite dans `SECURITY.md`.

**La migration échoue s'il existe un doublon, et c'est voulu.** Une reprise silencieuse
choisirait un droit à la place d'un humain. Le contrôle à passer avant de la jouer :

```sql
SELECT user_id, count(*) FROM tools_core.user_role GROUP BY user_id HAVING count(*) > 1;
```

`V2.69.0` en profite pour supprimer `tools_core.config` et `tools_core.user_config_override`,
créées par `V2.3.0` et jamais lues ni écrites par aucun code. Le système de paramètres qui les
remplace tient son catalogue en dur dans l'API et ne persiste que les valeurs surchargées : une
table de catalogue en base n'y a plus de rôle.

### `PUT /modules/{id}` est un vrai remplacement

L'API Java expose un PATCH déguisé en PUT : `Module.update()` teste chaque champ et ignore ceux
qui valent `null`, si bien qu'un `{ "active": true }` seul ne touche que l'activation. Le Core
retient la sémantique HTTP : **le corps décrit le module dans son intégralité**, `code` et
`name` sont obligatoires, et un champ absent est écrasé.

Le comportement observable ne change pas pour autant. Le seul appelant côté frontend est
`ModuleEditModal.vue`, qui construit son formulaire par `{ ...props.module }` : il envoie
toujours l'objet complet. Aucun appel partiel n'existe dans le code.

La conséquence est à connaître pour la suite : un futur bouton d'activation qui n'enverrait que
`active` fonctionnerait sur Java et serait refusé par le Core, avec `VALIDATION_FAILED`. C'est
la raison pour laquelle `web/AGENTS.md` a été corrigé — il décrivait l'activation comme un envoi
partiel.

### Journalisation : une ligne par requête, une trace par changement de droit

Deux mécanismes distincts, ajoutés ensemble parce qu'aucune des routes d'administration
n'écrivait quoi que ce soit dans les logs — les premiers appels en QA passaient sans laisser la
moindre trace.

**`UseSerilogRequestLogging()`** émet une ligne par requête HTTP avec méthode, chemin, statut et
durée. Il est placé juste après `RequestIdMiddleware`, pour que l'identifiant de corrélation
soit déjà posé et pour englober ce qui échouerait plus loin dans le pipeline.

Le niveau est calculé par requête : `Error` sur une exception ou un 5xx, **`Verbose` sur
`/health*` et `/version`**, `Information` pour le reste. Sans cette exception, les sondes
interrogées toutes les trente secondes par le healthcheck et Watchtower produiraient à elles
seules des milliers de lignes par jour — le dépôt garde d'ailleurs le souvenir d'un incident où
elles avaient rempli les logs.

**Les six écritures d'administration tracent leur acteur** en `Information` : attribution d'un
rôle global, création et modification d'un module, ouverture, changement et révocation d'un
accès. Le message porte toujours l'identifiant de celui qui agit **et** celui de la cible :

```text
Rôle global modifié par userId=3 : cible=42 roleId=4
Accès module accordé par userId=3 : moduleId=1 cible=42 rôle=READ_ONLY
```

Ces lignes sont écrites **après le commit**, jamais avant : une trace ne doit pas affirmer un
changement que la transaction aurait annulé. Les lectures restent muettes — les journaliser à
chaque affichage du panel noierait ce qui compte.

### Un bug d'affichage antérieur, révélé au passage

`POST /modules` et `PUT /modules/{id}` sont typés `Promise<AdminModule>` côté frontend, mais
l'API Java ne renvoie **aucun corps** (`void`, 201 et 204). `createModule` rend donc `""`, et
`AdminModules.vue` fait `store.addModuleLocally("")` : une entrée vide s'ajoute à la liste
jusqu'au prochain chargement. `updateModuleLocally` ne trouve jamais son index et n'actualise
rien.

Le Core reproduit le 204 sur le PUT, et renvoie `{ id }` sur le POST — l'entrée fantôme devient
donc `{ id: 5 }`, toujours sans nom. Le défaut est antérieur à la bascule et n'a pas été
corrigé avec elle : le réparer suppose soit de renvoyer le module complet à la création, soit
de recharger la liste côté frontend. À trancher séparément.

### Ce qui n'a pas été porté

`DELETE /modules/{id}` et `GET /users/{id}` existent côté Java mais ne sont appelés par aucun
écran : ils n'ont pas été repris. `GET /modules/users/{userId}` non plus — le profil renvoyé
par `/users/me` porte déjà les modules de l'appelant.

## Notifications — le socle, avant SignalR

Le Core sait désormais **enregistrer une notification et lui résoudre ses destinataires**. Il
ne sait pas encore la livrer : les routes de lecture, le marquage lu et le temps réel restent
servis par l'API Java, et les deux écrivent dans les mêmes tables `tools_core.notifications`
et `tools_core.user_notifications`.

C'est délibéré. La persistance est la moitié qui ne bougera pas quand SignalR arrivera — le Hub
poussera ce que ce module aura déjà enregistré. Rien de ce qui est écrit ici n'est à jeter.

**Conséquence à connaître** : une notification créée par le Core n'est pas poussée. Le
destinataire la découvre au prochain chargement de page, le frontend n'ayant aucun polling —
seulement un chargement au démarrage puis le flux SSE/WebSocket de Java, qui ignore tout de ce
que le Core vient d'écrire.

### Ciblage

Deux critères, repris à l'identique de l'API Java :

```csharp
SendNotificationCommand.ForUser(userId, ...)        un destinataire
SendNotificationCommand.ForMinRole(RoleCode.Admin, ...)   ADMIN et au-dessus
```

`ForMinRole` s'appuie sur `RoleCodes.CodesAtOrAbove`, construit depuis la même table que
`Parse` : les deux sens ne peuvent pas diverger. Les variantes `global()` et `module()` de Java
n'ont pas été portées — elles n'ont aucun appelant là-bas.

**Deux exclusions, vérifiées contre le comportement Java plutôt que supposées** : les comptes
**TECH** ne reçoivent jamais rien, y compris lorsqu'ils cumulent TECH et ADMIN ; les comptes
désactivés non plus (`findAllIdsByRoleCodes` filtre déjà sur `is_active`).

### Signalement des inscriptions

`AdminSignupNotifier` (module Auth) prévient les administrateurs dans trois cas :

| Flux | Message |
|---|---|
| `RegisterUserUseCase` | Nouvelle inscription — adresse pas encore confirmée |
| `VerifyEmailUseCase` | Inscription confirmée — le compte est actif |
| `CompleteGoogleOAuthLoginUseCase` | Nouvelle inscription via Google |

Trois précautions :

- **Seule une création est signalée.** Une inscription reprise avant confirmation ne crée aucun
  compte ; l'annoncer brouillerait la lecture. Google distingue de même la première connexion
  des suivantes, via `GoogleAuthenticationResult.AccountCreated`.
- **Google n'a pas d'étape de confirmation** : l'adresse est garantie par le provider, le compte
  naît actif. Il n'y a donc qu'une notification, pas deux.
- **Un échec de notification ne fait jamais échouer une inscription.** Quand on arrive là, le
  compte est créé et l'email parti ; l'erreur est journalisée puis absorbée. C'est une
  information pour les administrateurs, pas une étape du flux.

### Le contrat Java → Core, quand le realtime migrera

Les deux producteurs métier — `AlmanaxSubscriptionNotifier` et `ValorantWatchlistNotifier` —
restent en Java par conception. Ils devront publier vers le Core par **appel HTTP interne**, sur
le réseau Docker et non par le reverse proxy public, après commit et sans faire échouer la
commande métier si l'appel rate.

Reste à trancher leur authentification : le Core exige un jeton sur toutes ses routes
(`FallbackPolicy`). Soit un compte de service, soit une route `/internal/*` non exposée par le
proxy et protégée par un secret d'en-tête — la seconde évite d'inventer un utilisateur qui
n'existe pas.

## Déploiement en production — trois pièges rencontrés

La mise en production du 15/08/2026 a livré d'un coup l'authentification, l'administration et la
bascule du frontend. Deux incidents, tous deux d'ordre opérationnel et non applicatif. Un
troisième s'est ajouté le 17/08/2026, du côté du reverse proxy.

**Un `JWT_SECRET` désaligné entre les deux APIs boucle sur la déconnexion.** Le secret avait été
changé sur le Core mais pas répercuté sur l'API Java du même environnement. Le symptôme est
trompeur : la connexion **réussit**, `/users/me` répond 200, puis l'écran renvoie « Votre session
a expiré » en boucle. L'enchaînement est le suivant — une route métier Java répond 401 parce
qu'elle refuse la signature, l'intercepteur du front renouvelle le jeton auprès du Core avec
succès, rejoue la requête, se prend un second 401, constate que `_retry` vaut déjà `true` et
déconnecte l'utilisateur.

Le diagnostic tient en une comparaison :

```bash
docker exec <core> env | grep JWT_SECRET
docker exec <java> env | grep JWT_SECRET
```

Tant que le module core vit dans les deux APIs, **le secret se change sur les deux à la fois**,
avec `docker compose up -d --force-recreate` — un `restart` conserve l'environnement existant.

**Une migration non appliquée ne se voit pas au démarrage.** `V2.65.0` (colonne
`email_verified_at`) n'était pas passée en production. L'application démarre pourtant sans
broncher : seul `EmailVerificationCleanupService` échoue, toutes les trente minutes, avec un
`42703: column u.email_verified_at does not exist`. Le login n'y touche pas — mais l'inscription
et la confirmation d'adresse sont hors service, sans que rien ne l'annonce.

À retenir pour les prochaines tranches : **appliquer les migrations avant de déployer le Core**,
sans compter sur le workflow, puisque les trois pipelines (`database/**`, `api/**`,
`web/**`) se déclenchent en parallèle sur le même merge et qu'aucun n'attend les autres. Le
risque symétrique existe côté frontend : si l'image web arrive avant celle du Core, le front
appelle des routes qui n'existent pas encore et plus personne ne peut se connecter. Neutraliser
Watchtower sur le conteneur web le temps que le Core soit à jour évite cette fenêtre.

**Une « erreur CORS » qui n'en est pas une : nginx garde l'adresse du conteneur en cache.**
Incident du 17/08/2026. Après une mise à jour Watchtower, le frontend de production a renvoyé des
erreurs CORS **en boucle et de façon permanente** — pas quelques secondes, treize minutes, jusqu'à
un redémarrage manuel de Nginx Proxy Manager. L'API Core, elle, tournait et se déclarait
`healthy`.

La preuve tient dans ses journaux : `Application started` à 17:08:11, puis **aucune requête
reçue** jusqu'à 17:21:23, l'instant du redémarrage du proxy. Côté nginx, au même moment :

```text
connect() failed (111: Connection refused) while connecting to upstream,
request: "OPTIONS /api/core/hub/negotiate", upstream: "http://172.18.0.22:8080/..."
```

L'enchaînement :

1. Watchtower ne redémarre pas un conteneur, il le **recrée** — donc nouvelle IP sur le réseau Docker.
2. nginx continue d'écrire vers l'ancienne adresse et prend un `Connection refused` sur **tout**,
   y compris le `OPTIONS` de préflight — la toute première requête de chaque appel.
3. Une réponse produite par nginx (502) ne porte **aucun en-tête CORS**, contrairement aux réponses
   de l'application, qui les portent même en 401 ou 404. Le navigateur n'annonce donc pas « API
   injoignable » mais « No 'Access-Control-Allow-Origin' header ».

**La cause exacte est une incohérence de Nginx Proxy Manager**, et elle explique pourquoi l'API
Java n'a jamais connu ce problème en deux ans :

| | ce que NPM génère | conséquence |
|---|---|---|
| destination principale du proxy host (API Java) | `proxy_pass $forward_scheme://$server:$port;` | `proxy_pass` **avec variable** → nginx re-résout le nom à chaque requête, jamais de cache |
| *custom location* (`/api/core/`) | `proxy_pass http://tools_api_core:8080;` | `proxy_pass` **littéral** → nom résolu une seule fois au démarrage, IP gardée à vie |

Les deux se remplissent de la même façon dans l'interface, rien n'indique la différence. Tout
service branché par une custom location est donc exposé, prod comme QA.

**Correctif retenu : une IP fixe sur les conteneurs du Core** (`ipv4_address` dans leur
`docker-compose.yml`, réseau déclaré `external: true`, adresses hautes hors de la plage
d'attribution automatique). Le nom résout alors toujours vers la même adresse et le cache d'nginx
devient sans effet — sans toucher au reverse proxy, dont dépend tout le reste de la machine.
Watchtower réutilisant la configuration du conteneur existant, l'IP survit aux mises à jour.
Un dernier redémarrage de NPM est nécessaire après le changement, pour purger l'adresse périmée.

L'autre voie — réécrire la location dans l'*Advanced* du proxy host avec `resolver 127.0.0.11`,
une variable et le `rewrite` d'origine — fonctionne aussi, mais impose de reproduire à la main les
en-têtes `Upgrade`/`Connection` du hub SignalR. À noter si le sujet revient : **on ne peut pas se
contenter d'ajouter un `proxy_pass` dans le champ *Advanced* d'une custom location** — NPM insère
ce texte avant le sien, nginx voit deux `proxy_pass` dans le même bloc et refuse de démarrer,
coupant tous les hôtes du proxy.

### Points restés en suspens

- **`ValidateOnStart()` sur les options JWT** — l'application démarre aujourd'hui avec une
  configuration invalide et n'échoue qu'à la première requête. Proposé, non fait.
- **`UseForwardedHeaders`** — nécessaire si la limitation de débit revient un jour
  (voir `REGISTRATION.md`).
- **`Modules/Core/Users/Domain/User.cs`** est orphelin depuis la suppression du bac à sable de
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
