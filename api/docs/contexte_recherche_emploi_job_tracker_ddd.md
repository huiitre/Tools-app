# Module Job Tracker — suivi de candidatures

Module de suivi de candidatures : collecter des offres d'emploi, y rattacher ses propres
candidatures et en suivre l'avancement.

Le module se construit en deux temps. La **V1** est un domaine saisi à la main, volontairement
étroit. La **V2** ajoute la collecte automatique des offres et la déduplication, qui est le vrai
morceau du sujet.

> Ce document vient d'une réflexion menée hors du contexte du dépôt. Le module est en réalité
> développé **dans l'API C# de ce monorepo** (`api/`), sur **PostgreSQL**. La section
> « Architecture et intégration » a été alignée en conséquence ; le domaine métier, lui, est
> inchangé.

---

## 1. Idée directrice : l'offre n'est pas la candidature

Le point fondamental du domaine est cette séparation :

```text
JobOffer     l'offre d'emploi, telle qu'elle est publiée
Application  la candidature d'un utilisateur à cette offre
```

Une offre existe **indépendamment** de toute candidature.

C'est ce qui rend le modèle tenable en V2 : un scraper importera des centaines d'offres, alors
qu'un utilisateur ne candidatera qu'à quelques-unes.

```text
500 JobOffer
 12 Application
```

Confondre les deux obligerait à créer une candidature fictive pour chaque offre collectée.

---

## 2. Multi-utilisateur : ce qui est partagé, ce qui est privé

L'application est multi-utilisateur : chacun mène ses propres recherches et suit ses propres
candidatures.

### Données partagées

Mutualisées entre tous les utilisateurs, donc **sans `user_id`** :

- `Company`
- `JobOffer`
- `Technology`
- `JobOfferTechnology`

### Données personnelles

La candidature appartient à un utilisateur — `Application.user_id` est **obligatoire**.

Deux utilisateurs peuvent donc candidater à la même offre sans que l'offre soit dupliquée :

```text
User A ── Application A ──┐
                          ├── JobOffer
User B ── Application B ──┘
```

Même principe pour une entreprise : une seule ligne `Company`, quel que soit le nombre de
candidats.

---

## 3. Schéma de données V1

Objectif : démarrer simple, ne pas sur-concevoir.

### USER

```text
USER
----
id
email
password_hash
created_at
updated_at
```

L'utilisateur existe déjà dans le module Core : **cette table n'est pas à créer**. On référence
`tools_core.users` — pas de table utilisateur propre au module, pas de duplication d'identité.

### COMPANY

```text
COMPANY
-------
id
name
website
created_at
updated_at
```

Pas de `user_id` — une entreprise est une donnée partagée.

### JOB_OFFER

```text
JOB_OFFER
---------
id
company_id

source
external_id
url

title
description

contract_type
city

remote_policy
remote_days_per_week

salary_min
salary_max

experience_min_years
experience_max_years

published_at
expires_at

created_at
updated_at
```

### TECHNOLOGY

```text
TECHNOLOGY
----------
id
name
category
created_at
```

Exemples :

| name | category |
|---|---|
| C# | LANGUAGE |
| Java | LANGUAGE |
| JavaScript | LANGUAGE |
| TypeScript | LANGUAGE |
| Vue.js | FRAMEWORK |
| React | LIBRARY |
| ASP.NET | FRAMEWORK |
| Spring Boot | FRAMEWORK |
| Oracle | DATABASE |
| PostgreSQL | DATABASE |
| Docker | TOOL |
| Kubernetes | TOOL |

### JOB_OFFER_TECHNOLOGY

Relation N-N entre offres et technologies.

```text
JOB_OFFER_TECHNOLOGY
--------------------
job_offer_id
technology_id
is_required
```

Exemple :

```text
C#        REQUIRED
ASP.NET   REQUIRED
Vue.js    REQUIRED
Docker    OPTIONAL
```

> **Ne pas créer une colonne par technologie.** Le tableau `has_java`, `has_csharp`, `has_vue`,
> `has_react`, `has_docker`… est exactement ce que cette table N-N évite.

### APPLICATION

```text
APPLICATION
-----------
id

user_id
job_offer_id

status
applied_at

expected_salary
proposed_salary

next_action_at
notes

created_at
updated_at
```

`user_id` est obligatoire.

### Énumérations

`source` :

```text
HELLOWORK
APEC
LINKEDIN
WELCOME_TO_THE_JUNGLE
FRANCE_TRAVAIL
COMPANY_WEBSITE
OTHER
```

`contract_type` :

```text
CDI
CDD
FREELANCE
INTERNSHIP
APPRENTICESHIP
OTHER
```

`remote_policy` :

```text
ONSITE
HYBRID
REMOTE
UNKNOWN
```

---

## 4. Statuts de candidature

```text
SAVED
APPLIED
CONTACTED
HR_INTERVIEW
TECHNICAL_INTERVIEW
FINAL_INTERVIEW
REJECTED
OFFER_RECEIVED
ACCEPTED
WITHDRAWN
```

En V1, un simple champ `status` sur `Application` suffit. Un historique des statuts pourra être
ajouté plus tard, si un besoin réel apparaît.

---

## 5. Relations

```text
Company
   │
   └── 1..N JobOffer
                │
                ├── N..N Technology
                │
                └── 0..N Application
                            │
                            └── User
```

Vue par entité :

```text
USER
  └── 1..N APPLICATION

COMPANY
  └── 1..N JOB_OFFER

JOB_OFFER
  ├── N..N TECHNOLOGY
  └── 0..N APPLICATION
```

Exemple complet :

```text
Company
└── Acme

JobOffer
├── title = "Développeur Full Stack C# / Vue"
├── salary = 40k - 45k
├── remote = HYBRID
├── experience_min = 3
│
├── C#         REQUIRED
├── ASP.NET    REQUIRED
├── Vue.js     REQUIRED
├── Docker     OPTIONAL
│
├── Application User A
│   ├── status = TECHNICAL_INTERVIEW
│   └── expected_salary = 42000
│
└── Application User B
    ├── status = APPLIED
    └── expected_salary = 40000
```

---

## 6. Hors périmètre V1

Ces concepts sont identifiés mais **ne doivent pas être ajoutés sans besoin réel** :

- `Interview`
- `FollowUp`
- `Contact`
- `ApplicationStatusHistory`
- `ApplicationAssessment`
- `JobOfferSnapshot`
- historique de salaire
- historique de modification d'annonce

Ils pourront devenir des entités ou des value objects plus tard, si le métier l'exige. L'objectif
est de laisser émerger le domaine plutôt que de le décréter.

---

## 7. V2 — collecte automatique des offres

À terme, le module collecte les offres sur plusieurs plateformes.

Objectifs :

- récupérer automatiquement les offres ;
- normaliser les données ;
- extraire les technologies ;
- éviter les doublons ;
- permettre la consultation des offres collectées ;
- rattacher ensuite une candidature personnelle à une offre partagée.

```text
Sources d'emploi
      │
      ▼
   Scrapers
      │
      ▼
 RawJobOffer
      │
      ▼
 Normalisation
      │
      ▼
 Déduplication
      │
      ▼
   JobOffer
```

---

## 8. Déduplication

### Le problème

Une même offre apparaît sur plusieurs plateformes, sous des titres différents :

```text
HelloWork : "Développeur C# .NET H/F - Lyon"
LinkedIn  : "Software Engineer .NET - Lyon"
Apec      : "Développeur Back-End C#"
```

Ces trois annonces peuvent correspondre au **même recrutement**. Un identifiant externe ne suffit
donc pas : il n'a de sens qu'à l'intérieur d'une seule source.

### Étage 1 — règles déterministes

Avant toute IA.

Doublons certains ou très probables :

```text
même source + même external_id
même URL normalisée
```

Contrainte envisagée : `UNIQUE(source, external_id)`.

Candidats à une comparaison approfondie, par préfiltrage :

- entreprise identique ou proche ;
- ville / zone géographique proche ;
- titre similaire ;
- dates de publication proches ;
- salaire proche ;
- technologies communes.

Le préfiltrage n'est pas une optimisation, c'est une nécessité : comparer toutes les offres deux à
deux avec un LLM est hors de question.

```text
500 offres → 500 × 499 / 2 = 124 750 comparaisons
```

### Étage 2 — embeddings

Pour la similarité sémantique, avant tout LLM génératif :

```text
Description offre
      │
      ▼
  Embedding
      │
      ▼
   Vecteur
      │
      ▼
Cosine similarity
```

```text
Offer A ↔ Offer B = 0.94
Offer A ↔ Offer C = 0.41
```

### Étage 3 — petit LLM local, pour les cas ambigus seulement

Piste envisagée : un Qwen local, modèle léger (~2B / 3B). Il ne sert pas à coder — uniquement à
classifier des annonces déjà présélectionnées.

Entrée :

```json
{
  "offerA": {
    "company": "Acme",
    "title": "Développeur .NET C#",
    "city": "Lyon",
    "salary": "40-45k",
    "technologies": ["C#", ".NET", "SQL Server"],
    "description": "..."
  },
  "offerB": {
    "company": "Acme SAS",
    "title": "Software Engineer C#",
    "city": "Lyon 3e",
    "salary": "40000-45000",
    "technologies": ["C#", "ASP.NET Core", "SQL Server"],
    "description": "..."
  }
}
```

Sortie structurée attendue :

```json
{
  "sameOffer": true,
  "confidence": 0.93,
  "reason": "Same company, location, salary range and highly similar responsibilities."
}
```

### Pipeline complet

```text
             Scrapers
                │
                ▼
           RawJobOffer
                │
                ▼
          Normalisation
                │
        ┌───────┴─────────┐
        ▼                 ▼
 Exact matching      Embeddings
 source/id/url        similarity
        │                 │
        └────────┬────────┘
                 ▼
         Ambiguous matches
                 │
                 ▼
            Local Qwen
                 │
                 ▼
        DuplicateDecision
                 │
        ┌────────┴────────┐
        ▼                 ▼
existing JobOffer     new JobOffer
```

**Règle générale : le LLM n'est jamais la seule source de vérité.** Ordre imposé :

1. exact matching
2. règles déterministes
3. embeddings
4. petit LLM, uniquement pour les cas ambigus

### Conséquence sur le schéma V1

La déduplication casse le schéma tel qu'il est écrit au §3. `JOB_OFFER` porte `source`,
`external_id` et `url` comme **colonnes scalaires** : une offre ne peut donc appartenir qu'à une
seule plateforme. Or fusionner trois annonces HelloWork / LinkedIn / Apec en une seule `JobOffer`
demande d'en conserver les trois provenances — sinon deux des trois URL sont perdues, et un
re-scrape recrée le doublon qu'on vient de supprimer.

Il faut donc sortir la provenance dans une table fille :

```text
JOB_OFFER_SOURCE
----------------
id
job_offer_id

source
external_id
url

first_seen_at
last_seen_at
```

avec `UNIQUE(source, external_id)` déplacé sur cette table — c'est là qu'il a du sens.

Ce n'est **pas** une entité à créer en V1 : tant qu'il n'y a pas de collecte, une offre saisie à la
main n'a qu'une provenance. Mais c'est la première chose à migrer quand la V2 démarre, et le savoir
maintenant évite d'écrire du code V1 qui suppose une provenance unique.

### Scoring

Exemple de signaux agrégés :

```text
same_external_id       = true
same_url               = false
same_company           = true
same_location          = true
semantic_similarity    = 0.91
llm_same_offer         = true
llm_confidence         = 0.94
```

Décision possible :

```text
>= 0.95        fusion automatique
0.80 - 0.95    doublon probable → validation
< 0.80         offres distinctes
```

Ces seuils **doivent être calibrés sur de vraies données**. Les coder en dur dès le départ serait
de l'arbitraire déguisé en règle.

---

## 9. Architecture et intégration au monorepo

### Stack

```text
C#
ASP.NET Core
approche DDD
DIP / DI
PostgreSQL, accès par Dapper sur du SQL écrit à la main
```

Pas d'ORM : c'est la règle du dépôt (`AGENTS.md`), pas une préférence locale. Le principe « le
Domain ne dépend pas de la persistance » reste identique, il se joue simplement contre un
repository Dapper au lieu d'un `DbContext`.

### Séparation des couches

L'objectif est une vraie séparation des responsabilités, pas une pseudo-DDD qui se limiterait à un
découpage de dossiers.

Le **Domain** ne dépend pas :

- d'ASP.NET ;
- d'un ORM ;
- de la base de données ;
- du scraper.

Le reste :

- les règles métier vivent dans le Domain **lorsqu'elles relèvent réellement du métier** ;
- les cas d'usage vivent dans l'Application ;
- les adapters techniques vivent dans l'Infrastructure ;
- les contrôleurs / endpoints restent dans la couche API ;
- DIP et DI de bout en bout ;
- **ne pas inventer d'abstraction qui ne répond à aucun besoin réel.**

### Structure : un module de `api/`, pas une solution à part

Le module vit dans l'arborescence existante, au même titre qu'`EliteDangerous` :

```text
api/Modules/JobTracker/
├── JobTrackerModule.cs              composition explicite du module
└── <SousDomaine>/
    ├── Api/                         contrôleurs
    ├── Application/
    │   ├── Commands/
    │   ├── Usecases/                un use case = une classe
    │   └── Views/                   DTO de sortie
    ├── Domain/
    └── Infrastructure/              repositories Dapper
```

Les couches Domain / Application / Infrastructure de la réflexion initiale sont conservées — elles
deviennent des dossiers du module au lieu de projets `.csproj` séparés.

Le nom définitif du module n'est pas encore arrêté (`JobTracker` est un nom de travail ; le schéma
SQL suivrait, `tools_job_tracker`).

### Règles du dépôt qui s'appliquent à ce module

- **Un use case = une classe.** S'il exige des droits, il hérite de `SecuredUseCase`
  (voir `api/docs/SECURITY.md`) ; sinon c'est une classe ordinaire.
- **La sécurité vit dans le use case, jamais sur la route.** Pas d'`[Authorize]`, pas de policy
  HTTP : le contrôle de rôle est dans le use case.
- **Les contrôleurs résolvent leurs use cases par action** (`[FromServices]`), jamais dans le
  constructeur — un use case sécurisé applique son contrôle dès sa construction.
- **Un contrôleur appelle toujours un use case**, jamais directement un service ou un repository.
- **Composition explicite** dans `JobTrackerModule.cs`, aucune découverte par réflexion.
- **Migrations** : un nouveau fichier `database/sql/V2.x.y__nom.sql`. On ne modifie jamais un
  fichier de migration existant.
- **Bruno** : toute route ajoutée, modifiée ou supprimée est répercutée dans `bruno/` dans le même
  commit. Une route sans son entrée Bruno n'est pas testable, donc pas terminée.

### Piège connu sur les traitements planifiés

Le rappel de relance (§13) tournera sur un `BackgroundService`. **Une classe appelée par un
planificateur ne doit jamais hériter de `SecuredUseCase`** : il n'y a pas d'utilisateur authentifié
sur un thread de fond, et la résolution du use case échoue au démarrage. Le scheduler doit appeler
un use case non sécurisé, qui reçoit l'identifiant de l'utilisateur en paramètre.

---

## 10. Point de départ

Ne pas commencer par le scraper ni par le LLM.

Premier domaine, strictement :

```text
Company
JobOffer
Technology
Application
```

Premières tables :

```text
COMPANY
JOB_OFFER
TECHNOLOGY
JOB_OFFER_TECHNOLOGY
APPLICATION
```

avec la référence à l'utilisateur existant du module Core.

La V2 introduira la collecte et la déduplication **quand la V1 fonctionnera**.

---

## 11. Recherche externe — comment font les outils existants

Notes issues d'une recherche externe sur les outils payants du marché (Huntr, Teal, Jobscan,
LoopCV). **Rien ici n'est une décision** : c'est de la matière pour la V2.

### Trois techniques de collecte

1. **Les API internes.** Plutôt que de lire le HTML, on intercepte les requêtes réseau (onglet
   Network des devtools). Beaucoup de sites chargent leurs offres par XHR/AJAX et renvoient un
   JSON déjà structuré. C'est la méthode la plus stable — et la moins coûteuse à maintenir, parce
   qu'un changement de maquette ne la casse pas.
2. **Les flux standardisés.** Les gros sites exposent souvent du XML/RSS, ou du `JobPosting`
   Schema.org dans le HTML pour Google Jobs. Format normalisé, donc un seul parseur pour plusieurs
   sources.
3. **Navigateur headless.** Playwright ou Puppeteer en mode stealth pour les sites protégés,
   généralement accompagnés de proxys résidentiels tournants. C'est la voie la plus fragile et la
   plus chère.

> **Réserve à garder en tête.** Les conditions d'utilisation de plusieurs plateformes — LinkedIn en
> tête — interdisent explicitement la collecte automatisée, et la rotation de proxys ou le
> contournement de CAPTCHA sert précisément à passer outre un contrôle d'accès. Techniquement
> faisable, juridiquement exposé. Les points 1 et 2, sur des sources qui publient volontairement
> leurs offres, ne posent pas ce problème — pas plus que l'extension navigateur décrite plus bas,
> qui est d'ailleurs la réponse des outils commerciaux à cette contrainte.

### Obtenir le nom exact de l'entreprise

C'est le point dur : le scraping brut ramène du parasite (« Recrutement par un cabinet de
conseil », « Entreprise confidentielle », mentions `SAS` / `SA`).

1. **Sélecteurs stricts + regex de nettoyage** — extraction ciblée sur la balise de l'entreprise,
   puis suppression des formes juridiques et des espaces superflus.
2. **Enrichissement par API tierce** — c'est là que se joue la précision. Le nom brut est envoyé à
   une base d'entreprises qui renvoie la raison sociale exacte, le site et le logo. En France,
   l'**API Sirene de l'Insee** est gratuite et fait exactement ça ; Clearbit ou Lusha sont les
   équivalents payants.
3. **Extension navigateur** — l'approche de Teal et Huntr : plutôt qu'un serveur qui scrape,
   l'extension lit le DOM de la page que l'utilisateur consulte. Comme il est déjà authentifié sur
   le site, rien ne bloque la lecture.

L'API Sirene mérite d'être notée : elle donne un identifiant stable (SIREN) qui ferait une bien
meilleure clé de déduplication d'entreprise qu'une comparaison de chaînes.

### Fonctionnalités repérées chez les outils payants

- **Score de compatibilité CV / offre (ATS).** Comparaison des mots-clés de l'offre avec le CV,
  restituée en pourcentage.
- **Extension navigateur « ajouter à mon suivi ».** Un bouton sur la page de l'offre, qui aspire
  l'annonce en un clic. Bien moins coûteux qu'un scraper par site, et sans le problème de blocage.
- **Relances automatisées.** « Candidature envoyée il y a 7 jours, aucune réponse → relancer. »
- **Génération de lettre de motivation par LLM**, en croisant l'offre et le CV.
- **Statistiques de conversion.** Le tunnel `40 candidatures → 5 entretiens → 1 offre`, pour voir
  où le processus bloque.

---

## 12. Sources de collecte — état des lieux par plateforme

> **Statut : non vérifié.** Ces notes viennent d'une recherche menée par un assistant externe. Les
> noms de champs, formes d'endpoints et scopes cités **n'ont pas été confrontés aux API réelles**.
> Rien ici ne doit être transformé en code d'adapter sans avoir d'abord ouvert la doc officielle ou
> appelé l'endpoint à la main. Les ordres de grandeur et le classement sont en revanche cohérents
> avec ce qui est connu de ces plateformes.

### Classement

| Source | API exploitable | Scraping HTML | Verdict |
|---|---|---|---|
| France Travail | oui, officielle | inutile | **Excellent** |
| Apec | pas d'API candidat publique, endpoints du site observables | plutôt facile | Très intéressant |
| HelloWork | pas d'API de recherche publique trouvée | assez simple | Très intéressant |
| Welcome to the Jungle | API officielle, accès global sous partenariat | possible, moins intéressant | À étudier |
| Indeed | API GraphQL, recherche globale réservée aux partenaires | techniquement possible, moins propre | Secondaire |
| LinkedIn | APIs très contrôlées | interdit sans autorisation | **À écarter** |

### France Travail — priorité n°1

API officielle publiée sur `francetravail.io`, pensée pour que des applications externes exploitent
les données. L'accès passe par une application déclarée et une authentification.

```text
FranceTravailProvider
        │
        ▼
  API France Travail
        │
        ▼
    RawJobOffer
```

Ni navigateur headless, ni parsing HTML, ni endpoint qui disparaît du jour au lendemain. C'est la
seule source de la liste qui offre un contrat stable.

### Apec — endpoints internes du site

Pas d'API publique destinée à récupérer l'ensemble des offres côté candidat. L'API annoncée vise
les ATS qui *publient* vers l'Apec — l'inverse du besoin ici.

En revanche le moteur de recherche du site s'appuie sur des appels JSON observables, qui exposent
des champs très proches du modèle défini plus haut : `numeroOffre`, `intitule`, `nomCommercial`,
`lieuTexte`, `salaireTexte`, `texteOffre`, `datePublication`, `typeContrat`, coordonnées
géographiques, télétravail.

**Un endpoint interne n'est pas un contrat.** Il peut changer sans préavis et sans version. À
traiter comme une source best-effort, jamais comme une dépendance dure.

### HelloWork — HTML rendu côté serveur

Pas d'API XHR renvoyant les résultats : les offres sont dans le HTML, et la pagination se réduit à
un paramètre.

```text
?p=2
?p=3
```

Un `HttpClient` et un parseur HTML suffisent — pas de Playwright. Les pages exposent titre,
entreprise, ville, contrat, salaire, expérience, télétravail, description, date et référence.

### Welcome to the Jungle — vraie API, mais portail fermé

Endpoints cités :

```text
GET /api/v1/external/jobs/:reference
GET /api/v1/external/jobs
GET /api/v1/external/jobs/all
```

avec un modèle proche du nôtre : `salary.min` / `salary.max` / `salary.currency` / `salary.period`,
`remote`, `experience_level`, `education_level`, `contract_type`, `office`, `description`,
`profile`, `published_at`, `apply_url`.

Le piège : l'accès global demanderait un partenariat et un scope dédié. Pas de clé personnelle qui
donnerait toute la base.

À noter quand même : que leur modèle public ressemble à ce point au schéma du §3 est un bon signal
sur le découpage retenu — notamment le salaire en min/max/devise/période plutôt qu'en texte libre.

### Indeed — API réelle, accès fermé

Infrastructure GraphQL avec une vraie `jobSearch(location:, what:, limit:)`. Mais l'accès au
service de recherche est réservé aux partenaires, ATS et employeurs ; une clé développeur ordinaire
se voit refuser l'accès. À garder en réserve.

### LinkedIn — ne rien construire dessus

Les conditions d'utilisation interdisent explicitement scraping, crawlers, bots, scripts de
collecte et contournement des restrictions. Les APIs Jobs existent mais sont soumises à validation
explicite et tournent autour de la publication côté recruteur.

Le risque n'est pas seulement un adapter fragile : c'est le blocage du compte. **Pas de
`LinkedInProvider` dans ce module.**

### Le port de collecte

L'abstraction proposée, qui est la bonne : le domaine ne saura jamais si une offre vient d'un REST,
d'un HTML HelloWork ou d'un JSON interne. C'est exactement le rôle des adapters d'infrastructure.

```csharp
public interface IJobOfferProvider
{
    Task<IReadOnlyCollection<RawJobOffer>> SearchAsync(
        JobSearchCriteria criteria,
        CancellationToken cancellationToken);
}
```

```text
IJobOfferProvider
│
├── FranceTravailProvider        API officielle
├── ApecProvider                 endpoints JSON du site
├── HelloWorkProvider            HTML
├── WelcomeToTheJungleProvider   API si accès obtenu
└── IndeedProvider               plus tard
```

Puis convergence :

```text
RawJobOffer → Normalizer → DuplicateDetector → JobOffer
```

Ordre d'implémentation :

```text
1. France Travail       API officielle
2. HelloWork            HTML simple
3. Apec                 JSON interne
4. Welcome to the Jungle  selon accès
5. Indeed               si accès exploitable
6. LinkedIn             aucun adapter
```

### Réserves sur ce port

Le contrat ci-dessus est juste dans son intention, mais quatre points le rattraperont :

- **`JobSearchCriteria` va dériver vers le plus petit dénominateur commun.** Chaque source filtre
  différemment. Plutôt que d'imposer les mêmes critères à tous, laisser chaque provider **déclarer
  ce qu'il sait faire** et ignorer explicitement le reste — un filtre silencieusement ignoré produit
  des résultats faux, pas des résultats incomplets.
- **Rien ne porte l'incrémental.** Un import planifié ne doit pas re-télécharger tout le catalogue à
  chaque passage : il faut un `since` ou un curseur, sinon la source finira par bloquer les appels.
- **Tout ramener d'un coup ne tient pas à l'échelle.** Un `IAsyncEnumerable<RawJobOffer>` laisse
  l'adapter gérer sa pagination sans charger des milliers d'offres en mémoire.
- **Quotas et limites de débit appartiennent à l'adapter**, pas au use case. France Travail impose
  des quotas ; le domaine n'a pas à en connaître l'existence.

Enfin, `RawJobOffer` devrait **conserver la charge brute** (JSON ou HTML d'origine). Quand le
normaliseur évoluera — et il évoluera — rejouer la normalisation sur du brut stocké évite de
re-solliciter les sources. C'est précisément le `JobOfferSnapshot` mis de côté au §6 : parqué en V1,
mais c'est en V2 qu'il devient utile.

Et chaque provider produit naturellement un triplet `(source, external_id, url)` — c'est-à-dire
exactement une ligne de `JOB_OFFER_SOURCE` (§8), pas trois colonnes sur `JOB_OFFER`.

### Précédent dans le dépôt

Ce type d'ingestion planifiée existe déjà côté monorepo : `GameServersPollingService` dans l'API C#,
et les modules de sync Dofus / Palworld / Riot côté Java. Le déclenchement peut être un
`BackgroundService` interne ou une route de sync appelée par un cron externe — les deux ont un
précédent. Voir le piège `SecuredUseCase` du §9.

---

## 13. Fonctionnalités candidates

Rien de ce qui suit n'est en V1. À arbitrer quand le socle tourne.

### Ce que la plateforme fournit déjà

Le module n'est pas seul : `api/` porte déjà des briques transverses qu'il serait absurde de
réécrire.

| Besoin | Brique existante |
|---|---|
| Rappel de relance à J+7 | module `Notifications` + push temps réel (SignalR) |
| Relance par courriel | module `Mail` |
| Préférences (salaire visé, villes, télétravail) | module `Settings`, avec portée par utilisateur |
| Accès au module, rôles | `tools_core.module` + `user_module_role` |

Concrètement : `Application.next_action_at` est déjà dans le schéma V1. Un `BackgroundService` qui
balaie les échéances et pousse une notification suffit — pas de nouveau système de rappels à
construire. (Attention au piège du §9 : pas de `SecuredUseCase` sur un thread de fond.)

### Pistes propres au domaine

- **Conversion par source.** Le tunnel global est utile, mais l'information qui décide vraiment,
  c'est *quelle plateforme convertit*. 30 candidatures LinkedIn pour 0 réponse contre 5 candidatures
  en direct pour 2 entretiens change la façon de chercher. Le schéma le permet déjà :
  `Application` → `JobOffer` → `source`.
- **Conversion par technologie.** Même calcul via `JOB_OFFER_TECHNOLOGY` : quelles stacks
  répondent. Utile pour arbitrer ce qu'on met en avant.
- **Délai de réponse observé.** Moyenne entre `applied_at` et le premier changement de statut, par
  source et par entreprise. C'est ce qui permet de calibrer le J+7 de la relance plutôt que de le
  fixer au hasard.
- **Écart salarial.** `expected_salary` / `proposed_salary` face à `salary_min`/`salary_max` de
  l'offre : mesure si les prétentions sont systématiquement au-dessus ou en dessous du marché
  observé, sur données réelles.
- **Offre expirée ou republiée.** Re-visiter les URL et marquer les offres mortes. Une annonce
  republiée plusieurs fois est un signal en soi — le poste ne se pourvoit pas.
- **Détection des doublons de candidature.** Empêcher de postuler deux fois à la même entreprise
  sans le savoir : contrainte applicative sur `(user_id, job_offer_id)`, et alerte sur
  `(user_id, company_id)` récent.
- **Import initial.** Une reprise CSV pour ne pas repartir de zéro si un tableur existe déjà.

### À écarter

- **Génération de lettre de motivation par LLM.** Techniquement simple — le module `Mail` et un
  appel API suffisent — mais c'est un module à part entière, sans rapport avec le suivi. À ne pas
  glisser dans la V1.
- **Score ATS CV / offre.** Suppose de modéliser le CV, donc un second domaine. Intéressant, mais
  c'est un projet, pas une fonctionnalité.

---

## 14. Conventions de travail sur ce module

Le code de ce module est écrit à la main, pas généré d'un bloc. L'IA assiste, elle ne livre pas le
module clé en main.

Quand du code est proposé :

- expliquer brièvement les concepts C# / ASP.NET spécifiques en jeu ;
- ne pas cacher la logique derrière des abstractions inutiles ;
- avancer par étapes ;
- privilégier un code simple et idiomatique.

Comprendre l'intention architecturale (DDD, DIP, DI, Clean Architecture, repositories) ne veut pas
dire connaître par cœur la syntaxe ou les détails d'ASP.NET Core : les deux se traitent séparément.
