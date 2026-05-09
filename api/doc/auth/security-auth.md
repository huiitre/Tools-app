# Sécurité & Authentification — Tools API v3

## Objectif
Mettre en place une authentification **moderne, stateless et sécurisée**, indépendante du métier, conforme aux bonnes pratiques API (JWT + refresh token).

Cette authentification est **figée** et ne doit plus être modifiée sans raison explicite.

---

## Principes clés

- API **stateless**
- Aucun état serveur (pas de session, pas de stockage token en base)
- Authentification par **JWT**
- Renouvellement via **refresh token**
- Séparation stricte :
  - Auth = infrastructure / application
  - Métier = domain / application

---

## Types de tokens

### Access Token (JWT)
- Durée : **15 minutes**
- Transport : header HTTP
  ```
  Authorization: Bearer <accessToken>
  ```
- Contenu :
  - `iss` : tools-api
  - `sub` : identifiant logique
  - `exp` : expiration
  - claims métier légers (ex: role)

➡️ Utilisé sur **toutes les routes métier**.

---

### Refresh Token
- Durée : **7 jours**
- Stockage : **cookie HttpOnly**
- Jamais accessible en JavaScript
- Jamais stocké en base
- Transport automatique par le navigateur

Cookie :
- name : `refresh_token`
- HttpOnly : true
- SameSite : Strict
- Secure :
  - DEV : false
  - QA / PROD : true
- Path : `/api/v3/auth`

---

## Endpoints d’authentification

### POST `/auth/login`
- Génère :
  - un access token (JSON)
  - un refresh token (cookie)
- Aucune logique métier (login technique temporaire)

Réponse :
```json
{
  "accessToken": "jwt"
}
```

---

### POST `/auth/refresh`
- Utilise le cookie `refresh_token`
- Vérifie :
  - signature
  - expiration
  - issuer
- Génère :
  - un **nouvel access token**
  - un **nouveau refresh token** (rotation)

➡️ L’ancien refresh token devient invalide.

---

## Workflow utilisateur

1. Login
2. Appels métier avec access token
3. Access token expiré → 401
4. Refresh automatique → nouveau access token
5. Rejouer la requête métier

⚠️ **Le refresh n’est jamais appelé à chaque requête**.

---

## Expiration & reconnexion

- Access token expiré :
  - refresh automatique possible
- Refresh token expiré :
  - **reconnexion obligatoire**
  - aucun moyen de régénérer un token

➡️ Comportement voulu.

---

## Sécurité

- CSRF désactivé (API stateless)
- Cookies sécurisés par environnement
- Headers HTTP de sécurité activés :
  - X-Content-Type-Options
  - X-Frame-Options
  - Content-Security-Policy
  - Referrer-Policy

---

## Ce qui est volontairement NON géré

- Révocation manuelle de token
- Blacklist de refresh token
- Auth utilisateur métier (DB)
- OAuth / SSO

Ces points pourront être ajoutés plus tard **sans casser ce contrat**.

---

## Règle d’or

> Le métier ne connaît **jamais** :
> - les JWT
> - les cookies
> - le mécanisme d’authentification

Il lit uniquement le `SecurityContext`.

---

## État final

- Authentification **finalisée**
- Sécurité **prête production**
- Contrat **figé**
- Aucun couplage avec le métier
