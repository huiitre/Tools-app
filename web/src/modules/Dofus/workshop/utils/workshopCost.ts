import type { WorkshopItem, WorkshopItemIngredient } from '@/modules/Dofus/workshop/types/workshop.types'

//* Coûts d'un objet d'atelier. Les quantités requises arrivent déjà multipliées sur toute la
//* chaîne (objet → ingrédient → sous-ingrédient) : on ne les multiplie jamais ici.
//*
//* Un ingrédient crafté (qui a des sous-ingrédients) n'est jamais payé lui-même : seules les
//* ressources de base, les feuilles de l'arbre, le sont.

export type PriceOf = (itemId: number) => number

export type CostStrategy = 'buy' | 'craft'

export type MixedChoice = {
  strategy: CostStrategy
  cost: number
}

export type MixedCost = {
  //* Le moins cher en partant de zéro.
  total: MixedChoice
  //* Le moins cher pour ce qu'il reste à payer : c'est lui qui dit quoi faire.
  adjusted: MixedChoice
}

export function baseIngredients(ingredients: WorkshopItemIngredient[]): WorkshopItemIngredient[] {
  const parents = new Set(ingredients.map(ing => ing.parentIngredientId).filter(id => id != null))
  return ingredients.filter(ing => !parents.has(ing.id))
}

export function remainingQuantity(ingredient: WorkshopItemIngredient): number {
  return Math.max(0, ingredient.quantityRequired - ingredient.quantityObtained)
}

//* Terminé quand toutes les ressources de base sont obtenues, y compris celles d'un ingrédient crafté.
//* Sans ingrédient, un objet est considéré comme terminé.
export function isComplete(ingredients: WorkshopItemIngredient[]): boolean {
  return baseIngredients(ingredients).every(ing => ing.quantityObtained >= ing.quantityRequired)
}

export function buyCost(workshopItem: WorkshopItem, priceOf: PriceOf): number {
  return priceOf(workshopItem.item.id) * workshopItem.quantity
}

export function craftCostTotal(ingredients: WorkshopItemIngredient[], priceOf: PriceOf): number {
  return baseIngredients(ingredients)
    .reduce((sum, ing) => sum + priceOf(ing.item.id) * ing.quantityRequired, 0)
}

export function craftCostRemaining(ingredients: WorkshopItemIngredient[], priceOf: PriceOf): number {
  return baseIngredients(ingredients)
    .reduce((sum, ing) => sum + priceOf(ing.item.id) * remainingQuantity(ing), 0)
}

//* Un prix à 0 est un prix inconnu : il ne gagne jamais une comparaison. Ce qui est connu ou non se
//* lit sur le prix (achat) ou sur le coût total (craft), jamais sur un reste à payer, qui peut
//* valoir 0 légitimement (tout est obtenu).
function cheapest(buy: number, buyKnown: boolean, craft: number, craftKnown: boolean): MixedChoice {
  if (!craftKnown) return { strategy: 'buy', cost: buy }
  if (!buyKnown) return { strategy: 'craft', cost: craft }
  return craft < buy ? { strategy: 'craft', cost: craft } : { strategy: 'buy', cost: buy }
}

type BestCost = { total: number; remaining: number }

//* Meilleur coût de chaque ingrédient direct, en choisissant à chaque niveau le moins cher entre
//* l'acheter et le crafter, quand l'utilisateur a prévu de le crafter. Une ressource de base
//* s'achète toujours.
export function bestCraftCost(ingredients: WorkshopItemIngredient[], priceOf: PriceOf): BestCost {
  const childrenByParent = new Map<number, WorkshopItemIngredient[]>()
  for (const ing of ingredients) {
    if (ing.parentIngredientId == null) continue
    const siblings = childrenByParent.get(ing.parentIngredientId) ?? []
    siblings.push(ing)
    childrenByParent.set(ing.parentIngredientId, siblings)
  }

  const best = (ing: WorkshopItemIngredient): BestCost => {
    const price = priceOf(ing.item.id)
    const children = childrenByParent.get(ing.id)
    if (!children) return { total: price * ing.quantityRequired, remaining: price * remainingQuantity(ing) }

    //* Un ingrédient crafté est passé à 1/1 par l'API : ça veut dire « je le crafte », pas « je l'ai ».
    //* L'acheter coûte donc toujours son prix plein ; ce qui reste à payer pour lui, ce sont ses ressources.
    const buy = price * ing.quantityRequired
    const craft = sum(children.map(best))
    const craftKnown = craft.total > 0
    return {
      total: cheapest(buy, price > 0, craft.total, craftKnown).cost,
      remaining: cheapest(buy, price > 0, craft.remaining, craftKnown).cost,
    }
  }

  return sum(ingredients.filter(ing => ing.parentIngredientId == null).map(best))
}

function sum(costs: BestCost[]): BestCost {
  return costs.reduce((acc, c) => ({ total: acc.total + c.total, remaining: acc.remaining + c.remaining }), { total: 0, remaining: 0 })
}

export function mixedCost(workshopItem: WorkshopItem, priceOf: PriceOf): MixedCost {
  const price = priceOf(workshopItem.item.id)
  const buy = price * workshopItem.quantity

  if (!workshopItem.item.hasRecipe) {
    const choice: MixedChoice = { strategy: 'buy', cost: buy }
    return { total: choice, adjusted: choice }
  }

  const craft = bestCraftCost(workshopItem.ingredients, priceOf)
  const craftKnown = craft.total > 0

  return {
    total: cheapest(buy, price > 0, craft.total, craftKnown),
    adjusted: cheapest(buy, price > 0, craft.remaining, craftKnown),
  }
}
