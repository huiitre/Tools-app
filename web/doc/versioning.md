# Versioning automatique

Les releases sont générées automatiquement à chaque push sur `master` via `semantic-release`.

## Convention de commits

| Préfixe | Exemple | Impact |
|---------|---------|--------|
| `fix:` | `fix: correction du calcul de prix` | Patch `1.0.0` → `1.0.1` |
| `feat:` | `feat: ajout du module almanax` | Minor `1.0.0` → `1.1.0` |
| `feat!:` | `feat!: refonte de l'authentification` | Major `1.0.0` → `2.0.0` |
| *(rien)* | `mise à jour du readme` | Aucune release créée |

## Comportement

- Plusieurs commits regroupés → un seul bump (niveau le plus élevé gagne)
- Commit sans préfixe reconnu → pas de release, pas de tag, pas de build Electron
- Le build Docker web se fait à chaque push sur `master`, indépendamment

## Workflow de déploiement

### QA (`deploy-qa.yml`)
Déclenché sur push `qa` :
- Build Docker → image `huiitre/tools_web:qa` avec mode `qa`
- Build Electron Linux (AppImage) + Windows (exe) → uploadés sur la release GitHub `qa-latest`

### Production (`deploy.yml`)
Déclenché sur push `master` :
1. `semantic-release` → analyse les commits, incrémente la version, crée le tag et la release GitHub
2. Build Docker → image `huiitre/tools_web:latest` (utilise le `package.json` mis à jour)
3. Build Electron Linux + Windows → uploadés sur la release GitHub versionnée (ex: `v1.0.1`)

Les jobs Docker et Electron tournent en parallèle après semantic-release.

## Préfixes non versionnants (utiles pour commits sans impact)

- `chore:` — maintenance, dépendances
- `docs:` — documentation
- `style:` — formatage
- `refactor:` — refacto sans changement fonctionnel
- `ci:` — modifications CI/CD

## Affichage de la version

La version est injectée au build via `__APP_VERSION__` dans `vite.config.ts` depuis `package.json`.
Accessible dans n'importe quel composant Vue :

```ts
const version = __APP_VERSION__
```

Affichée dans le footer : `v{{ version }}`

## Badge d'environnement

Visible dans le header en mode `development` et `qa` uniquement.
Basé sur `import.meta.env.MODE` injecté par Vite au build.