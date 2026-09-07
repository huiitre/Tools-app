# Inscription

```text
POST /auth/register              { "name": "...", "email": "...", "password": "..." }
POST /auth/verify-email?token=…
```

Les deux routes sont anonymes : un visiteur sans session est le seul appelant possible.

## Deux paramètres gouvernent l'entrée

Déclarés dans `SettingCatalog.Auth`, portée `Global` uniquement, visibles des seuls
administrateurs. Leurs défauts reproduisent le comportement d'avant leur existence : déployer
ces définitions ne change rien tant que personne n'a rien posé.

| paramètre | défaut | effet quand il est posé |
|---|---|---|
| `auth.registrationEnabled` | `true` | `false` ferme la création de compte |
| `auth.adminApprovalRequired` | `false` | `true` laisse le compte inactif après confirmation |

**Les deux portes, pas une.** `registrationEnabled` est lu par `RegisterUserUseCase` *et* par
`GoogleIdentityAuthenticationService`, où le premier login Google crée le compte à la volée. Ne
fermer que `/auth/register` laisserait la fenêtre Google grande ouverte. Le refus est
`403 REGISTRATION_CLOSED`, et côté Google il est placé **après** la recherche du compte existant :
fermer les inscriptions ne met dehors aucun habitué.

Les deux se lisent avec `GetGlobal` et jamais `Get` : l'appelant est un visiteur anonyme, et
`Get` lèverait faute d'utilisateur identifié.

**L'approbation n'a pas d'état propre.** `auth.adminApprovalRequired` ne fait que laisser
`is_active` à `false` — activer le compte depuis le tableau d'administration
(`PUT /users/{id}/active`) *est* l'approbation. Une colonne `approved_at` a été écrite puis
abandonnée le 08/09/2026 : la route d'activation existait déjà, et elle rendait la colonne
inutile.

Ce qu'on perd, et qui est assumé : le login d'un compte en attente répond `USER_DISABLED`, comme
un compte suspendu. Rien ne les distingue de l'extérieur, et l'administrateur les distingue à la
date d'inscription — le tableau trie par `created_at DESC`.

Les administrateurs sont prévenus dans les deux cas, avec un message différent : une
notification qui annoncerait « le compte est actif » alors qu'il attend serait fausse, et
personne ne saurait qu'il y a une file à traiter.

## Déroulé

`register` crée l'utilisateur, ses credentials, son provider `PASSWORD` et son rôle `USER`
dans une seule transaction — un compte sans rôle serait créé mais incapable d'agir. Le compte
naît **inactif**, avec `email_verified_at` à `NULL` : `LoginUseCase` refuse un compte inactif,
la connexion est donc impossible avant confirmation.

Le jeton fait 32 octets aléatoires encodés en Base64 URL sans remplissage, valable 30 minutes,
et **une seule demande est active par utilisateur**. Le lien envoyé est
`{App:FrontendBaseUrl}{Auth:Registration:VerifyPath}?token=…`.

L'email part **après le commit** : un jeton annulé ne peut jamais être envoyé.

`verify-email` renseigne `email_verified_at`, consomme le jeton, et active le compte **sauf si**
`auth.adminApprovalRequired` est posé. La route répond `200` avec un `status` — `ACTIVE` ou
`PENDING_APPROVAL` — et non plus `204` : sans lui le frontend ne pouvait pas distinguer les deux
cas, et invitait à se connecter quelqu'un dont le compte attendait encore. L'écran de
confirmation purge au passage la session éventuellement présente dans le navigateur, le lien
concernant le compte qui vient de s'inscrire et non celui qui était connecté. Un jeton inconnu, déjà utilisé ou expiré donne la même
réponse — `400 INVALID_EMAIL_VERIFICATION_TOKEN` — rien ne permet de les distinguer de
l'extérieur.

À l'arrivée, un compte inscrit par mot de passe est dans le même état qu'un compte créé via
Google : adresse confirmée, et actif si aucune validation d'administrateur n'est exigée. Les
deux chemins lisent le même paramètre, ils ne peuvent pas diverger.

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

La suspension elle-même se fait par `PUT /users/{id}/active` (rôle ADMIN). Elle ne touche pas
`email_verified_at` : l'adresse reste confirmée, seule la connexion est fermée. Un
administrateur ne peut pas suspendre son propre compte — `409 CANNOT_DEACTIVATE_SELF` —, sans
quoi il lui faudrait un autre administrateur ou un `UPDATE` à la main pour revenir en arrière.

La suspension ne coupe pas la session déjà ouverte : le claim `isActive` est figé à l'émission
de l'access token, qui reste donc accepté jusqu'à son expiration (10 minutes). Le
renouvellement, lui, relit `is_active` et refuse (`RefreshSessionUseCase`) — la suspension prend
donc effet au plus tard au premier refresh.

Un event SignalR `Core.UserActiveChanged`, de charge `{ "active": bool }`, est poussé vers
l'utilisateur visé, dans les deux sens. Le frontend l'écoute dans `armRealtimeSync` et déconnecte
sur `active: false` (voir `web/AGENTS.md`). Il écourte cette attente pour un client connecté au hub,
**sans rien garantir** : un client qui l'ignore garde son jeton jusqu'au bout. Ce n'est donc pas
un contrôle de sécurité, et il ne remplace rien côté serveur. Fermer la fenêtre pour de bon
demanderait une liste des comptes suspendus consultée dans `EnforceAccessTokenRules` — envisagé,
pas fait : dix minutes de sursis sur un compte qu'on vient de suspendre sont sans conséquence.

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

## Limitation de débit — retirée

Une politique `email-sending` (5 requêtes par IP et par fenêtre de 15 minutes) a été mise en
place sur `/auth/register` et `/auth/password/reset-request`, puis **retirée**.

La raison est le déploiement : derrière le reverse proxy, ASP.NET ne voit que l'adresse IP du
proxy tant que `UseForwardedHeaders` n'est pas activé. Toutes les requêtes tombent alors dans
la même partition — la limite ne protège plus de rien et devient une gêne, cinq inscriptions
par quart d'heure pour l'ensemble des visiteurs.

Entre une protection inopérante et pas de protection, la seconde est plus honnête : elle ne
donne pas l'illusion d'une sécurité qui n'existe pas.

Pour la réintroduire un jour, il faut les deux ensemble : `UseForwardedHeaders` **et** la
politique. L'un sans l'autre ne vaut rien.

## Reste à faire

`Cors:AllowedOrigins` est renseigné en QA et en Production. Reste, si la limitation de débit
revient un jour, à activer `UseForwardedHeaders` pour qu'ASP.NET lise `X-Forwarded-For`.

Exposition assumée en attendant : `/auth/register` et `/auth/password/reset-request` sont
anonymes et déclenchent chacune un envoi d'email. Un appel en boucle épuiserait le quota SMTP
OVH et dégraderait la réputation du domaine — au point que les mails de réinitialisation
partiraient en spam. Les comptes créés, eux, ne donnent aucun accès : inactifs tant que
l'adresse n'est pas confirmée, et effacés par le nettoyage.
