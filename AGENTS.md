# AGENTS.md - Monorepo Guide

## Les deux APIs

Depuis le renommage du 17/08/2026 :

| dossier | ce que c'est |
|---|---|
| `api/` | **l'API** — ASP.NET Core (C#). Plateforme transverse (auth, administration, notifications, realtime) et, au fur et à mesure, les modules métier. C'est ici qu'on écrit tout ce qui est nouveau. |
| `api-java/` | l'API Spring Boot d'origine, figée et vidée module par module. Elle sert encore Dofus, Palworld, Riot et Feedback. |

Le nommage est le même partout — dossier, image Docker et conteneur — pour qu'un nom lu quelque
part désigne toujours la même chose :

| | dossier | image | conteneur prod | conteneur QA |
|---|---|---|---|---|
| C# | `api/` | `huiitre/tools_api` | `tools_api` | `tools_api_qa` |
| Java | `api-java/` | `huiitre/tools_api_java` | `tools_api_java` | `tools_api_java_qa` |

L'API Java joint l'API C# par `TOOLS_API_HOST` (route interne `/internal/notifications`).

Aucune URL publique n'a changé : `api-java/` garde son `context-path=/api/v3`, `api/` reste servie
sous `/api/core`. La cible et le mécanisme de bascule sont décrits dans `api/docs/ARCHITECTURE.md`
(section « Cible : une seule API C# et des satellites polyglottes »).

## Build & Run
- **API (C#)** : `cd api && dotnet watch run` — alias `npm run api:dev`, tests `npm run api:test`
- **API Java** : `cd api-java && ./mvnw spring-boot:run -Dspring-boot.run.profiles=dev` — alias `npm run java:dev`
- **WEB** : `cd web && npm install && npm run dev`
- **ELECTRON** : `cd web && npm run electron:watch`
- **GLOBAL** : `npm run dev` (API + API Java + Web) ou `npm run dev:qa` (Web seul, pointé sur les API QA distantes). Lance `dev-console/server.js` : logs des 3 process en temps réel sur `http://localhost:4488` (filtre par process, clear, copier), résout automatiquement l'accès Postgres (direct sur le LAN maison, sinon tunnel SSH via `.env`, voir `.env.example`).

## Architecture Rules
- **Règle transversale** : respecter les patterns, composants et conventions déjà présents dans le monorepo. Ne pas introduire de variante locale ou de comportement maison lorsqu'un équivalent existe. Le code doit rester cohérent, maintenable, évolutif et organisé par responsabilité ; demander une clarification avant de choisir une architecture incertaine.
- **API (C#, `api/`)** :
    - Dapper sur SQL écrit à la main. Pas d'ORM.
    - Un use case = une classe ; s'il exige des droits, il hérite de `SecuredUseCase` (voir `api/docs/SECURITY.md`).
    - Les contrôleurs résolvent leurs use cases **par action** (`[FromServices]`), jamais dans leur constructeur.
    - Composition explicite par module (`<Module>Module.cs`), aucune découverte par réflexion.
- **API Java (`api-java/`)** :
    - JDBC pur (`JdbcTemplate`). JAMAIS de Hibernate/JPA.
    - Chaque Use Case est une classe @Service unique.
    - Invariants métier dans le Domain (0 dépendance).
- **WEB (Vue 3)** :
    - Composition API.
    - PicoCSS pour tout le styling (respecter `/web/doc/pico-css-variables-guide.md`).
    - Electron pour les modules Sniffer/Autofocus.

## Common Tasks
- **Route API (règle immuable)** : toute route **ajoutée, modifiée ou supprimée**, quelle que soit l'API (C# `api/`, Java `api-java/`, Node), doit être répercutée dans la collection Bruno (`bruno/`) dans le même commit. C'est le seul moyen de tester les routes à la main : une route livrée sans son entrée Bruno n'est pas testable, donc pas terminée.
- **Migration BDD** : Ajouter un script dans `database/sql/V2.x.y__nom.sql` et l'exécuter manuellement sur Postgres.
- **Nouvelle Feature** : Créer le Use Case dans l'API, le repository (Port/Adapter), puis le service/composant dans le Web.
- **Update** : Les releases sont gérées via `huiitre/Tools-app` sur GitHub.
