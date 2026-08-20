# Paramètres

Des paramètres persistants, hérités du site vers la personne, et réglables depuis l'interface.

## Le partage : le catalogue en code, les valeurs en base

| | vit où | change comment |
|---|---|---|
| **le catalogue** — quels paramètres existent, de quel type, avec quelles bornes | `SettingCatalog` (C#) | commit + déploiement |
| **les valeurs** — ce que quelqu'un a posé | `tools_core.setting_value` | à chaud |

Conséquence directe et recherchée : **ajouter un paramètre est un commit, jamais une migration.**

La raison de fond est qu'un paramètre n'existe que parce qu'un bout de code le lit. Le créer
depuis une interface donnerait une ligne que rien ne consomme ; le supprimer laisserait le code
qui le lit retomber en silence sur son défaut. En code, supprimer une définition **casse la
compilation** à l'endroit qui s'en servait.

Autre conséquence : **le module ne se stocke pas**. `dofus.autoSync` appartient à Dofus, mais
c'est une propriété du paramètre, pas de la valeur. La table ne connaît que des codes.

`tools_core.config` et `tools_core.user_config_override`, qui visaient la même intention avec un
catalogue en base, ont été supprimées par `V2.69.0` sans avoir jamais été lues.

## Les trois accroches

Une valeur est toujours accrochée à quelque chose :

| accroche | exemple |
|---|---|
| `Global` | « par défaut, tout le monde est en thème sombre » |
| `Role` | « les modérateurs sont en thème clair » |
| `User` | « moi, je suis en clair » |

**La plus précise gagne** : `User > Role > Global > défaut du catalogue`. Les valeurs de
l'énumération `SettingScope` portent cet ordre (1, 2, 3), la résolution compare donc des entiers
— même principe que `RoleCode`, dont la valeur porte le niveau hiérarchique.

Une définition déclare les accroches qu'elle accepte. Un paramètre d'instance ne déclare que
`Global` : « mon mode maintenance à moi » n'a aucun sens, et l'exprimer par un rôle très élevé
serait faux — ce n'est pas une question de droit.

L'unicité porte sur l'accroche précise, pas sur le paramètre : une ligne `GLOBAL` par code, une
par couple (code, rôle), une par couple (code, utilisateur). Ce sont les trois index uniques
partiels de `V2.70.0`. Ils sont partiels parce qu'une contrainte `UNIQUE` classique ne
protégerait rien : deux lignes `GLOBAL` du même code ont `role_code` et `user_id` à `NULL`, et
`NULL` n'entre pas en collision avec `NULL` en PostgreSQL.

## Le verrou renverse la priorité

`is_locked` sur une ligne signifie : *cette valeur s'impose, rien de plus précis ne la remplace*.
Un `ui.theme` verrouillé en `Global` s'applique à tout le monde ; les lignes de rôle et
d'utilisateur restent en base et redeviennent actives au déverrouillage.

D'où la règle : **c'est la valeur verrouillée la plus large qui gagne**
(`locked.MinBy(c => c.Scope)`), et non la plus précise. Sinon un verrou global serait contourné
par un verrou de rôle.

Sur une ligne `User`, où rien n'est plus précis, cela revient à « cette personne ne peut pas
modifier sa propre valeur ».

## Les quatre droits d'une définition

Écraser ces questions en un seul champ ne marche pas : « tout le monde peut choisir son thème »
et « tout le monde peut fixer le thème du site » ne sont pas la même phrase.

| champ | question |
|---|---|
| `AllowedScopes` | ce paramètre a-t-il un sens par personne ? |
| `MinRoleToView` | qui le **voit** ? |
| `MinRoleToSetOwn` | qui pose **sa propre** valeur ? |
| `MinRoleToAdminister` | qui pose la valeur **globale ou par rôle** ? (ADMIN par défaut) |

### `RoleCode` sert à deux choses opposées

C'est le point qui se confond le plus facilement :

- **seuil `>=`** pour une **permission** — `MinRoleToView`, `MinRoleToSetOwn`. Un administrateur
  voit tout ce qu'un modérateur voit.
- **égalité `=`** pour une **cible** — une valeur posée sur `scope = ROLE, role_code = MODERATOR`
  s'applique aux modérateurs et à personne d'autre. Pas de cascade vers le haut.

L'un dit « qui a le droit », l'autre « à qui ça s'adresse ».

Pour un paramètre portant un `Module`, tous ces seuils se comparent au rôle **dans ce module**
(`SettingAudience.RoleFor`), jamais au rôle global — la règle de `UseCaseAuthorizer`, reprise à
l'identique. Un administrateur du site absent de Dofus ne voit pas les paramètres Dofus.

## Le code d'un paramètre est un nom, pas un lien

Par convention `<namespace>.<nom>`, le namespace valant le code du module pour un paramètre de
module (`dofus.autoSync`) et un domaine fonctionnel sinon (`ui.theme`). Le préfixe existe pour
garantir l'unicité : sans lui, Dofus et Palworld ne pourraient pas avoir chacun un `autoSync`.

**La correspondance avec `Module` n'est pas vérifiée, et c'est délibéré.** L'imposer
transformerait le renommage d'un `ModuleCode` — déjà lourd : deux énumérations, la colonne
partagée `tools_core.module.code`, les noms de routes du frontend — en travail sur les
paramètres. Avec la correspondance libre, `elite_dangerous` → `elite` ne change qu'un champ
`Module` dans le catalogue : zéro `PreviousCodes`, zéro SQL, zéro ligne touchée en base.

`PreviousCodes` reste, pour ce à quoi il sert vraiment : renommer **un paramètre**.

## `JSONB` et pas `TEXT`

C'est la sélection multiple qui tranche : sa valeur est un tableau. En texte il faudrait un
séparateur, donc un encodage maison, qui casse le jour où une option le contient. En JSON, un
booléen est `true`, un entier `42`, une multi-sélection `["a","b"]`.

## Lire un paramètre depuis un use case

```csharp
public sealed class SomeUseCase(UseCaseAuthorizer authorizer, SettingReader settings)
    : SecuredUseCase(authorizer)
{
    public async Task Execute()
    {
        bool compact = await settings.Get(SettingCatalog.Ui.CompactMode);
        long size    = await settings.Get(SettingCatalog.Ui.PageSize);
    }
}
```

**L'appelant ne passe ni identifiant ni rôle.** `ICurrentUserProvider` les connaît déjà, et les
faire circuler à la main est le chemin par lequel la règle finit par différer d'un appelant à
l'autre — l'un oubliant les rôles de module, l'autre comparant le rôle global sur un paramètre
de module.

**Aucune chaîne non plus.** Les définitions sont des champs nommés et typés
(`SettingDefinition<TValue>`) : le type de retour est déduit, aucune faute de frappe n'est
possible, et supprimer une définition casse le build à l'endroit qui la lisait.

| appel | pour qui |
|---|---|
| `Get(def)` | l'appelant courant |
| `GetGlobal(def)` | une **tâche de fond** — aucun utilisateur identifié, seul le global s'applique |
| `GetFor(def, audience)` | quelqu'un d'autre (administration, destinataire d'un mail) |

`GetGlobal` est explicite plutôt que de laisser `Get` retomber en douce sur le global hors
requête HTTP. Appeler `Get` depuis un scheduler lève, avec un message qui renvoie vers
`GetGlobal` — même piège que `SecuredUseCase` construit depuis un cron.

`SettingReader` est **Scoped** et mémorise les lignes de l'appelant : un use case qui lit trois
paramètres ne fait qu'un aller-retour. `Invalidate()` doit être appelé après toute écriture faite
dans la même requête.

## La résolution est une fonction pure

`SettingResolution.Resolve(definition, candidates, audience)` ne fait aucune I/O. Le repository
ramène des lignes, ce fichier décide. Mettre la priorité, le verrou et la validation dans une
requête SQL les aurait rendus vérifiables seulement avec PostgreSQL, et les aurait dupliqués au
premier autre appelant.

Le SQL ramène volontairement **large** : toutes les lignes des rôles que porte l'appelant, sans
savoir lequel s'applique à quel paramètre — une même requête ne peut pas trancher entre rôle
global et rôle de module. La résolution refait le tri exact, c'est elle qui fait foi.

### Trois filtres défensifs à la lecture

Ils existent pour qu'une incohérence n'empêche jamais d'afficher la page de réglages :

- **une ligne qui ne concerne pas ce paramètre** est écartée. Le contrôle est dans `Targets` et
  non chez l'appelant, qui passe volontiers toutes les lignes qu'il a chargées ;
- **une accroche retirée du catalogue** est ignorée — un paramètre d'instance ne doit pas
  pouvoir être détourné par une ligne `USER` posée du temps où elle était permise, ou à la main ;
- **une valeur devenue invalide** après un resserrement de contrainte est écartée au profit de
  l'héritage, plutôt que de faire échouer la lecture.

## Le garde-fou du catalogue

`SettingCatalog` vérifie à l'initialisation, donc au démarrage de l'application :

- pas de code en double, codes historiques compris ;
- pas d'accroche vide ;
- `MinRoleToSetOwn` déclaré si et seulement si l'accroche `User` est autorisée ;
- aucun seuil d'écriture sous le seuil de lecture — on ne règle pas ce qu'on ne voit pas ;
- la valeur par défaut satisfait ses propres contraintes.

`AddSettingsModule` touche `SettingCatalog.All` exprès : une définition incohérente empêche
l'application de démarrer, au lieu d'échouer le jour où quelqu'un ouvre ses réglages. Un test
par réflexion vérifie en plus que toute définition déclarée figure bien dans `All`.

## Ce qui a été écarté

**Le catalogue en base, avec un panel admin qui génère du DML.** Envisagé, puis écarté : le
`Module` deviendrait une chaîne non vérifiée, les contraintes du JSON à interpréter à
l'exécution, et chaque lecture par le code une chaîne libre où une faute de frappe passe. Surtout,
créer ou supprimer un paramètre s'accompagne de toute façon du code qui le lit — le panel
n'économiserait que le cas déjà gratuit.

Reste envisageable, et sans ces défauts : mettre la **présentation** en base (libellé,
description, section, ordre d'affichage), éditable sans déploiement parce qu'elle ne peut rien
casser.

**La cascade hiérarchique sur l'accroche `Role`.** Une valeur posée sur `USER` ne remonte pas
vers `ADMIN`. Ça avait l'air naturel puisque `RoleCode` est ordonné, mais ça réintroduisait un
arbitrage et produisait des surprises — on pose une valeur « pour les utilisateurs », elle
s'applique silencieusement aux administrateurs. Avec l'égalité, ce que montre la table est ce qui
s'applique ; pour viser tout le monde, c'est `Global`.

## État

Fait : le Domain complet, la résolution et ses tests, le port, `SettingReader`, l'adaptateur
PostgreSQL, la composition.

Reste : les use cases d'écriture (poser sa valeur, réinitialiser, administrer), le contrôleur,
les entrées Bruno, et le frontend — où `web/src/modules/Settings/settingsConfigMock.ts` attend
d'être remplacé par un vrai store.
