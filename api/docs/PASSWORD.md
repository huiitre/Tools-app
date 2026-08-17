# Mot de passe

Trois flux, portés par le module `Auth`. Le schéma est celui déjà en place (`V2.2.0`, `V2.14.0`) : aucune migration n'a été nécessaire.

## Réinitialisation par email

```text
POST /auth/password/reset-request   { "email": "..." }
POST /auth/password/reset           { "token": "...", "password": "..." }
```

`reset-request` répond **toujours** la même chose, que le compte existe ou non, qu'il ait un mot de passe ou non :

```json
{ "status": "RESET_REQUESTED", "message": "Si un compte correspondant existe, un email a été envoyé." }
```

Seul un compte disposant du provider `PASSWORD` reçoit un email. Un compte Google qui n'a jamais défini de mot de passe n'a rien à réinitialiser : il doit d'abord en créer un depuis ses options.

Le jeton fait 32 octets aléatoires encodés en Base64 URL sans remplissage, valable 30 minutes, et **une seule demande est active par utilisateur** — une nouvelle demande remplace la précédente. Le lien envoyé est `{App:FrontendBaseUrl}/auth/reset-password?token=…`.

`reset` renvoie `204`, ou `400 INVALID_PASSWORD_RESET_TOKEN` si le jeton est inconnu ou expiré. La lecture du jeton, l'écriture du mot de passe et la suppression du jeton sont dans une seule transaction : un échec ne consomme pas le jeton.

## Définir ou changer son mot de passe

```text
PATCH /auth/password   { "password": "..." }
```

Réservé à l'utilisateur authentifié, rôle minimum `READ_ONLY` — donc accessible à tous. L'identifiant vient du jeton, jamais de la requête : personne ne peut viser le compte d'un autre.

Si l'utilisateur a déjà un mot de passe, il est remplacé. **S'il n'en a pas** — le cas d'un compte Google — la ligne `user_credentials` et le provider `PASSWORD` sont créés ensemble, avec `provider_user_id = email` (même convention qu'à l'inscription). À partir de là, ce compte peut se connecter par mot de passe et utiliser « mot de passe oublié ».

## Nettoyage

`PasswordResetCleanupService` supprime les jetons expirés toutes les 30 minutes. C'est un `BackgroundService`, donc un singleton : il crée son propre scope à chaque passage pour résoudre les services scoped. Il n'appelle aucun use case sécurisé — hors requête HTTP, aucun utilisateur n'est identifié. Il n'est pas enregistré en environnement `Testing`.

## Configuration

```text
App__FrontendBaseUrl=https://qa.tools.huiitre.fr
Auth__PasswordReset__TokenBytes=32
Auth__PasswordReset__TokenTtlMinutes=30
Auth__PasswordReset__ResetPath=/auth/reset-password
```

## Écarts assumés avec l'API Java

- **L'email part après le commit**, pas dedans. Côté Java, `SendPasswordResetUseCase` est `@Transactional` : un rollback après l'envoi laisserait partir un lien portant un jeton inexistant.
- **`reset` écrit le mot de passe même sans ligne de credentials préexistante.** Le Java fait un `updatePassword` seul : si la ligne manquait, l'`UPDATE` ne touchait rien et le use case répondait quand même succès.
- Les dates sont en **UTC** (`DateTime.UtcNow`) là où le Java utilise `LocalDateTime.now()`. Sans incidence tant que les jetons ne sont pas partagés entre les deux API, les conteneurs tournant en UTC.

## Ce qui n'a pas été repris

Comme côté Java, il n'y a **aucune règle de complexité côté serveur** (seul le vide est refusé) : les règles — 8 caractères, une lettre, un chiffre — ne vivent que dans le front (`web/src/modules/Auth/views/passwordValidation.ts`) et sont donc contournables par un appel direct.

De même, le changement de mot de passe **ne demande pas le mot de passe actuel**. Un access token volé permet donc de s'approprier le compte.

Ces deux points sont des choix repris à l'identique de l'existant, pas des oublis.

## Front

Le front appelle encore l'API Java (`clientV3`) sur `/auth/password/reset-request`, `/auth/password/reset` et `/user/password`. Le Core expose les deux premières à l'identique ; la troisième devient `PATCH /auth/password`, le mot de passe relevant de l'identification et non du profil (voir le contrat de routes dans ARCHITECTURE.md).
