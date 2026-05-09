# Authentification – Tools API v3
> Document de compréhension – état actuel

Ce document explique :
- les **3 tables d’authentification**
- le **cheminement complet d’une requête `/auth/register`**
- le **rôle précis de chaque fichier**
- pourquoi certains contrôles existent
- où sont les responsabilités

Objectif : **comprendre le système existant**, pas en ajouter.

---

## 1. Les tables d’authentification

### 1.1 `tools_core.users`

Table centrale représentant un utilisateur.

Rôle :
- identité de base
- existence de l’utilisateur dans le système

Colonnes principales :
- `id` : identifiant interne
- `name` : nom / pseudo (NOT NULL)
- `email` : email principal (UNIQUE)
- `user_type` : HUMAN / APPLICATION / SYSTEM
- `created_at`

Points importants :
- **La contrainte UNIQUE sur `email` est la vérité absolue**
- Si cette contrainte est violée → l’inscription doit échouer

---

### 1.2 `tools_core.user_credentials`

Table dédiée aux identifiants **email / mot de passe**.

Rôle :
- stocker le **hash du mot de passe**
- éviter de polluer la table `users` avec des données sensibles

Colonnes :
- `user_id` : FK vers `users.id`
- `password_hash` : hash bcrypt (ou autre)

Remarques :
- Un utilisateur peut exister **sans** credentials (ex : Google plus tard)
- Cette table est spécifique au provider PASSWORD

---

### 1.3 `tools_core.user_auth_provider`

Table de liaison entre un utilisateur et un **provider d’authentification**.

Rôle :
- permettre plusieurs méthodes d’auth :
  - PASSWORD
  - GOOGLE
  - GITHUB (plus tard)

Colonnes :
- `user_id`
- `provider`
- `provider_user_id`
- `provider_email`

Important :
- Cette table **n’est pas la source de vérité pour l’email**
- Elle sert à lier un user à un provider

---

## 2. Vue d’ensemble des fichiers impliqués

### Controller
`AuthController`
- point d’entrée HTTP
- validation structurelle
- appel du use case

### Use case
`RegisterUserUseCase`
- logique métier d’inscription
- orchestration
- transaction

### Repositories
- `PostgresUserRepository`
- `PostgresUserCredentialsRepository`
- `PostgresUserAuthProviderRepository`

### Sécurité
- `SecurityConfig`
- `JwtAuthenticationFilter` (non utilisé pour /register)

### Gestion des erreurs
- `ApiExceptionHandler`

---

## 3. Cheminement de `/auth/register`

1. Requête HTTP POST `/api/v3/auth/register`
2. Passage Security (permitAll)
3. Validation controller
4. Exécution du use case
5. Insert `users`
6. Insert `user_credentials`
7. Insert `user_auth_provider`
8. Retour HTTP

---

## 4. Points clés

- La DB est la vérité finale
- Les contraintes SQL sont obligatoires
- Les erreurs SQL doivent être traduites
- Le controller ne fait pas de métier
- Le use case ne fait pas de HTTP

---

Fin du document.
