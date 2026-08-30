# Temtem

Module né directement en C# le 30/08/2026 — il n'a jamais existé côté Java. Seul son schéma
`tools_temtem` avait été créé en V2.24.0 puis laissé vide pendant deux ans.

## Où on en est (30/08/2026)

| Chantier | État |
|---|---|
| Migration `V2.71.0__temtem_catalogue.sql` | **appliquée** (à la main, hors Flyway) |
| Sync `POST /internal/temtem/sync` | **livrée et vérifiée en base** |
| Domaine `TypeEffectiveness` + 8 tests unitaires | **livré** |
| Migration `V2.72.0__temtem_team.sql` | **appliquée** (à la main, hors Flyway) |
| Migration `V2.73.0__temtem_team_member_slot_order.sql` | à appliquer (réordonnancement des équipes) |
| Catalogue en lecture (API) | **livré et vérifié contre la base** |
| Sous-module `Teams/` (API) | **livré et vérifié contre la base** |
| Front : Temtemdex | **livré** |
| Front : Mes équipes | **livré** |
| Front : Simulateur de combat | **livré** (sélection éphémère, sans localStorage) |
| Choix du trait d'un membre d'équipe | à faire (demande une migration) |

## L'objectif, tel que décrit par l'utilisateur

Trois écrans, dans cet ordre :

1. **Catalogue** — tous les Temtem, calqué sur la page Paldex de Palworld (grille de cartes,
   barre de recherche, tri, filtres). Chaque carte porte un petit bouton en haut à droite, comme
   les cartes de skins Valorant, qui ouvre une popup « ajouter à une équipe » : la popup liste les
   équipes existantes et offre un bouton « créer une équipe », lequel crée l'équipe **et** y place
   le Temtem dans la foulée.
2. **Mes équipes** — toutes les équipes, chacune avec ses Temtem. Pour chaque membre on choisit
   **4 techniques** parmi celles qu'il apprend, et plus tard un trait parmi ceux disponibles. Une
   barre de recherche permet d'ajouter un Temtem à une équipe depuis cette page.
3. **Simulateur de combat** — on sélectionne une équipe puis deux Temtem adverses. L'application
   met en évidence les forces et faiblesses de types ; les recommandations de techniques viendront
   dans un second temps.

## La règle du double type

Le multiplicateur d'une technique contre un Temtem à deux types est le **produit** des deux
multiplicateurs simple-type :

```
mult = mult(type technique → type1) × mult(type technique → type2)
```

Vérifié le 29/08/2026 contre l'endpoint du site source (`mode=double` sur Feu×Eau) : le résultat
correspond au produit des deux résultats `mode=simple`, valeur par valeur sur les 12 types. C'est
pourquoi **seule la matrice simple-type est stockée** (`type_matrix`, 144 lignes) et qu'il n'y a
aucune table de doubles types.

Le calcul vit dans `Modules/Temtem/Types/Domain/TypeEffectiveness.cs`, sans aucune dépendance, et
il est couvert par `tests/Tools.Api.UnitTests/Modules/Temtem/TypeEffectivenessTests.cs`. Un couple
absent de la matrice y lève une erreur au lieu de valoir « neutre » : la matrice est pleine, une
absence est une donnée manquante.

## Architecture visée

Le découpage suit **Dofus** : un sous-module par entité, jamais par écran. Chez Dofus il y a
`item/`, `itemtype/`, `monster/`, `recipe/`… et un `catalogue/` qui les compose pour la page.

```
Modules/Temtem/
├── Types/        matrice d'efficacité + domaine        (livré)
├── Creatures/    le Temtem : entité, vues, lecture     (livré)
├── Techniques/   technique + ciblage                   (vues seules, portées par Creatures)
├── Traits/                                             (vues seules, portées par Creatures)
├── Teams/        équipes de l'utilisateur              (livré)
└── Sync/         rechargement du catalogue             (livré)

`Techniques/` et `Traits/` n'ont ni port ni contrôleur : rien ne demande la liste des 317
techniques hors du contexte d'un Temtem. Ils n'existent que pour que leurs vues vivent chez
l'entité qu'elles décrivent, et non chez celle qui les consomme.
```

Deux vues réutilisées partout, sur le modèle `ItemDto` / `ItemLightDTO` :

- **`TemtemSummaryView`** — id, slug, nom, types, statistiques, image. La carte du catalogue et la
  vignette d'équipe. Les 165 tiennent en un appel.
- **`TemtemDetailView`** — la précédente, plus les techniques (avec source et niveau) et les
  traits. Nécessaire pour composer une équipe et pour le simulateur.

**Ne pas créer de vue par page.** Le catalogue, « Mes équipes » et le simulateur consomment les
mêmes. C'est la consigne explicite de l'utilisateur.

## Les routes de lecture

Trois routes, toutes `SecuredUseCase` en `READ_ONLY` **dans le module Temtem** — un OWNER global
à qui le module n'est pas ouvert reçoit 403 `NO_MODULE_ACCESS`.

| Route | Rend |
|---|---|
| `GET /temtem/types` | les 12 types, triés par nom français |
| `GET /temtem/creatures` | les 165 Temtem en `TemtemSummaryView`, par numéro de Temtemdex |
| `GET /temtem/creatures/{slug}` | `TemtemDetailView` ; 404 `TEMTEM_NOT_FOUND` sur un slug inconnu |

Le segment `creatures` n'est pas décoratif : `/temtem/{slug}` serait entré en collision avec
`/temtem/types` le jour où un Temtem s'appellerait ainsi.

**Le catalogue part entier, sans pagination ni filtre serveur** — 165 lignes, environ 70 Ko.
Recherche, tri et filtres sont l'affaire de la grille, côté navigateur, comme sur la Paldex.

La matrice d'efficacité est exposée par `GET /temtem/types/effectiveness` : 144 lignes, chargées
une seule fois à l'entrée du module. Le simulateur applique côté front le produit des deux lignes
pour un Temtem à double type ; il ne recopie donc jamais la matrice synchronisée en TypeScript.

La fiche coûte trois requêtes — le Temtem, ses techniques, ses traits — et non une par technique ;
les cibles sont agrégées dans la requête des techniques par un `array_agg`.

Vérification faite le 30/08/2026 : les **165 fiches** appelées en HTTP répondent 200, la liste
rend bien 86 doubles types et 79 types uniques, et les refus 401 / 403 / 404 sont ceux attendus.
Les tests d'intégration (`TemtemCatalogueTests`) couvrent le routage et les rôles sur des
adaptateurs en mémoire : **ils ne voient aucune ligne de SQL**, d'où la vérification en base.

## Les équipes

La seule partie du module qui écrit, et la seule en `RoleCode.User` — le catalogue se contente de
`ReadOnly`. Huit routes sous `/temtem/teams`, toutes filtrées sur le propriétaire **dans le use
case et dans le SQL** : l'équipe d'un autre rend **404 et non 403**, confirmer son existence
renseignerait déjà l'intrus.

| Route | Effet |
|---|---|
| `GET /temtem/teams` | mes équipes, membres et techniques retenues compris |
| `POST /temtem/teams` | crée l'équipe ; `temtemId` facultatif place un premier membre |
| `PATCH /temtem/teams/{teamId}` | renomme |
| `DELETE /temtem/teams/{teamId}` | supprime (204) ; membres et techniques partent en cascade |
| `POST /temtem/teams/{teamId}/members` | place un Temtem à une place précise ou à la première libre |
| `DELETE /temtem/teams/{teamId}/members/{memberId}` | retire un membre |
| `PUT /temtem/teams/{teamId}/members/{memberId}/techniques` | remplace les techniques retenues |
| `PUT /temtem/teams/{teamId}/members/order` | remplace l'ordre complet des membres |

**Chaque écriture rend l'équipe entière**, pas un accusé de réception : le front affiche une
équipe complète après chaque geste, sans la recharger ni deviner l'état obtenu. Seule la
suppression rend 204.

`temtemId` à la création n'est pas une commodité : c'est le bouton « créer une équipe » de la
popup du catalogue, qui crée et place d'un seul geste. Les deux écritures tiennent dans la même
transaction — une équipe vide restée derrière un ajout raté serait un déchet que personne ne
nettoierait.

### Ce qui ne peut pas vivre en SQL

`Teams/Domain/TeamRoster.cs` porte les deux règles que la base ne sait pas exprimer, et rien de
plus :

- **La première place libre**, et non « la suivante ». Le `CHECK (slot BETWEEN 1 AND 6)` et
  l'unicité `(team_id, slot)` bornent bien l'équipe à six, mais ne désignent pas la place à
  attribuer. Un membre retiré au milieu laisse un trou que le prochain ajout rebouche : sans
  cette règle, une équipe de trois pourrait se retrouver « pleine ».
- **Quatre techniques par membre** — un CHECK ne compte pas de lignes.

Une troisième règle reste au use case parce qu'elle interroge une autre table : le Temtem doit
réellement apprendre la technique choisie, ce que seule `temtem_technique` dit. D'où
`ITemtemCreatureRepository.FindLearnedTechniqueIds` — le catalogue répond aux questions sur le
jeu, y compris celles que se posent les équipes.

Le même Temtem peut occuper deux places : le jeu l'autorise, et l'interdire reviendrait à décider
à la place du joueur.

Le réordonnancement reçoit la liste complète des `memberId` après un drag-and-drop et réécrit
uniquement les `slot`. La contrainte unique `(team_id, slot)` est différable depuis `V2.73.0` :
l'échange de deux places reste atomique, sans supprimer le membre ni les techniques qui lui sont
liées.

### Le SQL n'est pas dupliqué non plus

`TemtemCreatureSql` et `TemtemTechniqueSql` (dans les `Infrastructure/` de `Creatures/` et
`Techniques/`) portent les colonnes, les jointures, la ligne Dapper et la projection d'un résumé
et d'une technique. Le catalogue et les équipes les incorporent au lieu de recopier vingt lignes
de `SELECT` — la consigne « ne pas dupliquer les DTO » vaut aussi pour les requêtes qui les
remplissent. Les alias `t`/`t1`/`t2` et `tec`/`ty`/`c`/`p` leur sont réservés.

Une équipe se lit en trois requêtes — les équipes, les membres, les techniques retenues — et non
une par membre. Une seule requête à plat multiplierait chaque membre par ses techniques.

Vérification faite le 30/08/2026, cycle complet contre la base réelle : création depuis la popup,
remplissage jusqu'à six puis 409 `TEAM_FULL`, retrait en place 3 et rebouchage de cette place-là,
409 sur nom déjà pris (casse et espaces ignorés), 400 sur cinq techniques et sur une technique non
apprise, dédoublonnage, 404 sur l'équipe d'un autre, `updated_at` touché par une modification de
composition, et cascade vérifiée à la suppression (0/0/0). Toutes les lignes de test ont été
supprimées.

## La synchronisation

`POST /internal/temtem/sync` — **route interne**, authentifiée par le secret partagé
`X-Internal-Token`, contrairement aux autres extracteurs qui se connectent avec un compte TECH.
Un extracteur n'agit au nom d'aucun utilisateur. `SyncTemtemCatalogueUseCase` n'est donc pas un
`SecuredUseCase`.

L'extracteur (`/data/docker/tools/tools_temtem_extractor/update_temtem.sh` sur le NAS) tourne
toutes les heures, ne scrape que si une mise à jour du jeu est détectée, puis appellera cette
route — **le bloc d'appel est encore un TODO dans le script**.

Trois points de conception :

- **Un fichier source vide interrompt tout, avant l'ouverture de la transaction.** Le catalogue se
  recharge par upsert puis suppression de ce qui a disparu de la source : une extraction ratée qui
  publierait un tableau vide viderait la table et emporterait le reste par cascade.
- **L'upsert distingue créé, modifié et inchangé** — `ON CONFLICT DO UPDATE ... WHERE la ligne
  IS DISTINCT FROM excluded` puis `RETURNING (xmax = 0)`. Une synchronisation horaire sans patch
  rend `0/0/0`, pas un faux « tout réécrit ».
- **Les URL d'images se construisent depuis le slug ou le `filename`**, jamais depuis le champ
  `image` des JSON qui porte le chemin du site source (`/img/temtemdex/...`). Attention :
  `STATUS` donne `statut.png`, et les priorités s'appellent `hight.png` et `veryhight.png` (sic).

Vérification faite le 30/08/2026 : comparaison JSON ↔ base table par table et champ par champ,
10/10 identiques (4 226 lignes), et les 186 URL d'images répondent 200.

## Le schéma

`V2.71.0` (appliquée) aligne le catalogue sur l'extracteur : référentiels `category` et
`priority`, colonnes `slug`/`image_url`/statistiques, tables `technique_target`, `trait`,
`temtem_trait` et `type_matrix`. Elle corrige aussi quatre défauts de V2.24.0 — dont une clé
primaire que les données violaient (un couple `(temtem, technique)` apparaît deux fois quand la
technique s'apprend par deux moyens) et un `damage NOT NULL DEFAULT 0` qui aurait transformé
100 techniques sans dégâts en techniques à 0 dégât.

`charge_turns` est le nombre de tours de **chargement avant de pouvoir utiliser** la technique —
pas un temps de recharge après usage. Le champ s'appelait `targets` dans les extracts antérieurs
au 30/08/2026, ce qui était trompeur.

`V2.72.0` (appliquée) ajoute les équipes : `team`, `team_member`,
`team_member_technique`. La colonne `slot` (1 à 6) borne l'équipe à six sans qu'aucun code n'ait à
compter de lignes. Deux règles restent au use case, faute de pouvoir vivre en SQL : le maximum de
quatre techniques par membre, et le fait que le Temtem apprenne réellement la technique choisie —
c'est `temtem_technique` qui le dit.

## Le front

`web/src/modules/Temtem/`, client `clientCore`. Deux écrans livrés le 30/08/2026, détaillés dans
**`web/AGENTS.md`** (section « Module Temtem ») — arborescence, stores, pièges.

| Écran | Route | Ce qu'il consomme |
|---|---|---|
| Temtemdex | `/temtem/temtemdex` | `GET /temtem/creatures`, `GET /temtem/types`, `GET /temtem/types/effectiveness` |
| Mes équipes | `/temtem/teams` | les huit routes `/temtem/teams`, plus `GET /temtem/creatures/{slug}` |
| Simulateur | `/temtem/simulator` | données déjà chargées à l'entrée du module |

Le nom de la route racine (`temtem`) doit valoir le code du module en base : `BurgerNav.vue`
teste `router.hasRoute(module.code)` pour afficher l'entrée de menu.

Deux choses à ne pas défaire :

- **Les vues sont partagées, pas dupliquées.** `TemtemSummary` sert à la carte du catalogue, à la
  vignette d'équipe et demain au simulateur ; l'infobulle vit dans `shared/components/` pour la
  même raison. C'est la consigne explicite de l'utilisateur, et elle vaut aussi côté API.
- **Le catalogue est chargé une fois** à l'entrée sur `/temtem` et gardé en mémoire, comme le
  Paldex. La recherche, le tri et les filtres sont locaux : le serveur ne pagine ni ne filtre.

## Évolution possible : recommandations de techniques

Le premier simulateur se limite aux avantages de type. Il pourra ensuite recommander les
techniques retenues par chaque membre, sans changer le contrat de la matrice.

## Ce qu'il ne faut pas faire

- Ne pas modéliser `version.json` : il appartient à l'extracteur.
- Ne pas reprendre `nameEn` (décision de l'utilisateur : « on s'en fout de l'anglais »).
- Ne pas reprendre `mandatory` de `technique_target.json` : ce champ doit disparaître de la source.
- Ne pas recopier la matrice d'efficacité en TypeScript : elle vient de l'API synchronisée.
- Ne pas recharger la liste des équipes après une écriture : l'API rend l'équipe entière, le
  store la substitue.

## Deux pièges qui coûtent du temps

- **`dotnet watch` ne sait pas ajouter un enregistrement DI à chaud.** Après avoir câblé un use
  case dans `TemtemModule.cs`, la route rend 500 `No service for type …UseCase` jusqu'au
  redémarrage du process (`curl -X POST localhost:4488/restart/api`). Le symptôme ressemble à un
  bug de code et n'en est pas — même piège qu'un conteneur QA non relancé.
- **`vue-tsc` ne voit pas les erreurs de template.** Un `v-bind` sans expression passe le
  typecheck et casse le rendu. Demander le fichier au serveur Vite et chercher `ErrorOverlay`
  dans la réponse les attrape.
