import { Item } from "@/modules/Dofus/item/types/item.types"

export enum WorkshopLinkSource {
  DOFUSBOOK = 'DOFUSBOOK',
  CUSTOM = 'CUSTOM'
}

export type WorkshopLink = {
  id: number
  source: WorkshopLinkSource
  url: string
  label: string
  createdAt: string
}

export type Workshop = {
  id: number
  name: string
  active: boolean
  tags: WorkshopTag[]
  pinned: boolean
  links: WorkshopLink[]
  breeds: WorkshopBreed[]
  jobs: WorkshopJob[]
  //* Objets avec leur arbre d'ingrédients (sans zones de récolte) : chiffres des cartes de la liste.
  items: WorkshopItem[]
}

//* Icônes système : non modifiables par l'utilisateur, URL déjà résolue par l'API.
export type WorkshopBreed = {
  id: number
  name: string
  iconUrl: string | null
}

export type WorkshopJob = {
  id: number
  name: string
  iconUrl: string | null
}

export type WorkshopTag = {
  id: number
  name: string
  color: string
}

export type WorkshopItem = {
  id: number
  workshopId: number
  item: Item
  quantity: number
  ingredients: WorkshopItemIngredient[]
}

export type WorkshopItemIngredient = {
  id: number
  workshopItemId: number
  item: Item
  parentIngredientId?: number
  quantityRequired: number
  quantityObtained: number
}

type ResourcesByZone = {
  name: string
  percent: number
  resources: AggregatedResource[]
}

export type AggregatedResource = {
  item: Item
  quantityRequired: number
  quantityObtained: number
  remainingToBuy: number
  totalCost: number
  usedIn: WorkshopItem[]
}

export interface WorkshopSummary {
  totalItems: number
  totalResources: number
  totalMissing: number
  totalComplete: number
  buyItemsCost: number
  totalCost: number
  craftCost: number
  craftAdjusted: number
  mixedCost: number
  mixedAdjusted: number
  resources: AggregatedResource[]
  zones: {
    name: string
    percent: number
    resources: AggregatedResource[]
  }[],
  buyItemsRemaining: number
  //* Prix ajustés si les ressources prévues étaient complètes ; null sans prévision.
  preview: { buyItemsRemaining: number; craftAdjusted: number; mixedAdjusted: number } | null
}

export type CraftCard = {
  type: 'main' | 'craft'
  workshopItem: WorkshopItem
  craftedIngredient?: WorkshopItemIngredient
  subIngredients?: WorkshopItemIngredient[]
  level?: number
  parentName?: string
}
