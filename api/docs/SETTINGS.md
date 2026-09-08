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
`Global` : « mon inscription ouverte à moi » n'a aucun sens, et l'exprimer par un rôle très élevé
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

## Les droits d'une définition

| champ | question |
|---|---|
| `Module` | à qui le paramètre descend-il ? |
| `AllowedScopes` | ce paramètre a-t-il un sens par personne ? |
| `MinRole` | qui pose **sa propre** valeur ? |
| `MinRoleToAdminister` | qui pose la valeur **globale ou par rôle** ? (ADMIN par défaut) |

### Il n'y a pas de seuil de visibilité

**Tout le monde reçoit la valeur de tous les paramètres de son périmètre**, y compris ceux qu'il
n'a pas le droit de modifier. Un READ_ONLY de Palworld doit rafraîchir son tableau de bord à
l'intervalle réglé : il lui faut donc la valeur. Un seuil de lecture rendrait le paramètre
invisible de ceux qu'il gouverne — c'est le contraire du but.

Seul `Module` filtre : un paramètre de module ne descend qu'à ses membres, un paramètre
transverse descend à tout le monde.

`MinRole` ne décide donc que de l'**écriture de sa propre valeur**. En dessous du seuil, on
reçoit la valeur et on la subit : celle de son rôle, celle du site, ou le défaut du catalogue.

Un troisième seuil, `MinRoleToSetOwn`, a existé jusqu'au 08/09/2026 en plus d'un
`MinRoleToView`. Il était redondant : `AllowedScopes` dit déjà si un paramètre a un sens par
personne, et le seul cas que les deux champs séparés couvraient — *sur un paramètre à accroche
User, certains rôles règlent le leur et d'autres regardent seulement* — ne valait pas un champ
dans le modèle.

### `MinRoleToAdminister` n'est presque jamais renseigné

`Admin` par défaut, et c'est ce qui s'applique partout. Un administrateur fait tout : la valeur
globale, celle d'un rôle, celle d'un utilisateur donné, et la suppression de l'une d'elles. Le
champ n'existe que pour descendre le seuil sur un paramètre précis — `Moderator` sur un réglage
de modération, par exemple.

Le frontend n'a besoin d'aucun booléen calculé pour savoir ce qu'il propose : `minRole`,
`module` et `minRoleToAdminister` voyagent avec la définition, et `auth.store.hasModuleAccess`
fait déjà la comparaison au bon rôle — celui du module pour un paramètre de module, le rôle
global sinon.

### `RoleCode` sert à deux choses opposées

C'est le point qui se confond le plus facilement :

- **seuil `>=`** pour une **permission** — `MinRole`, `MinRoleToAdminister`. Un administrateur
  peut tout ce qu'un modérateur peut.
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
- `MinRoleToAdminister` au moins égal à `MinRole` — fixer la valeur du site sans pouvoir régler
  la sienne n'a pas de sens ;
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

**La présentation en base** (libellé, description, section), un temps envisagée parce qu'elle ne
peut rien casser. Tranchée le 08/09/2026 : elle vivra dans le catalogue, en C#, avec le reste de
la définition. Le site est monolingue, ces textes ne bougeront pas souvent, et un `Label` requis
rend l'oubli impossible — c'est exactement l'argument qui avait mis le catalogue en code. Une
table de libellés côté frontend aurait ouvert l'i18n, mais au prix d'une seconde source à tenir,
où un paramètre ajouté côté API s'afficherait sous son code brut sans que rien ne le signale.

**La cascade hiérarchique sur l'accroche `Role`.** Une valeur posée sur `USER` ne remonte pas
vers `ADMIN`. Ça avait l'air naturel puisque `RoleCode` est ordonné, mais ça réintroduisait un
arbitrage et produisait des surprises — on pose une valeur « pour les utilisateurs », elle
s'applique silencieusement aux administrateurs. Avec l'égalité, ce que montre la table est ce qui
s'applique ; pour viser tout le monde, c'est `Global`.

## Comment ça se consomme — décidé le 08/09/2026

### Deux lectures, parce que ce sont deux questions

C'est le piège principal, et il ne se voit qu'une fois les écrans dessinés.

| route | question | pour qui |
|---|---|---|
| `GET /settings` | qu'est-ce qui s'applique **à moi** ? | le store au démarrage, la page Settings |
| une route d'administration | quelles valeurs sont **posées**, et à quelle accroche ? | la page Admin |

`ResolveVisible` répond à la première : pour chaque paramètre visible, **une seule** valeur, celle
qui gagne après résolution, plus son origine et les droits associés.

Elle ne peut pas répondre à la seconde. Un administrateur qui a posé `ui.theme = light` pour
lui-même reçoit `light` avec `Source = User` ; la valeur globale, `dark`, a perdu la résolution et
n'apparaît nulle part. L'écran qui règle le thème **du site** doit pourtant afficher `dark`. La
route d'administration rend donc les lignes de `setting_value` telles quelles — la ligne `GLOBAL`,
celles par rôle, avec leur verrou — sans aucune résolution. On y regarde la table, pas ce qu'on
subit.

### Une seule route de lecture pour tout le monde

`GET /settings` n'a pas de variante « admin ». `ResolveVisible(audience)` rend tout ce qui
concerne l'appelant, sans seuil de rôle : seuls les paramètres des modules auxquels il n'a pas
accès sont absents de la réponse. Ce qui distingue les deux écrans n'est pas ce qu'ils reçoivent,
mais ce qu'ils proposent d'éditer.

### Tout est chargé au démarrage

Le frontend charge l'intégralité de ce qu'il a le droit de voir en une fois, au lancement, et le
garde dans un store — même les paramètres qu'il ne peut pas modifier, puisqu'ils pilotent
l'interface. C'est la façon de faire d'EasyWeb et EasyMobile, et le volume la justifie : quelques
dizaines de paramètres avec leur type et leurs bornes tiennent en quelques kilo-octets.

Un appel séparé plutôt qu'un ajout à `/users/me`, bien que les deux partent ensemble au
démarrage : après chaque écriture il faut recharger les paramètres **seuls**, sans refaire un
profil complet.

### Deux écrans, pour deux actions — pas pour deux populations

| écran | ce qu'il montre | droit |
|---|---|---|
| page Settings | les paramètres réglables pour soi | `CanSetOwn` |
| page Admin | la valeur globale et les valeurs par rôle | `MinRoleToAdminister` |

Un administrateur utilise **les deux**, et `ui.theme` apparaît aux deux endroits sans que ce soit
un doublon : dans Settings il choisit le sien, dans Admin il fixe celui du site. Ce sont deux
valeurs distinctes, à deux accroches différentes.

### La page Settings n'affiche pas tout ce qu'elle reçoit

Le store porte la valeur de **tous** les paramètres, parce que l'interface s'en sert. L'écran de
réglages, lui, ne montre que ceux dont l'utilisateur peut poser sa propre valeur : accroche
`User` autorisée et `MinRole` atteint dans le périmètre du paramètre. Le réglage Palworld
n'apparaît donc pas chez un READ_ONLY du module, alors que sa valeur est bien descendue et bien
appliquée.

## État

Fait : le Domain complet, la résolution et ses tests, le port, `SettingReader`, l'adaptateur
PostgreSQL, la composition. Les deux paramètres d'inscription (`auth.registrationEnabled`,
`auth.adminApprovalRequired`) sont déclarés **et lus** — voir `REGISTRATION.md`. Ce sont les
premiers paramètres réellement consommés par du code.

`instance.maintenanceMode` a été supprimé le 08/09/2026 sans avoir jamais été lu : un mode
maintenance doit être visible d'un visiteur anonyme, que la résolution ne sait pas servir, et
couper le conteneur rend de toute façon tout paramètre applicatif inopérant.

Manquent dans le Domain, à faire avant les écrans :

- **`Label` (requis), `Description`, `Section`** sur `SettingDefinition`. Rien ne porte de texte
  aujourd'hui : un écran de réglages afficherait `ui.compactMode`. L'ordre d'affichage suit celui
  de `SettingCatalog.All`, qui est déjà une liste tenue à la main — pas de champ supplémentaire.
Manquent ensuite : les use cases d'écriture (poser sa valeur, réinitialiser, administrer une
valeur globale ou de rôle), les deux routes de lecture, le contrôleur, les entrées Bruno, et le
frontend — dont `web/src/modules/Settings/settingsConfigMock.ts`, maquette jamais branchée,
a été supprimé le 26/08/2026 ; un vrai store reste à écrire.

## Reporté

**Le cas anonyme.** Un visiteur sur la page de login ne peut pas savoir que les inscriptions sont
fermées : `auth.registrationEnabled` ne descend qu'aux comptes connectés, et il n'en est pas un. Le formulaire s'affiche donc toujours et n'échoue
qu'à la soumission — `403 REGISTRATION_CLOSED`, ou la redirection `?error=REGISTRATION_CLOSED`
côté Google. Assumé pour l'instant : le refus est explicite et le message correct. Le jour où on
voudra masquer le formulaire, il faudra une petite route publique : il n'y a rien à abaisser,
un visiteur n'a pas de session du tout.

**Un générateur de définition dans l'administration.** Un formulaire qui produit la ligne C# à
coller dans `SettingCatalog`, en grisant les combinaisons que le garde-fou refuse. Purement
frontend, aucun backend. Utile parce qu'il déplace ces erreurs du démarrage de l'application vers
la saisie — mais il ne génère que la déclaration, jamais le `settings.Get(...)` qui donne son
existence au paramètre.
