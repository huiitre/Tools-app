# User Stories - Workshop Backend

## Tag Workshop
- **Créer un tag** (nom, couleur)
  - `POST /api/v3/dofus/workshops/tags`
  - `CreateWorkshopTagUseCase`

- **Modifier un tag** (nom, couleur)
  - `PATCH /api/v3/dofus/workshops/tags/{tagId}`
  - `UpdateWorkshopTagUseCase`

- **Supprimer un tag** (+ suppression des liaisons)
  - `DELETE /api/v3/dofus/workshops/tags/{tagId}`
  - `DeleteWorkshopTagUseCase`

- **Lister tous les tags de l'utilisateur**
  - `GET /api/v3/dofus/workshops/tags`
  - `ListWorkshopTagsUseCase`

## Workshop
- **Créer un workshop** (nom, game_version_id, tags optionnels, is_active=true par défaut)
  - `POST /api/v3/dofus/workshops`
  - `CreateWorkshopUseCase`

- **Modifier un workshop** (nom, is_active, tags)
  - `PATCH /api/v3/dofus/workshops/{workshopId}`
  - `UpdateWorkshopUseCase`

- **Supprimer un workshop** (+ cascade sur items et ingrédients)
  - `DELETE /api/v3/dofus/workshops/{workshopId}`
  - `DeleteWorkshopUseCase`

- **Lister mes workshops** (filtres : is_active, tag_id, game_version_id)
  - `GET /api/v3/dofus/workshops`
  - `ListWorkshopsUseCase`

- **Récupérer le détail complet d'un workshop** (items, ingrédients, arbre hiérarchique)
  - `GET /api/v3/dofus/workshops/{workshopId}`
  - `GetWorkshopDetailUseCase`

## Items du workshop
- **Rechercher des items craftables** (réutilisation route catalogue avec has_recipe=true)
  - `GET /api/v3/dofus/items?has_recipe=true`
  - _(Utilise le catalogue existant)_

- **Ajouter un item craftable à un workshop** (item_id, quantity)
  - `POST /api/v3/dofus/workshops/{workshopId}/items`
  - `AddItemToWorkshopUseCase`

- **Modifier la quantité d'un item dans le workshop**
  - `PATCH /api/v3/dofus/workshops/{workshopId}/items/{workshopItemId}`
  - `UpdateWorkshopItemQuantityUseCase`

- **Supprimer un item du workshop** (+ cascade sur ses ingrédients)
  - `DELETE /api/v3/dofus/workshops/{workshopId}/items/{workshopItemId}`
  - `DeleteWorkshopItemUseCase`

## Ingrédients d'un item
- **Marquer un ingrédient comme "à crafter"** (crée une nouvelle carte de craft avec parent_ingredient_id)
  - `POST /api/v3/dofus/workshops/{workshopId}/items/{workshopItemId}/ingredients/{ingredientId}/craft`
  - `MarkIngredientAsCraftableUseCase`

- **Modifier la quantité obtenue d'un ingrédient spécifique** (pour un workshop_item donné)
  - `PATCH /api/v3/dofus/workshops/{workshopId}/items/{workshopItemId}/ingredients/{workshopItemIngredientId}`
  - `UpdateIngredientQuantityObtainedUseCase`

- **Cocher/décocher une ressource globalement** (met à jour quantity_obtained proportionnellement partout)
  - `PATCH /api/v3/dofus/workshops/{workshopId}/resources/{itemId}`
  - `UpdateResourceGloballyUseCase`

- **Supprimer une carte de craft d'un ingrédient** (gestion du parent_ingredient_id des enfants)
  - `DELETE /api/v3/dofus/workshops/{workshopId}/items/{workshopItemId}/ingredients/{workshopItemIngredientId}`
  - `DeleteIngredientCraftCardUseCase`

## Agrégation & Zones de farm
- **Récupérer le résumé agrégé d'un workshop** (total items, ressources uniques, coût total, ressources groupées par zone)
  - `GET /api/v3/dofus/workshops/{workshopId}/summary`
  - `GetWorkshopSummaryUseCase`

- **Récupérer les zones de farm pour une ressource** (item_id → areas/subareas/monsters)
  - `GET /api/v3/dofus/items/{itemId}/farm-zones`
  - `GetItemFarmZonesUseCase`