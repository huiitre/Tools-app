# Diagrammes de l'API

**Régénérés automatiquement à chaque compilation de l'API** : la cible MSBuild `GenerateDiagrams`
de `Tools.Api.csproj` lance le script après le build, donc `dotnet build`, `dotnet run`,
`dotnet watch` et le débogage VS Code les tiennent à jour sans y penser. Ces fichiers ne se
modifient pas à la main.

La cible est **incrémentale** (`Inputs`/`Outputs`) : elle est sautée tant qu'aucun `.cs` n'est plus
récent que les diagrammes — sinon `dotnet watch` la relancerait à chaque sauvegarde. Elle est aussi
en `ContinueOnError` : Python ou Graphviz manquants ne doivent jamais casser une compilation. Et le
script pose un verrou, car une compilation qui construit aussi les tests déclenche la cible deux
fois en parallèle.

À la main si besoin : `npm run api:diagrams`.

```
diagrams/
├── uml.py                    le générateur
├── 00-modules.md             la vue d'ensemble
├── Core/
│   ├── Auth/        auth.drawio + auth.md
│   ├── GameServers/ gameservers.drawio + gameservers.md
│   └── …
└── EliteDangerous/
    └── RoadToRiches/
```

## Ce qu'ils montrent

Un **graphe de dépendances**, pas un diagramme de classes : chaque boîte porte un nom de type,
rien d'autre. Les signatures de méthodes ont été essayées puis retirées — elles débordaient des
boîtes une fois rendues, et répéter les mêmes méthodes sur un port et sur son adaptateur
n'apprenait rien.

`00-modules.md` répond à la question d'ensemble : quel sous-module dépend de quel autre. C'est là
qu'on vérifie la règle du projet — **un module métier peut dépendre de Core, jamais l'inverse, et
jamais d'un autre module métier**.

Un dossier par sous-module ensuite, parce que le Core compte 285 types et qu'il est illisible d'un
seul tenant.

## Les deux natures de lien

| flèche | sens | lue dans |
|---|---|---|
| `A --> B` | **A dépend de B** | le constructeur de A, ses paramètres `[FromServices]`, et les entités de domaine qu'elle manipule |
| `A -.-> B` | **A implémente B** | la liste après `:` de la déclaration |

Ensemble, elles donnent le schéma d'inversion de dépendances : le use case pointe vers le port,
l'adaptateur remonte vers ce même port.

Les contrôleurs de ce projet résolvent leurs use cases **par action** (`[FromServices]`) et non
dans leur constructeur : ces paramètres comptent donc comme des dépendances, sinon un contrôleur
paraîtrait ne dépendre de rien.

Les entités de domaine sont incluses parce que l'injection ne dit pas tout : un use case ne
construit pas son entité, il la manipule — sans quoi elle apparaîtrait seule. Les **énumérations**
en sont exclues : `RoleCode` produisait 44 flèches pour aucune information. Les types de câblage
posés à la racine d'un module (`CoreModule`) le sont aussi.

## Le placement

Calculé par **Graphviz** (`dot -Tplain`), dont l'algorithme hiérarchique range les nœuds par rang
de dépendance et minimise les croisements — un tri maison n'y arrivait pas, les flèches
traversaient les boîtes. Graphviz est donc requis pour les `.drawio`.

L'arête d'implémentation est déclarée **en sens inverse et invisible** pour le seul calcul des
rangs : sans ça l'adaptateur se retrouvait à gauche du port, au niveau des use cases. On obtient
la lecture attendue :

```
Api  →  Use cases  →  Ports  →  Adaptateurs  →  Domain
```

Les couches se lisent à la couleur : bleu Api, vert Application, orange Domain, rouge
Infrastructure. Les interfaces sont arrondies.

**Limite connue** : une flèche qui saute un rang traverse ce qui se trouve sur son chemin, draw.io
ne contournant pas les obstacles. Seuls les points d'entrée et de sortie sont maîtrisés.

## Les lire et les modifier

**`.drawio` — le format de travail.** Ouvre-le dans VS Code (extension `hediet.vscode-drawio`) :
le diagramme s'affiche, éditable. C'est du XML en clair, donc lisible en diff — mais **régénéré à
chaque exécution** : ce qui est retouché à la main est perdu. Pour garder un schéma annoté,
l'enregistrer sous un autre nom.

**`.md` — le format de lecture.** Un bloc ```mermaid``` que GitHub rend nativement. Dans VS Code
il demande `bierner.markdown-mermaid` et la prévisualisation (`Ctrl+Shift+V`) ; sur certaines
installations le conteneur s'affiche sans le diagramme, d'où l'existence du `.drawio`.

Le contenu du bloc se colle aussi tel quel dans un board Whimsical (Ctrl+V), qui reconnaît le
Mermaid — d'où le choix de `flowchart` plutôt que `classDiagram`, seul format qu'il importe.
