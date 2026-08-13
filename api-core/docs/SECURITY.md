# Sécurité

Les droits sont contrôlés **au niveau des use cases**, jamais au niveau des routes. C'est la raison pour laquelle un endpoint n'appelle qu'un use case : c'est le seul point de passage, donc le seul endroit où placer la règle.

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

## Hiérarchie des rôles

```text
READ_ONLY (1) < USER (2) < MODERATOR (3) < TECH (4) < ADMIN (5) < OWNER (6)
```

Identique à l'API Java. Le niveau est porté par la valeur de l'énumération `RoleCode` : il n'y a pas de table de niveaux à maintenir à côté. Un rôle satisfait l'exigence dès qu'il est **supérieur ou égal** au rôle demandé.

## Déroulé du contrôle

1. `ICurrentUserProvider` fournit l'appelant : identifiant et rôles. L'implémentation HTTP lit l'en-tête `Authorization: Bearer` et délègue la validation du token à `ITokenService`.
2. Sans utilisateur identifié : `401 UNAUTHENTICATED`. Avec un token invalide ou expiré : `401 INVALID_ACCESS_TOKEN`.
3. Les rôles proviennent du claim `roles` de l'access token, gravé à l'émission par `AuthSessionService`. **Aucune requête n'est faite lors de l'autorisation.**
4. Rôle insuffisant ou inexistant : `403 INSUFFICIENT_ROLE`. La réponse ne révèle pas le rôle attendu ; la tentative est journalisée.

Un utilisateur peut cumuler plusieurs rôles : c'est **le plus permissif** qui détermine son niveau effectif. Un code de rôle inconnu de l'énumération est ignoré plutôt que d'accorder un droit.

## Fenêtre de révocation — choix assumé

Les droits sont lus dans le token, pas en base. Un rôle retiré ne prend donc effet qu'au **renouvellement de l'access token** (`AccessTokenTtlSeconds`, 10 minutes par défaut) — pas immédiatement. C'est le compromis classique du JWT : aucune requête par appel, au prix d'une fenêtre bornée.

Le refresh, lui, relit la base (`RefreshSessionUseCase`) et refuse de renouveler un compte désactivé : la fenêtre ne peut jamais dépasser la durée de vie de l'access token en cours.

Si une révocation immédiate devient nécessaire, la réponse n'est pas de relire la base à chaque appel mais d'ajouter une **denylist** partagée (Redis, par exemple) : petite, écrite rarement, lue très vite. Un cache de rôles avec TTL ne supprimerait pas la fenêtre, il la déplacerait.

Ce point diverge de l'API Java, qui relit les rôles en base à chaque use case. Divergence connue et acceptée ; le Java pourra s'aligner plus tard.

## Différences assumées avec l'API Java

- L'aspect Java laisse passer un appel sans utilisateur identifié, car Spring Security bloque déjà en amont au niveau des routes. Le Core n'a aucune protection équivalente : l'absence d'utilisateur y est donc un **refus**.
- Java relit les rôles en base à chaque use case ; le Core les lit dans le token (voir la fenêtre de révocation ci-dessus).
- `PostgresUserRoleProvider` côté Java prend le premier rôle trouvé (`LIMIT 1` sans tri), ce qui est non déterministe pour un utilisateur qui en cumule plusieurs. Le Core retient le plus élevé.
- Le contrôle Java repose sur un aspect AOP qui intercepte `execute(..)` ; le Core le porte par héritage, sans proxy dynamique ni dépendance supplémentaire.

## Compatibilité des tokens entre les deux API

Les deux API signent avec le même `JWT_SECRET`, le même issuer et le même choix d'algorithme HMAC, et écrivent les mêmes claims : `roles` en tableau JSON, `modules` en objet. La bascule vers la lecture des rôles dans le token n'a touché que la **lecture** côté Core : un token émis par l'une reste lisible par l'autre.

## Tâches de fond

Un use case sécurisé ne doit pas être appelé hors requête HTTP : aucun utilisateur n'est alors identifié et l'exécution est refusée. Un traitement planifié doit appeler le service applicatif sous-jacent, jamais le use case sécurisé.

## Ce qui reste à faire

Les droits par module (`tools_core.user_module_role`) ne sont pas encore portés : seul le rôle global est contrôlé. `SecuredUseCase` devra recevoir un module optionnel, comme `requiredModule()` côté Java.
