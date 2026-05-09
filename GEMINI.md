# Tools - Monorepo Context

## Structure du Projet
- `/api` : Backend Java 21 / Spring Boot 3.5. DDD Strict, JDBC pur (pas d'ORM).
- `/web` : Frontend Vue.js 3 / Vite / Electron. PicoCSS pour le style.
- `/database` : Scripts SQL (Flyway-style, mais gestion manuelle).

## Commandes Globales
- `npm run dev` (dans `/web`) : Lance le front en mode PWA.
- `npm run electron:watch` (dans `/web`) : Lance le front en mode Electron.
- `./mvnw spring-boot:run` (dans `/api`) : Lance le backend.
- `./mvnw test` (dans `/api`) : Lance les tests backend.

## Conventions de Navigation
- Toujours vérifier le `GEMINI.md` local du sous-dossier avant d'intervenir.
- Les fichiers `/doc` à la racine de chaque module contiennent les specs détaillées.

## Rappels Stratégiques
- **DDD** : Séparation stricte (Domain, Application, Infrastructure, API).
- **Clean Code** : Pas de commentaires inutiles, records Java, records/DTOs simples.
- **Sécurité** : Spring Security + AOP (`SecuredUseCase`). Hiérarchie : READ_ONLY < USER < MODERATOR < TECH < ADMIN < OWNER.
- **UI** : Variables PicoCSS uniquement. Pas de Tailwind.
- **Monorepo** : Les releases Electron pointent vers le repo `huiitre/Tools-app`.
