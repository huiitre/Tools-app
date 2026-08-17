# Dofus Unity --- Documentation d'intégration des données (Assets)

Ce document décrit la correspondance entre les fichiers JSON extraits
(Doduda / Dofus Unity) et les tables de la base **tools_dofus_unity**.

Il est volontairement évolutif : de nouvelles colonnes, relations et
tables pourront être ajoutées au fil des versions.

------------------------------------------------------------------------

## 1. Items (`tools_dofus_unity.item`)

### Source

-   Fichier JSON : **items.json**
-   Type : `ItemData`

### Mapping principal

  -------------------------------------------------------------------------------------
  Champ DB         Source JSON                 Description
  ---------------- --------------------------- ----------------------------------------
  iditem           data.id                     Identifiant unique de l'objet

  name             data.nameId                 Clé de traduction → `languages/fr.json`

  description      data.descriptionId          Clé de traduction → `languages/fr.json`

  level            data.level                  Niveau de l'objet

  img              data.iconId                 Image :
                                               `./img/item/1x|2x/{iconId}-64|128.png`

  iditem_type      data.typeId                 Référence vers `item_types.json`

  idpanoply        data.itemSetId              `NULL` si `-1`, sinon lien vers panoplie

  weight           data.realWeight             Poids réel

  price            data.price                  Prix de référence

  recipe_slots     data.recipeSlots            Nombre d'emplacements de recette
  -------------------------------------------------------------------------------------

### Traductions

Tous les champs textuels (`name`, `description`, etc.) sont résolus via
: - `languages/fr.json` - Clé = `nameId` ou `descriptionId`

> Le support multi-langue pourra être ajouté ultérieurement.

------------------------------------------------------------------------

## 2. Types d'objets (`tools_dofus_unity.item_type`)

### Source

-   Fichier JSON : **item_types.json**
-   Type : `ItemTypeData`

### Mapping

  -------------------------------------------------------------------------
  Champ DB          Source JSON                 Description
  ----------------- --------------------------- ---------------------------
  iditem_type       data.id                     Identifiant du type

  name              data.nameId                 Clé de traduction
                                                (`fr.json`)

  idcategory        data.categoryId             Catégorie (voir remarque
                                                ci-dessous)

  super_type        data.superTypeId            Super-type

  is_encyclopedia   data.isInEncyclopedia       Visible dans l'encyclopédie
  -------------------------------------------------------------------------

### Remarque importante --- Catégories

À ce stade, aucun fichier JSON listant explicitement les catégories
n'est présent dans les assets. La table `category` ne peut donc pas être
alimentée correctement pour l'instant.

------------------------------------------------------------------------

## 3. Recettes (`tools_dofus_unity.recipe`)

### Source

-   Fichier JSON : **recipes.json**
-   Type : `RecipeData`

### Principe

Une recette génère **plusieurs lignes** en base : une par ingrédient.

### Mapping

  Champ DB   Source JSON                     Description
  ---------- ------------------------------- -----------------------
  idrecipe   auto                            Identifiant technique
  idparent   data.resultId                   Objet résultant
  idenfant   data.ingredientIds.Array\[n\]   Ingrédient
  quantity   data.quantities.Array\[n\]      Quantité requise
  job_id     data.jobId                      Métier
  skill_id   data.skillId                    Compétence

### Exemple

Pour une recette avec 4 ingrédients : - 4 lignes sont insérées - même
`idparent` - `idenfant` et `quantity` varient

------------------------------------------------------------------------

## 4. Images

-   Dossier : `./img/item/`
-   Résolutions :
    -   `1x` → `64px`
    -   `2x` → `128px`
-   Nom de fichier :
    -   `{iconId}-64.png`
    -   `{iconId}-128.png`

------------------------------------------------------------------------

## 5. Évolutions futures prévues

-   Support multi-langue
-   Panoplies (`item_sets.json`)
-   Effets (`possibleEffects`)
-   Drops monstres
-   Catégories normalisées
-   Historisation des versions

------------------------------------------------------------------------

**Document vivant --- à enrichir au fil des intégrations.**
