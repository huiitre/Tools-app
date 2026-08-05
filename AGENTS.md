# AGENTS.md - Monorepo Guide

## Build & Run
- **API (Standard)** : `cd api && ./mvnw spring-boot:run -Dspring-boot.run.profiles=dev`
- **API (Alias)** : `api:dev` (alias vers `SPRING_PROFILES_ACTIVE=dev mvn -f api/pom.xml spring-boot:run`)
- **WEB** : `cd web && npm install && npm run dev`
- **ELECTRON** : `cd web && npm run electron:watch`
- **GLOBAL** : `./dev.sh` (API + Web) ou `./dev.sh --electron`

## Architecture Rules
- **Règle transversale** : respecter les patterns, composants et conventions déjà présents dans le monorepo. Ne pas introduire de variante locale ou de comportement maison lorsqu'un équivalent existe. Le code doit rester cohérent, maintenable, évolutif et organisé par responsabilité ; demander une clarification avant de choisir une architecture incertaine.
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
