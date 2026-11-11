# Traduction

Traduit des textes depuis tools (page du module) et, à terme, depuis une extension Firefox /
Chrome. Le moteur est le conteneur LibreTranslate du NAS ; un autre (LLM) pourra s'y brancher.
Les textes ne sont jamais stockés.

## État (11/11/2026)

| Chantier | État |
|---|---|
| Migration `V2.80.0__translation_module.sql` (ligne du module) | écrite, testée sur une copie de la prod |
| API : traduire, langues, détection (US-1 à US-5) | livrée, testée contre le vrai LibreTranslate |
| Bruno | fait (`Tools API Core/Translation`) |
| Front (page du module, `web/src/modules/Translation/`) | livré le 11/11/2026, vu en capture (sombre, clair, mobile, erreur) |
| Extension navigateur | à faire |

## Accès

- **Utilisateur** : membre du module `translation`, à partir de **READ_ONLY** (à attribuer dans
  l'admin, page Modules). Un administrateur du site hors du module est refusé.
- **Extension** : clé d'API portant `translation:translate` (en-tête `X-Api-Key`).

Les trois use cases déclarent la même chose : `RequiredModule => Translation`,
`RequiredRole => ReadOnly`, `RequiredScope => TranslationTranslate`.

## Comment ça marche

```
contrôleur → use case (droits, préparation, mesure, journal)
           → TranslationService (traduire, rien d'autre)
           → ITranslationProvider ← LibreTranslateProvider
```

- **`TranslationService`** ne fait que traduire et vérifier qu'on récupère un texte par texte
  envoyé. Ce qui se passe avant et après appartient au use case. Un autre module peut l'appeler
  directement, sans HTTP.
- **Le moteur** se choisit dans `TranslationModule.cs` (une ligne). Il expose un `Name`
  (`libretranslate`), écrit dans le journal et renvoyé au client.
- **Limite : 5 000 caractères par requête, au total des fragments.** Au-delà, `400` avant tout
  appel au moteur. Mesuré sur le NAS : ~630 caractères/s, deux fois plus lent quand la paire passe
  par l'anglais (FR → ES), et LibreTranslate traite les requêtes en file (`LT_THREADS=4`). Le front
  et l'extension bloqueront aussi à 5 000. Le conteneur a `LT_CHAR_LIMIT=5000` en filet.
- **Détection automatique** : `source` absent ou `"auto"`. Sur un lot de fragments, LibreTranslate
  détecte **une seule langue pour tout le lot** : un fragment dans une autre langue reste tel quel.
- **Délai** : 60 s vers le moteur, puis `503 TRANSLATION_UNAVAILABLE`.

## Journal

Chaque traduction écrit une ligne `TRANSLATION` dans `application_logs` : `TRANSLATED`, ou
`FAILED` si le moteur refuse ou ne répond pas (les erreurs de saisie, refusées avant, ne sont pas
journalisées). `user_id` pour un utilisateur, `apiKeyId` / `apiKeyName` pour une clé.

| Métadonnée | Rôle |
|---|---|
| `source`, `detectedSource`, `target` | paire de langues (source demandée, ou détectée) |
| `characters`, `segments` | volume : total des caractères, nombre de fragments |
| `durationMs` | durée mesurée dans le use case, du début à la fin de l'appel |
| `provider` | moteur utilisé |
| `error` | code d'erreur pour un `FAILED` |

**Jamais le texte**, ni à traduire ni traduit. Il n'y a pas de table d'historique : ce journal en
tient lieu.

## User stories

| Story | En tant que… | Route |
|---|---|---|
| US-1 Traduire un texte | membre : un texte, langue cible, source facultative → texte traduit | `POST /translation/translate` |
| US-2 Lister les langues | membre : les langues et leurs cibles, pour les listes déroulantes (lues chez le moteur) | `GET /translation/languages` |
| US-3 Détecter une langue | membre : langues candidates, la plus probable en premier | `POST /translation/detect` |
| US-4 Traduire depuis l'extension | service (clé) : même chose que US-1 | `POST /translation/translate` |
| US-5 Traduire une page | extension : plusieurs fragments en un appel, rendus dans l'ordre | `POST /translation/translate` |

## Configuration

| Clé | Défaut | Rôle |
|---|---|---|
| `Translation:LibreTranslate:BaseUrl` | `http://libretranslate:5000` (réseau `network_tools`) ; dev : `http://localhost:5001` via le tunnel de dev-console | adresse du moteur |
| `Translation:LibreTranslate:TimeoutSeconds` | `60` | délai avant `503` |
| `Translation:LibreTranslate:ApiKey` | aucune | seulement si LibreTranslate exige une clé (`LT_API_KEYS`) |

Langues chargées sur le NAS (`LT_LOAD_ONLY`) : en, fr, es, de, it, pt, nl, pl, ru, uk.

## Où est le code

| Quoi | Fichier |
|---|---|
| Règles d'une demande (limite, langues) | `Domain/TranslationRequest.cs` |
| Contrat d'un moteur | `Application/Ports/ITranslationProvider.cs` |
| Service partagé | `Application/Services/TranslationService.cs` |
| Use cases | `Application/Usecases/` |
| Adaptateur LibreTranslate | `Infrastructure/LibreTranslate/` |

## Décisions (11/11/2026)

- **Un service qui ne fait que traduire**, des use cases qui préparent et journalisent : pas de
  duplication du « traduire », et chaque besoin futur (fichiers, format personnalisé) devient un
  use case de plus.
- **Un seul scope pour l'extension** (`translation:translate`), qui ouvre aussi les langues et la
  détection : ce sont des outils du même geste.
- **Pas de table** : le journal applicatif garde langues, volume et durée ; le texte n'est jamais conservé.
- **5 000 caractères en dur** pour commencer ; les textes plus longs (découpage en tâche de fond)
  viendront plus tard.
- **Le nom du moteur n'apparaît jamais dans ce que voit l'utilisateur** (description du module,
  messages d'erreur) : c'est un détail technique.

## Hors périmètre (pour l'instant)

- Textes de plus de 5 000 caractères, fichiers, format de sortie personnalisé.
- Historique consultable par l'utilisateur.
- Limite de débit par clé (une clé d'extension qui fuit peut occuper le moteur).
- Second moteur (LLM).
