# Journal d'apprentissage ASP.NET Core

Ce document est la mémoire opérationnelle du passage de l'API Java Tools vers `api-core` en C# / ASP.NET Core. Il sert à reprendre une session sans devoir reconstruire le contexte.

Pour le plan produit et les frontières de migration, voir `ARCHITECTURE.md` : il reste séparé car il décrit la cible, tandis que ce journal décrit ce qui a réellement été appris et mis en place.

## État de reprise — 12 août 2026

Le projet réel est maintenant dans :

```text
/home/huiitre/development/perso/tools/tools-app/api-core
```

Il s'agit d'une application .NET 10 nommée `Tools.ApiCore`, créée avec le template minimal `dotnet new web`. `api-core` est encore non versionné dans le monorepo ; aucun commit ni push n'a été fait.

Le projet C# n'est relié à rien en production : il ne modifie ni `api/` (Spring Boot), ni `web/`, ni Docker, ni le reverse proxy. Le démarrer localement est sans impact sur l'API Java.

## Démarrage local

Depuis `tools-app/api-core` :

```bash
DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1 dotnet watch run
```

Le profil Development écoute sur `http://localhost:5090`.

Endpoints actuellement validés :

```text
GET /health  → { "status": "ok" }
GET /version → version configurée + environnement courant
POST /users  → endpoint d'exercice transactionnel
```

## Configuration et environnements

`Program.cs` lit :

```csharp
var applicationVersion = builder.Configuration["Application:Version"]
    ?? "unknown";
```

`GET /version` retourne la version, le SHA Git et `app.Environment.EnvironmentName`. La valeur de l'environnement provient de `ASPNETCORE_ENVIRONMENT` :

```text
Development → appsettings.Development.json
QA          → appsettings.QA.json
Production  → appsettings.Production.json
```

`appsettings.json` contient actuellement `Application:Version = 0.1.0`. Au build Docker, les arguments `APPLICATION_VERSION` et `GIT_SHA` alimentent respectivement les variables d'environnement `Application__Version` et `Application__GitSha`, sans modifier le code. `/version` permet donc d'identifier précisément l'image exécutée.

Pour rester cohérent avec les Compose existants, l'application accepte aussi les variables `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USERNAME` et `DB_PASSWORD`. Lorsqu'elles sont toutes présentes, `Program.cs` construit la chaîne PostgreSQL avec `NpgsqlConnectionStringBuilder`. La forme ASP.NET Core standard `ConnectionStrings__Postgres` reste acceptée comme alternative, notamment pour le développement local.

Le `Dockerfile` est multi-stage : le SDK .NET compile et publie l'application, tandis que l'image finale n'embarque que le runtime ASP.NET Core et les fichiers publiés. Elle écoute en interne sur le port `8080` et s'exécute avec l'utilisateur non-root fourni par l'image officielle. Le Compose QA/Production définira l'environnement et les secrets ; il reste à créer.

Validation effectuée le 12 août 2026 :

```bash
docker build \
  --build-arg APPLICATION_VERSION=0.1.0 \
  --build-arg GIT_SHA=test-sha \
  -t tools-api-core:test .
```

L'image a démarré avec `ASPNETCORE_ENVIRONMENT=QA` et une chaîne PostgreSQL factice, suffisante pour les routes sans accès à la base. `GET /version` a retourné `{"version":"0.1.0","gitSha":"test-sha","environment":"QA"}` et `GET /health` a retourné `{"status":"ok"}`. Le conteneur de test éphémère a ensuite été arrêté.

En QA et Production, le Compose fournit `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USERNAME` et `DB_PASSWORD`, comme les services Java existants. Sans ces cinq variables (ou l'alternative `ConnectionStrings__Postgres`), le démarrage échoue volontairement avec `Connection string Postgres manquante`.

## DI, DDD et PostgreSQL

Le dossier `Modules/Users` a été copié depuis le prototype pour servir de support d'apprentissage. Il est organisé en :

```text
Api / Application / Domain / Infrastructure
```

Principes retenus :

- Le use case dépend de `IUserRepository` et de `ITransactionManager`, pas de Npgsql ou Dapper : c'est le DIP.
- `Program.cs` est le *composition root* : il relie les abstractions aux implémentations par DI.
- `AddSingleton` est utilisé pour `NpgsqlDataSource` (pool de connexions partagé).
- `AddScoped` est utilisé pour la session PostgreSQL, les repositories, le transaction manager et les use cases (une instance par requête HTTP).

Packages installés :

```text
Dapper 2.1.79
Npgsql 10.0.3
```

Comparaison Java :

```text
Dapper + SQL explicite ≈ JdbcTemplate
Npgsql direct          ≈ JDBC brut
```

`GetAllAsync()` / `CreateAsync()` utilisent Dapper ; `GetAllNative()` / `CreateNative()` montrent l'alternative Npgsql directe.

## Transactions

L'équivalent explicite de `@Transactional` est placé au début du use case :

```csharp
await using var transaction = await transactionManager.BeginAsync();
```

`await using` se transforme conceptuellement en `try/finally` et appelle `DisposeAsync()` à la sortie de la méthode, même si une exception est levée. `PostgresTransaction.DisposeAsync()` fait un rollback de sécurité si aucun `CommitAsync()` n'a terminé la transaction.

`PostgresSession` contient la connexion et transaction partagées ; Dapper les reçoit à chaque requête. Une écriture repository ne fait donc jamais son propre commit.

### Exercice de commit validé

`ListUsersUseCase.Execute()` ouvre une transaction, crée trois utilisateurs de démonstration, puis appelle explicitement `CommitAsync()`. Le contrôleur peut donc répondre cohérentement `204 No Content`.

Laisser le bloc `await using` se terminer sans `CommitAsync()` (ou lever une exception avant ce commit) provoque toujours le rollback de sécurité dans `DisposeAsync()`. Ce sera le support du prochain exercice, une fois la gestion centralisée des erreurs JSON ajoutée.

Validation effectuée le 12 août 2026 : `dotnet build --no-restore` réussit sans avertissement ni erreur. L'instance locale déjà active sur `http://127.0.0.1:5090` a répondu `204` à `POST /users` ; elle n'a pas été arrêtée ni remplacée pendant la vérification.

## Logs

Le code emploie `ILogger<T>` ; Serilog est le provider configuré dans `Program.cs` via :

```csharp
builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());
```

Packages installés :

```text
Serilog.AspNetCore 10.0.0
Serilog.Settings.Configuration 10.0.1
Serilog.Sinks.Console 6.1.1
Serilog.Sinks.File 7.0.0
```

Politique actuelle :

| Environnement | Niveau | Destinations | Rétention |
|---|---|---|---|
| Development | Debug | Console + `logs/app-*.log` | 7 jours |
| QA | Debug | Console + `logs/app-*.log` | 14 jours |
| Production | Information | Console + `logs/app-*.log` | 30 jours |

Chaque environnement vit dans son propre conteneur, donc le chemin reste le même à l'intérieur de chaque conteneur. Les logs sont exclus par `.gitignore`.

Le thème ANSI Serilog est activé en Development. Avec `dotnet watch`, ne pas forcer `applyThemeToRedirectedOutput=true` : `dotnet watch` capture la sortie pour détecter l'URL et les codes ANSI corrompent alors le lien ouvert.

## Diagnostics de santé

Le module `Modules/Health` porte les routes HTTP de diagnostic dans
`Api/HealthController.cs`. `Program.cs` reste le *composition root* : il
relie le port `IHealthRepository` à son adapter `PostgresHealthRepository`,
sans définir les routes du module.

```text
GET /health        → compatibilité, { "status": "ok" }
GET /health/live   → processus ASP.NET Core vivant, sans dépendance externe
GET /health/ready  → PostgreSQL accessible ; retourne 503 sinon
```

`/health/ready` suit le flux `HealthController` →
`CheckReadinessUseCase` → `IHealthRepository` →
`PostgresHealthRepository`. Cet adapter ouvre une connexion issue du
`NpgsqlDataSource` partagé et exécute `SELECT 1` avec Dapper. Le `HEALTHCHECK`
Docker appelle exclusivement `/health/live` : une base indisponible ne doit pas
provoquer le redémarrage du processus sain.

Validation du 13 août 2026 : `dotnet build --no-restore` réussit sans
avertissement ni erreur. L'API Development répond `200` à `/health` et
`/health/live`. `/health/ready` répond `503` tant que PostgreSQL n'est pas
joignable ; c'est le résultat attendu du check.

## Ce qui a été comparé à Java / EasyMobile

- `IUserRepository` dans le use case : DIP ; l'enregistrement `AddScoped<IUserRepository, PostgresUserRepository>()` : DI.
- ASP.NET Core enregistre des règles DI dans `Program.cs`, puis résout les objets après `builder.Build()`. L'ordre des `Add...` ne compte pas, sauf si une même interface est enregistrée plusieurs fois (la dernière gagne).
- EasyMobile/Inversify a des contraintes d'ordre seulement lorsqu'un `container.get(...)` est exécuté pendant le bootstrap ; les `bind(...)` seuls ne construisent pas les objets.
- Le pipeline HTTP et les middlewares sont le prochain grand sujet : il accueillera ensuite CORS, gestion globale des erreurs, JWT et autorisation.

## Suite recommandée

1. Exécuter manuellement le workflow QA une première fois afin de publier `huiitre/tools_api_core:qa` en version de bootstrap `0.1.0` et de valider les secrets Docker Hub.
2. Créer le Compose QA directement sur le NAS, avec Watchtower limité à `tools_api_core_qa`.
3. Vérifier la chaîne complète, puis ajouter la route reverse proxy QA de diagnostic.
4. Reprendre ensuite la gestion centralisée des erreurs JSON et l'exercice de rollback dans `Users`.

## Règle de travail

À chaque découverte ou décision, mettre à jour ce journal avec : le résultat réel, les commandes ou fichiers significatifs, et le prochain point de reprise.
