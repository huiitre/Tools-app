# AGENTS.md - Monorepo Guide

## Les deux APIs

Depuis le renommage du 17/08/2026 :

| dossier | ce que c'est |
|---|---|
| `api/` | **l'API** — ASP.NET Core (C#). Plateforme transverse (auth, administration, notifications, realtime) et, au fur et à mesure, les modules métier. C'est ici qu'on écrit tout ce qui est nouveau. |
| `api-java/` | l'API Spring Boot d'origine, figée et vidée module par module. Elle sert encore Dofus, Palworld et Feedback. Elite Dangerous en a été retiré le 24/08/2026 et Riot le 29/08/2026 ; leurs modules vivent désormais dans `api/`, où Temtem est né directement le 30/08/2026 sans jamais passer par elle. |

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
- **BASE LOCALE** : `npm run db:up` lance un Postgres 16 dans Docker (`docker-compose.dev.yml`, port 5433). `npm run db:clone -- prod` (ou `qa`) récupère un dump complet depuis le NAS et le rejoue en local. Le dump est gardé dans `.local/db/` pour être rejoué sans re-solliciter le NAS. Sur le NAS, `db-dump.sh` (écrit le dump sur stdout, ne modifie rien) et `db-restore.sh` (lit stdin, applique) vivent dans `/data/docker/tools/scripts/` : `db-dump.sh prod | db-restore.sh qa` clone la prod vers la qa sans transfert réseau. **La prod n'est jamais une cible de restauration**, et toute restauration passe par une base temporaire supprimée quoi qu'il arrive.
- **SYNC** : `npm run sync` — miroir local des données du NAS par rsync (`scripts/`) : assets (`npm run sync:assets`) et snapshots serveur Palworld (`npm run sync:snapshots`). Copie exacte de la source (`--delete`), les fichiers inchangés ne sont pas retéléchargés. Accès SSH lu dans `.env`, chemins surchargeables (voir `.env.example`).
- **GLOBAL** : `npm run dev` (API + API Java + Web) ou `npm run dev:qa` (Web seul, pointé sur les API QA distantes). Lance `dev-console/server.js` : démarre le Postgres local (`docker-compose.dev.yml`), ouvre un tunnel SSH unique vers le NAS, puis les process applicatifs, et sert leurs logs en temps réel sur `http://localhost:4488` (filtre par process, clear, copier, redémarrage individuel — celui du panneau `db` redémarre le conteneur). Tout est arrêté au Ctrl+C, conteneur et tunnel compris ; le volume est conservé.
- **TUNNEL SSH** : les serveurs de jeu vivent dans la netns du conteneur `wireguard-games` sur le NAS et leurs ports d'administration ne sont pas publiés — rien n'est joignable depuis le poste sans tunnel. `dev-console` ouvre **une seule** connexion SSH portant toutes les redirections (constante `TUNNELS` en tête de `dev-console/server.js`, une ligne par serveur, port local = port distant : seul le host change, `127.0.0.1` en dev). `ssh -L` ne transporte que du TCP : les serveurs interrogés en A2S (UDP) ne sont pas tunnelables.

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

## Diagrammes
- Les diagrammes sont **régénérés à chaque `dotnet build`** (cible MSBuild `GenerateDiagrams`,
  incrémentale et sans effet sur le build en cas d'échec). À la main : `npm run api:diagrams`.
  Un dossier par sous-module : un diagramme Mermaid des modules et de
  leurs dépendances, puis un par sous-module avec ses types et les flèches de dépendance lues dans
  les constructeurs. Deux formats par diagramme : `.drawio`
  (éditable dans VS Code) et `.md` (bloc mermaid, rendu par GitHub). Voir
  `api/docs/diagrams/README.md`.

## Common Tasks
- **Route API (règle immuable)** : toute route **ajoutée, modifiée ou supprimée**, quelle que soit l'API (C# `api/`, Java `api-java/`, Node), doit être répercutée dans la collection Bruno (`bruno/`) dans le même commit. C'est le seul moyen de tester les routes à la main : une route livrée sans son entrée Bruno n'est pas testable, donc pas terminée.
- **Migration BDD** : Ajouter un script dans `database/sql/V2.x.y__nom.sql` et l'exécuter manuellement sur Postgres.
- **Nouvelle Feature** : Créer le Use Case dans l'API, le repository (Port/Adapter), puis le service/composant dans le Web.
- **Update** : Les releases sont gérées via `huiitre/Tools-app` sur GitHub.
