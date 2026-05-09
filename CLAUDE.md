# CLAUDE.md - Monorepo Guide

## Build & Run
- **API** : `cd api && ./mvnw spring-boot:run`
- **WEB** : `cd web && npm install && npm run dev`
- **ELECTRON** : `cd web && npm run electron:watch`

## Architecture Rules
- **API (Java)** :
    - JDBC pur (`JdbcTemplate`). JAMAIS de Hibernate/JPA.
    - Chaque Use Case est une classe @Service unique.
    - Invariants métier dans le Domain (0 dépendance).
- **WEB (Vue 3)** :
    - Composition API.
    - PicoCSS pour tout le styling (respecter `/web/doc/pico-css-variables-guide.md`).
    - Electron pour les modules Sniffer/Autofocus.

## Common Tasks
- **Migration BDD** : Ajouter un script dans `database/sql/V2.x.y__nom.sql` et l'exécuter manuellement sur Postgres.
- **Nouvelle Feature** : Créer le Use Case dans l'API, le repository (Port/Adapter), puis le service/composant dans le Web.
- **Update** : Les releases sont gérées via `huiitre/Tools-app` sur GitHub.
