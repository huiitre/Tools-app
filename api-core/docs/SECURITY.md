# Sécurité

Les droits sont contrôlés **au niveau des use cases**, jamais au niveau des routes. C'est la raison pour laquelle un endpoint n'appelle qu'un use case : c'est le seul point de passage, donc le seul endroit où placer la règle.

L'**authentification**, elle, est bien portée par le pipeline HTTP — savoir qui appelle est une question technique, pas métier. Les deux étages sont décrits plus bas.

## Sécuriser un use case

Hériter de `SecuredUseCase<TCommand>` (ou `SecuredUseCase<TCommand, TResult>` s'il retourne un résultat), déclarer le rôle minimum, et implémenter `Handle` :

```csharp
public sealed class SendMailUseCase(UseCaseAuthorizer authorizer, MailService mailService)
    : SecuredUseCase<SendMailCommand>(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    protected override Task Handle(SendMailCommand command, CancellationToken cancellationToken) =>
        mailService.Send(command, cancellationToken);
}
```

`Execute` appartient à la classe de base et n'est pas virtuelle : une classe dérivée ne peut ni la remplacer, ni oublier d'appeler l'autorisation. Un use case sécurisé ne peut donc pas exister sans son contrôle — contrairement à un marquage par interface, où l'oubli du marqueur passe inaperçu.

Un use case non sécurisé reste une classe ordinaire : l'absence d'héritage est un choix visible à la lecture.

### Use case appartenant à un module

Un use case métier déclare en plus son module. Le rôle exigé est alors cherché **dans ce module**, pas parmi les rôles globaux :

```csharp
public sealed class CreateTodolistUseCase(UseCaseAuthorizer authorizer, ...)
    : SecuredUseCase<CreateTodolistCommand>(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.User;
    protected override ModuleCode? RequiredModule => ModuleCode.Todolist;
}
```

`RequiredModule` vaut `null` par défaut : les use cases transverses du Core (administration, compte, mail) ne relèvent d'aucun module et restent jugés sur le rôle global.

Le module se désigne par l'énumération `ModuleCode`, jamais par une chaîne. Une chaîne libre ne se trompe qu'à l'exécution, et un module que personne ne reconnaît est un contrôle qui ne s'applique à personne. Les valeurs y sont celles de `tools_core.module.code` (minuscules, convention héritée de `ModuleCode.name().toLowerCase()` côté Java) : les deux applications lisent la même colonne, elle ne peut pas être réécrite d'un seul côté.

### Rôle global et rôle de module ne se cumulent pas

C'est la règle à ne pas réinventer :

| Use case | Rôle comparé |
|---|---|
| sans module | le plus permissif des rôles globaux |
| avec module | le plus permissif des rôles **détenus dans ce module** |

Un administrateur du site absent d'un module ne peut pas y entrer ; présent en `READ_ONLY`, il y est `READ_ONLY`. Sans cette règle, un rôle global élevé serait un passe-partout métier et un droit de module ne voudrait plus rien dire pour les personnes qu'il vise en premier. C'est aussi le comportement de l'API Java, à laquelle le Core reste ici identique.

Deux refus distincts en découlent, que le frontend ne traite pas de la même manière :

- `403 NO_MODULE_ACCESS` — le module n'est pas ouvert à cet utilisateur ;
- `403 INSUFFICIENT_ROLE` — il l'est, mais le rôle qu'il y détient ne suffit pas.

## Hiérarchie des rôles

```text
READ_ONLY (1) < USER (2) < MODERATOR (3) < TECH (4) < ADMIN (5) < OWNER (6)
```

Identique à l'API Java. Le niveau est porté par la valeur de l'énumération `RoleCode` : il n'y a pas de table de niveaux à maintenir à côté. Un rôle satisfait l'exigence dès qu'il est **supérieur ou égal** au rôle demandé.

## Déroulé du contrôle

1. `ICurrentUserProvider` fournit l'appelant : identifiant et rôles. L'implémentation HTTP se contente de traduire les claims de `HttpContext.User` — la validation du jeton a déjà eu lieu dans le middleware d'authentification, une seule fois pour la requête.
2. Sans utilisateur identifié : `401 UNAUTHENTICATED`. Avec un token invalide ou expiré : `401 INVALID_ACCESS_TOKEN`. Sur une route protégée, ces refus viennent désormais du middleware, avant même d'atteindre le contrôleur ; `UseCaseAuthorizer` conserve le sien pour les appels hors HTTP.
3. Les rôles proviennent des claims `roles` (global) et `modules` (par module) de l'access token, gravés à l'émission par `AuthSessionService`. **Aucune requête n'est faite lors de l'autorisation.**
4. Rôle insuffisant ou inexistant : `403 INSUFFICIENT_ROLE`, ou `403 NO_MODULE_ACCESS` si le use case appartient à un module auquel l'appelant n'a aucun accès. La réponse ne révèle pas le rôle attendu ; la tentative est journalisée.

Un utilisateur peut cumuler plusieurs rôles, globalement comme à l'intérieur d'un module : c'est **le plus permissif** qui détermine son niveau effectif. La table `user_module_role` n'a pas de contrainte d'unicité sur `(user_id, module_id)` — ce cumul n'est donc pas théorique. Un code de rôle ou de module inconnu de l'énumération est ignoré plutôt que d'accorder un droit.

## Fenêtre de révocation — choix assumé

Les droits sont lus dans le token, pas en base. Un rôle retiré ne prend donc effet qu'au **renouvellement de l'access token** (`AccessTokenTtlSeconds`, 10 minutes par défaut) — pas immédiatement. C'est le compromis classique du JWT : aucune requête par appel, au prix d'une fenêtre bornée.

Le refresh, lui, relit la base (`RefreshSessionUseCase`) et refuse de renouveler un compte désactivé : la fenêtre ne peut jamais dépasser la durée de vie de l'access token en cours.

Si une révocation immédiate devient nécessaire, la réponse n'est pas de relire la base à chaque appel mais d'ajouter une **denylist** partagée (Redis, par exemple) : petite, écrite rarement, lue très vite. Un cache de rôles avec TTL ne supprimerait pas la fenêtre, il la déplacerait.

Ce point diverge de l'API Java, qui relit les rôles en base à chaque use case. Divergence connue et acceptée ; le Java pourra s'aligner plus tard.

## Différences assumées avec l'API Java

- L'aspect Java laisse passer un appel sans utilisateur identifié, en comptant sur Spring Security en amont. Le Core refuse malgré sa propre `FallbackPolicy` : un use case sécurisé doit rester sûr **seul**, puisqu'il sera appelé depuis un hub SignalR ou une tâche de fond où aucun middleware HTTP ne s'exécute. C'est de la défense en profondeur, pas une redondance.
- Java relit les rôles en base à chaque use case ; le Core les lit dans le token (voir la fenêtre de révocation ci-dessus).
- `PostgresUserRoleProvider` côté Java prend le premier rôle trouvé (`LIMIT 1` sans tri), ce qui est non déterministe pour un utilisateur qui en cumule plusieurs. Le Core retient le plus élevé.
- Le contrôle Java repose sur un aspect AOP qui intercepte `execute(..)` ; le Core le porte par héritage, sans proxy dynamique ni dépendance supplémentaire.

## Compatibilité des tokens entre les deux API

Les deux API signent avec le même `JWT_SECRET`, le même issuer et le même choix d'algorithme HMAC, et écrivent les mêmes claims : `roles` en tableau JSON, `modules` en objet. La bascule vers la lecture des rôles dans le token n'a touché que la **lecture** côté Core : un token émis par l'une reste lisible par l'autre.

Le claim `modules` associe un code module aux rôles qu'y détient l'utilisateur :

```json
"modules": { "todolist": ["USER"], "palworld": ["ADMIN"] }
```

La valeur est un tableau parce que le cumul est possible en base ; le Core accepte aussi la forme antérieure, un rôle unique en chaîne, le temps qu'un jeton déjà émis expire. Personne d'autre ne lit ce claim aujourd'hui — l'API Java relit `tools_core.user_module_role` à chaque use case et le frontend prend ses droits sur `/users/me`.

### Contrat de vérification pour un service tiers

Un service qui n'est ni le Core ni l'API Java — un satellite écrit dans un autre langage, voir
`ARCHITECTURE.md` — **ne réimplémente jamais l'authentification**. Il vérifie, et rien de plus.
Le Core est le seul à émettre.

Ce qu'il doit reproduire tient en deux blocs, et il n'a besoin d'**aucun accès à la base du
Core** — `isActive` est un claim, pas une lecture SQL.

Validation cryptographique (`JwtTokenParameters.Validation`) :

| Règle | Valeur |
|---|---|
| Signature | HMAC, clé = `JWT_SECRET` |
| Algorithme | **choisi selon la taille de la clé** : ≥ 64 octets → HS512, ≥ 48 → HS384, sinon HS256 |
| Issuer | validé, `Auth:Jwt:Issuer` |
| Audience | non validée |
| Expiration | validée, **`ClockSkew` à zéro** |

Règles applicatives (`JwtAuthenticationExtensions.EnforceAccessTokenRules`) :

- **`tokenType == "ACCESS"`** — sans ce contrôle, un refresh token présenté en `Bearer` vaut
  sept jours d'accès.
- **`isActive == "true"`** — un compte désactivé garde un jeton signé valide jusqu'à son
  expiration.

Décision de droits, une fois le jeton accepté :

- rôle global : claim `roles`, le plus permissif l'emporte ;
- rôle dans le module servi par le satellite : claim `modules`, le plus permissif l'emporte ;
- un use case rattaché à un module se juge **sur le rôle du module seul** — le rôle global n'y
  ajoute rien, y compris pour un administrateur du site.

C'est ce claim qui rend un satellite possible : il décide sans jamais lire
`tools_core.user_module_role`, donc sans toucher au schéma du Core — la règle d'appartenance
des schémas est respectée sans exception à négocier.

Le choix d'algorithme selon la taille de la clé est le piège : il imite le comportement de
JJWT côté Java, il est invisible dans un jeton, et un satellite qui coderait HS256 en dur
refuserait tout le jour où le secret passe à 64 octets. **Ces trois points — algorithme,
`ClockSkew`, les deux claims — sont le contrat ; toute divergence est un bug de sécurité ou
une panne silencieuse.**

Le reste du code de `JwtAuthenticationExtensions` (formatage `problem+json`, câblage des
options ASP.NET) n'est pas à porter : chaque service produit ses propres réponses d'erreur,
au format décrit dans `ARCHITECTURE.md`.

**Direction souhaitable : passer à une paire de clés asymétrique.** Le Core signerait avec sa
clé privée et publierait la clé publique ; chaque service la récupérerait **une fois au
démarrage**. Plus aucun secret recopié d'un conteneur à l'autre — la cause de l'incident du
15/08/2026 — et toujours zéro appel réseau par requête. Non fait.

## Tâches de fond

Un use case sécurisé ne doit pas être appelé hors requête HTTP : aucun utilisateur n'est alors identifié et l'exécution est refusée. Un traitement planifié doit appeler le service applicatif sous-jacent, jamais le use case sécurisé.

## Authentification et autorisation : deux étages distincts

En place depuis `JwtAuthenticationExtensions.AddCoreJwtAuthentication()`.

```text
middleware JwtBearer     valide le jeton, remplit HttpContext.User      authentification
HttpCurrentUserProvider  lit HttpContext.User, ne valide plus rien
SecuredUseCase           compare le rôle à RequiredRole                 autorisation
```

**L'authentification** — « ce jeton est-il valide, et qui es-tu ? » — est technique, propre
à HTTP, et se fait une fois par requête. C'est le rôle d'un middleware.

**L'autorisation** — « as-tu le droit de faire *cette* action ? » — reste dans le use case.
Déplacer cette règle sur la route la placerait dans l'adapter le plus externe : elle
n'existerait plus que tant que ce chemin d'accès existe. Or « envoyer un mail arbitraire
exige TECH » est une affirmation sur le système, pas sur une URL — elle doit rester vraie
qu'on entre par HTTP, par un hub SignalR ou par un futur adapter. `requiredModule()` en est
la preuve : un module est un concept du domaine, l'adapter HTTP n'a pas à le connaître.

### Secure by default

L'authentification est exigée par défaut via `FallbackPolicy`, et les routes publiques sont
déclarées explicitement anonymes :

```csharp
options.FallbackPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();
```

C'est l'inverse du réflexe habituel — poser `[Authorize]` sur ce qui doit être protégé. La
raison est la même que pour `SecuredUseCase` : avec l'opt-in, un oubli **ouvre** une route
en silence ; avec la `FallbackPolicy`, un oubli **ferme** et se voit à la première requête.
Toute route créée ensuite est protégée sans qu'on ait rien à écrire.

Routes publiques à déclarer anonymes : `auth/login`, `auth/refresh`, `auth/google/url`,
`auth/callback/google`, `auth/password/reset-request`, `auth/password/reset`, `health/*`,
`version`, et les endpoints `_tests/errors/*` de l'environnement Testing.
`auth/electron/session` n'en fait **pas** partie : elle exige un access token valide.

### Règles à ne pas perdre en déplaçant la validation

Le middleware valide signature, issuer et expiration — pas plus. Deux règles portées
aujourd'hui par `ITokenService.ReadAccessToken` doivent être explicitement reconduites :

- **`tokenType == "ACCESS"`**. Access et refresh tokens partagent secret, issuer et
  algorithme. Sans ce contrôle, un refresh token présenté en `Authorization: Bearer` vaudrait
  un accès de sept jours, hors de toute fenêtre de révocation.
- **`isActive == "true"`**. Un compte désactivé conserve un jeton signé valide jusqu'à son
  expiration.

Deux pièges techniques accompagnent le changement : `JwtBearerOptions.MapInboundClaims`
doit valoir `false` pour lire le `sub` standard, et le claim `roles` — écrit comme tableau
JSON — doit continuer d'être déplié en claims multiples par le handler du middleware, qui
n'est pas forcément celui utilisé par `JwtTokenService`.

Enfin, les 401 et 403 émis par le middleware passent par `ApiProblemDetailsFactory`
(`OnChallenge`, `OnForbidden`) : aucun second format JSON d'erreur n'existe. `OnChallenge`
distingue deux cas que le front traite différemment — pas d'en-tête `Bearer` donne
`UNAUTHENTICATED`, un jeton présenté mais refusé donne `INVALID_ACCESS_TOKEN`.

### Trois pièges rencontrés à la mise en place

**Lire la configuration à l'enregistrement des services fige les valeurs.** Une première
version lisait `configuration["JWT_SECRET"]` directement dans la méthode d'extension. Les
sources de configuration ajoutées ensuite — celles des tests d'intégration — étaient alors
ignorées au profit des variables d'environnement de la machine : les jetons étaient signés
avec un secret et validés avec un autre. La configuration est désormais résolue
paresseusement via `AddOptions<JwtBearerOptions>().Configure<IConfiguration, …>()`, donc à la
construction des options et non à leur déclaration.

**La `FallbackPolicy` s'applique aussi aux requêtes sans endpoint.** Une URL inconnue
répondait donc 401 au lieu de 404. Ce n'est pas cosmétique : le front interprète un 401 comme
une session expirée et déclencherait un refresh sur une faute de frappe. Une garde placée
entre `UseAuthentication` et `UseAuthorization` renvoie 404 quand `GetEndpoint()` est nul.

**Le claim `roles` peut arriver sous deux formes.** Écrit comme tableau JSON, il est déplié
en un claim par valeur par les handlers actuels — mais `HttpCurrentUserProvider` accepte
aussi le claim unique contenant le tableau brut. Faire dépendre les droits d'un détail
d'implémentation de la bibliothèque JWT serait un risque disproportionné au coût de la
double lecture.

### Paramètres cryptographiques

`JwtTokenParameters` centralise clé, algorithme et règles de validation. L'émission
(`JwtTokenService`) et la validation (middleware) s'y réfèrent toutes deux : les dupliquer
garantirait qu'elles divergent un jour, et un jeton signé en HS512 puis validé en HS256 est
refusé sans que rien n'indique pourquoi.

## Ce qui reste à faire

`SecuredUseCase<TCommand, TResult>` impose de déclarer un type d'entrée même quand le use case n'en a aucun — le cas d'une lecture comme `/me`. La forme manquante est une `SecuredQuery<TResult>` : en C#, l'arité générique fait la signature, `SecuredUseCase<TResult>` désignerait donc « une commande sans résultat ». C'est le prix du contrôle porté par héritage plutôt que par un aspect, et il vaut la garantie obtenue — l'oubli du contrôle est inexprimable.
