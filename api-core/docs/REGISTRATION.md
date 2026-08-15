# Inscription

```text
POST /auth/register              { "name": "...", "email": "...", "password": "..." }
POST /auth/verify-email?token=…
```

Les deux routes sont anonymes : un visiteur sans session est le seul appelant possible.

## Déroulé

`register` crée l'utilisateur, ses credentials, son provider `PASSWORD` et son rôle `USER`
dans une seule transaction — un compte sans rôle serait créé mais incapable d'agir. Le compte
naît **inactif**, avec `email_verified_at` à `NULL` : `LoginUseCase` refuse un compte inactif,
la connexion est donc impossible avant confirmation.

Le jeton fait 32 octets aléatoires encodés en Base64 URL sans remplissage, valable 30 minutes,
et **une seule demande est active par utilisateur**. Le lien envoyé est
`{App:FrontendBaseUrl}{Auth:Registration:VerifyPath}?token=…`.

L'email part **après le commit** : un jeton annulé ne peut jamais être envoyé.

`verify-email` active le compte, renseigne `email_verified_at` et consomme le jeton. Un jeton
inconnu, déjà utilisé ou expiré donne la même réponse — `400 INVALID_EMAIL_VERIFICATION_TOKEN`
— rien ne permet de les distinguer de l'extérieur.

À l'arrivée, un compte inscrit par mot de passe est dans le même état qu'un compte créé via
Google : actif, avec une adresse confirmée.

## Deux états, deux colonnes

| Colonne | Question |
|---|---|
| `is_active` | le compte est-il autorisé à se connecter ? |
| `email_verified_at` | l'adresse a-t-elle été confirmée un jour ? |

L'API Java n'avait que `is_active` pour les deux, et son nettoyage supprimait tout compte
inactif sans jeton de vérification. Un compte **suspendu par un administrateur**, dont le
jeton avait expiré depuis longtemps, tombait donc dans le filet : une tâche de fond effaçait
l'utilisateur et ses données.

`email_verified_at` sépare définitivement les deux notions. Le nettoyage ne regarde plus
jamais `is_active` (migration `V2.65.0`).

## Réinscription avant confirmation

Une adresse déjà **confirmée** est refusée : `409 EMAIL_ALREADY_REGISTERED`. Le compte peut
être suspendu, cela ne libère pas l'adresse pour autant.

Une adresse **jamais confirmée** reprend l'inscription en cours : aucun compte en double, un
nouveau jeton, et surtout **le mot de passe fourni remplace le précédent**. L'API Java le
jetait en silence — l'utilisateur qui recommençait avec un autre mot de passe se retrouvait
avec l'ancien après confirmation, sans comprendre pourquoi il ne pouvait pas se connecter.

## Nettoyage

`EmailVerificationCleanupService` s'exécute toutes les 30 minutes et effectue deux opérations,
dans cet ordre :

1. supprimer les comptes dont `email_verified_at IS NULL` et qui n'ont plus de jeton valide ;
2. supprimer les jetons expirés restants.

L'ordre importe : effacer les jetons d'abord rendrait indistinguables les inscriptions
expirées de celles encore en cours. La suppression du compte cascade sur `user_credentials`,
`user_auth_provider`, `user_role` et `user_email_verification`.

C'est un `BackgroundService`, donc un singleton : il crée son propre scope à chaque passage.
Il n'appelle aucun use case sécurisé — hors requête HTTP, aucun utilisateur n'est identifié.
Il n'est pas enregistré en environnement `Testing`.

## Sécurité du use case

`RegisterUserUseCase` et `VerifyEmailUseCase` **n'héritent pas** de `SecuredUseCase`, et c'est
délibéré. L'API Java marque son use case d'inscription comme sécurisé ; cela ne fonctionne que
parce que son aspect laisse passer les appels non identifiés. Chez nous, `UseCaseAuthorizer`
refuse un appelant absent : un tel marquage rendrait l'inscription impossible.

## Configuration

```json
"Auth": {
  "Registration": {
    "TokenBytes": 32,
    "TokenTtlMinutes": 30,
    "VerifyPath": "/auth/verify-email"
  }
}
```

## Limitation de débit

`/auth/register` et `/auth/password/reset-request` sont les deux seules routes anonymes qui
déclenchent un envoi d'email. Elles partagent la politique `email-sending` :
**5 requêtes par adresse IP et par fenêtre de 15 minutes**.

```csharp
[EnableRateLimiting(RateLimitingExtensions.EmailSendingPolicy)]
[HttpPost("register")]
```

Ce qu'on protège ici n'est pas l'accès aux données : un compte non confirmé ne peut pas se
connecter, n'a aucun module, et le nettoyage l'efface. Le risque réel est le **service
d'envoi** — quelques milliers d'appels épuisent le quota SMTP et dégradent la réputation du
domaine, au point que les mails légitimes de réinitialisation finissent en spam.

Le refus renvoie `429 TOO_MANY_REQUESTS` via `ApiProblemDetailsFactory`, avec un en-tête
`Retry-After` quand la fenêtre le permet. `UseRateLimiter()` est placé avant
l'authentification : une requête rejetée ne déclenche aucun travail.

Bloquer le sous-adressage (`utilisateur+test@gmail.com`) a été écarté : un attaquant utilise
des domaines jetables, pas le `+`. La mesure ne gênerait que les utilisateurs légitimes.

En environnement `Testing`, la politique est déclarée mais ne limite rien — une route
référençant une politique absente ferait échouer le démarrage, et les requêtes en mémoire
n'ayant pas d'adresse IP, elles partageraient toutes le même compteur.

## Reste à faire

**Derrière le reverse proxy, la limite s'appliquera au proxy et non au visiteur** : tous les
utilisateurs partageront un seul compteur. Il faut activer `UseForwardedHeaders` pour
qu'ASP.NET lise `X-Forwarded-For`. À traiter au déploiement, en même temps que
`Cors:AllowedOrigins`, aujourd'hui vide dans `appsettings.QA.json` et
`appsettings.Production.json` — toute requête navigateur y serait refusée.
