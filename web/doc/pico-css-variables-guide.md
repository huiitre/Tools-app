# Pico CSS – Variables utiles & cas d’usage

Ce document liste **les variables Pico réellement exploitables côté UI custom**, sans valeurs, avec **leur intention fonctionnelle**.
Il ne décrit pas l’implémentation (light/dark), uniquement **l’API de design**.

---

## 1. Couleurs globales (layout & texte)

| Variable | Cas d’usage |
|--------|-------------|
| `--pico-background-color` | Fond principal de l’application |
| `--pico-color` | Texte principal |
| `--pico-muted-color` | Texte secondaire, descriptions |
| `--pico-muted-border-color` | Bordures neutres, séparateurs |
| `--pico-text-selection-color` | Sélection de texte |
| `--pico-contrast` | Texte très contrasté |
| `--pico-contrast-inverse` | Texte sur fond contrasté |

---

## 2. Couleur primaire (thème actif)

> À utiliser comme **accent**, jamais partout.

| Variable | Cas d’usage |
|--------|-------------|
| `--pico-primary` | Accent principal |
| `--pico-primary-background` | Fond bouton primaire |
| `--pico-primary-border` | Bordure bouton / élément actif |
| `--pico-primary-hover` | Hover accent |
| `--pico-primary-focus` | Focus clavier |
| `--pico-primary-inverse` | Texte sur fond primaire |

---

## 3. États métier (succès / erreur)

> **Idéal pour vert / rouge** (modules actifs / inactifs, validations, erreurs)

### Succès / Valide (VERT)

| Variable | Cas d’usage |
|--------|-------------|
| `--pico-form-element-valid-border-color` | Indicateur succès |
| `--pico-form-element-valid-active-border-color` | Succès actif |
| `--pico-form-element-valid-focus-color` | Focus succès |
| `--pico-ins-color` | Texte / icône succès |
| `--pico-icon-valid` | Icône succès |

### Erreur / Invalide (ROUGE)

| Variable | Cas d’usage |
|--------|-------------|
| `--pico-form-element-invalid-border-color` | Indicateur erreur |
| `--pico-form-element-invalid-active-border-color` | Erreur active |
| `--pico-form-element-invalid-focus-color` | Focus erreur |
| `--pico-del-color` | Texte / icône erreur |
| `--pico-icon-invalid` | Icône erreur |

---

## 4. Surfaces & cartes

| Variable | Cas d’usage |
|--------|-------------|
| `--pico-card-background-color` | Fond de carte |
| `--pico-card-border-color` | Bordure de carte |
| `--pico-card-box-shadow` | Ombre de carte |
| `--pico-card-sectioning-background-color` | Sous-section de carte |

---

## 5. Formulaires (custom UI)

| Variable | Cas d’usage |
|--------|-------------|
| `--pico-form-element-background-color` | Fond input |
| `--pico-form-element-border-color` | Bordure input |
| `--pico-form-element-color` | Texte input |
| `--pico-form-element-placeholder-color` | Placeholder |
| `--pico-form-element-focus-color` | Focus input |
| `--pico-form-element-disabled-opacity` | Input désactivé |

---

## 6. Composants spécifiques

| Groupe | Variables |
|------|-----------|
| Dropdown | `--pico-dropdown-*` |
| Tooltip | `--pico-tooltip-*` |
| Progress | `--pico-progress-*` |
| Switch | `--pico-switch-*` |
| Range | `--pico-range-*` |
| Accordion | `--pico-accordion-*` |
| Modal | `--pico-modal-overlay-background-color` |

---

## 7. Variables à éviter en UI custom

> Réservées au moteur Pico

- `--pico-h1-color` → `--pico-h6-color`
- `--pico-code-*`
- `--pico-mark-*`
- `--pico-icon-*` (sauf cas précis)

---

## 8. Règles d’usage (résumé)

- Accent → `--pico-primary*`
- Succès / actif → `--pico-form-element-valid-*`
- Erreur / inactif → `--pico-form-element-invalid-*`
- Neutre → `--pico-muted-*`
- **Jamais de couleurs hardcodées**

---

Document volontairement concis.
