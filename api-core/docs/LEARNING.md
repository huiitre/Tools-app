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

## Déploiement Production

Les workflows QA et Production calculent la même version à partir de tous les
commits conventionnels qui modifient `api-core/`, dans leur ordre
chronologique. Ils publient respectivement `huiitre/tools_api_core:qa` et
`huiitre/tools_api_core:latest` pour Watchtower, ainsi qu'un tag
`sha-<SHA>` pour identifier ou restaurer une image précise. Cette règle rend
la version identique après un merge QA vers `master`.

## Ce qui a été comparé à Java / EasyMobile

- `IUserRepository` dans le use case : DIP ; l'enregistrement `AddScoped<IUserRepository, PostgresUserRepository>()` : DI.
- ASP.NET Core enregistre des règles DI dans `Program.cs`, puis résout les objets après `builder.Build()`. L'ordre des `Add...` ne compte pas, sauf si une même interface est enregistrée plusieurs fois (la dernière gagne).
- EasyMobile/Inversify a des contraintes d'ordre seulement lorsqu'un `container.get(...)` est exécuté pendant le bootstrap ; les `bind(...)` seuls ne construisent pas les objets.
- Le pipeline HTTP et les middlewares sont le prochain grand sujet : il accueillera ensuite CORS, gestion globale des erreurs, JWT et autorisation.

## Module Mail — état de reprise du 13 août 2026

### But

L'API Core devient l'unique propriétaire de l'envoi SMTP. L'API Java ne devra
plus avoir de dépendance SMTP : elle appellera l'endpoint Core pour demander un
envoi. API Core utilisera directement le même service lors de la migration de
la récupération de mot de passe.

### Audit Java réalisé

Deux implémentations Java SMTP existent encore et n'ont pas été modifiées :

- `modules/core/auth/infrastructure/mail/AuthMailSenderService` envoie les
  mails de vérification d'email et de réinitialisation de mot de passe ;
- `modules/core/mail/infrastructure/MailSenderService` supporte le texte et
  les pièces jointes vers la liste `MAIL_TO`, mais n'a actuellement aucun
  consommateur effectif.

La configuration Java actuelle est OVH SMTP : `ssl0.ovh.net:587`, authentifiée,
avec STARTTLS et les variables `MAIL_USERNAME` / `MAIL_PASSWORD`.

Les règles des flux auth Java actuels sont : un token sécurisé de 32 octets,
encodé Base64 URL, valable 30 minutes ; une seule demande active par utilisateur ;
la vérification d'email a un délai de renvoi de 5 minutes. La demande de reset
répond toujours avec succès, y compris si le compte n'existe pas ou n'a pas le
provider `PASSWORD`.

### Architecture Core retenue

Il n'y a pas de dossier `Domain` : envoyer un mail n'a pas de règle métier
propre. Le module suit strictement :

```text
MailController
  → SendMailUseCase
    → MailService
      → IMailSender
        → SmtpMailSender
```

Fichiers :

```text
Modules/Mail/
├── Api/MailController.cs
├── Application/
│   ├── SendMailCommand.cs
│   ├── Ports/IMailSender.cs
│   ├── Services/MailService.cs
│   └── Usecases/SendMailUseCase.cs
└── Infrastructure/SmtpMailSender.cs
```

- Le contrôleur dépend seulement de `SendMailUseCase`.
- `SendMailUseCase` est la façade HTTP : il délègue uniquement à `MailService`.
- Toute la validation réutilisable est dans `MailService`.
- Les autres use cases doivent injecter directement `MailService`, jamais
  appeler `SendMailUseCase`.
- `IMailSender` est le port. `SmtpMailSender` est l'adaptateur actuel ; un
  `SendmailMailSender` pourra le remplacer ultérieurement sans modifier les
  use cases ni le service.

### Contrat HTTP actuel

`POST /mail` accepte `to`, `subject`, `text` ou `html`, et optionnellement des
pièces jointes `{ fileName, contentType, contentBase64 }`. La réponse est
`204 No Content` lorsque SMTP accepte le message. Les détails et un exemple
JSON sont dans `docs/MAIL.md`.

Les pièces jointes sont transférées en Base64 : Java ne transmet jamais un
chemin de fichier qui ne serait pas accessible depuis le conteneur Core.

La route n'a pour l'instant aucune authentification applicative : elle doit
rester limitée au réseau Docker interne et ne pas être publiée par le reverse
proxy. Toute future exposition devra définir explicitement son mécanisme
d'authentification inter-service.

### Configuration Core à fournir au Compose QA

```text
Mail__Smtp__Host=ssl0.ovh.net
Mail__Smtp__Port=587
Mail__Smtp__Username=...
Mail__Smtp__Password=...
Mail__Smtp__EnableSsl=true
Mail__Smtp__FromAddress=admin@huiitre.fr
Mail__Smtp__FromName=Tools - Huiitre
```

Les secrets ne sont pas versionnés. Si la configuration SMTP est absente,
`SmtpMailSender` retourne l'erreur `503 MAIL_NOT_CONFIGURED`.

Les échecs SMTP sont convertis par le handler global en
`503 MAIL_DELIVERY_UNAVAILABLE`.

### Validation effectuée

```bash
dotnet test api-core/tests/Tools.ApiCore.IntegrationTests/Tools.ApiCore.IntegrationTests.csproj --no-restore
```

Résultat final : 7 tests réussis. Les tests remplacent `IMailSender` par un
expéditeur mémoire et vérifient que `POST /mail` transmet bien le sujet, le
contenu et une pièce jointe décodée au flux Application.

### Envoi SMTP réel validé en local

Un envoi réel vers `admin@huiitre.fr` a été effectué : réponse `204 No Content`
et log `SmtpMailSender — Email envoyé recipients=1`. La pièce jointe Base64 est
passée dans le même appel. Le SMTP OVH accepte donc le message tel que
`SmtpMailSender` le construit.

Le chemin d'erreur a aussi été vérifié sur une instance sans configuration :
`POST /mail` renvoie bien `503` avec `code: MAIL_NOT_CONFIGURED` au format
`application/problem+json` produit par le handler global.

### Configuration locale de développement

`Program.cs` charge désormais `appsettings.Local.json` (optionnel, déjà présent
dans `api-core/.gitignore`) :

```csharp
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
```

Ce fichier porte la section `Mail:Smtp` avec les identifiants OVH en local. Il
n'est pas versionné. Les mêmes identifiants existaient déjà côté Java sous les
variables d'environnement `MAIL_USERNAME` / `MAIL_PASSWORD`.

### Requêtes Bruno

Le dossier `bruno/Tools API Core/Mail/` a été ajouté (seq 3, après Diagnostics
et Auth), avec `Send mail` et `Send mail with attachment`. Elles suivent le
format déjà en place : URL `{{apiCoreUrl}}`, `auth: type none`, destinataire
`{{authEmail}}`, bloc `settings` identique aux autres requêtes.

### Exposition réelle de la route

L'hypothèse écrite plus haut — « route interne au réseau Docker » — est fausse :
l'API Core est publiée par le reverse proxy, comme le prouve le `RedirectUri`
Google `https://qa.api.tools.huiitre.fr/api/core/auth/callback/google`, que le
navigateur doit pouvoir atteindre. `POST /mail` est donc joignable depuis
l'extérieur et permet aujourd'hui d'envoyer depuis `admin@huiitre.fr` avec
SPF/DKIM valides. Le CORS n'y change rien : c'est une protection navigateur,
pas serveur.

Le blocage se fera **au niveau du use case**, comme dans l'API Java où aucune
route n'est sécurisée et où seuls les use cases le sont. C'est précisément la
raison pour laquelle les endpoints n'appellent que des use cases. Une première
tentative de sécurisation par le pipeline HTTP (`AddJwtBearer`, `[Authorize]`,
policy d'autorisation) a été annulée : elle ne correspondait pas à
l'architecture du monorepo.

### Point d'arrêt exact

Le module Mail Core est fonctionnel et validé de bout en bout en local, mais
toujours non commité, et la route n'a aucune restriction d'accès. Le Compose QA
n'a pas encore reçu les variables SMTP, et aucun code Java n'a été modifié.

`dotnet test` : 7 tests réussis.

### Suite recommandée

1. Porter le système de sécurité des use cases de l'API Java dans le Core, et restreindre `SendMailUseCase` aux rôles d'administration.
2. Ajouter les variables SMTP au Compose QA et rejouer l'envoi depuis l'environnement QA.
3. Migrer le recovery password dans Core : le use case injectera directement `MailService`, sans passer par la route HTTP.

## Règle de travail

À chaque découverte ou décision, mettre à jour ce journal avec : le résultat réel, les commandes ou fichiers significatifs, et le prochain point de reprise.
