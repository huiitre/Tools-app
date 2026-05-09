// src/modules/Dofus/preferences/dofusPreferences.ts

import { PriceDisplayMode } from '@/modules/Dofus/preferences/types/priceDisplayMode.enum'

/**
 * Définitions des préférences UI du module Dofus.
 * - Décrit quoi afficher / comment éditer
 * - Les valeurs sont stockées ailleurs (store, puis backend plus tard)
 */

export type DofusPreferenceKey =
  | 'priceDisplayMode'
  | 'showItemContextOnHover'

/* ======================
   BASE TYPES
====================== */

export type SelectOption<T extends string> = {
  value: T
  label: string
  help?: string
}

/* ======================
   PREFERENCE DEFINITIONS
====================== */

export type SelectPreferenceDefinition<T extends string> = {
  key: DofusPreferenceKey
  type: 'select'
  label: string
  description?: string
  defaultValue: T
  options: SelectOption<T>[]
}

export type SwitchPreferenceDefinition = {
  key: DofusPreferenceKey
  type: 'switch'
  label: string
  description?: string
  defaultValue: boolean
}

export type DofusPreferenceDefinition =
  | SelectPreferenceDefinition<PriceDisplayMode>
  | SwitchPreferenceDefinition

/* ======================
   DEFINITIONS
====================== */

export const DOFUS_PREFERENCES: DofusPreferenceDefinition[] = [
  {
    key: 'priceDisplayMode',
    type: 'select',
    label: 'Prix affiché en priorité',
    description: 'Définit quel prix est mis en avant.',
    defaultValue: PriceDisplayMode.USER,
    options: [
      {
        value: PriceDisplayMode.USER,
        label: 'Mon prix',
        help: 'Affiche mon prix en priorité.',
      },
      {
        value: PriceDisplayMode.COMMUNITY,
        label: 'Prix moyen communautaire',
        help: 'Affiche la moyenne du prix de tous les utilisateurs en priorité.',
      },
      {
        value: PriceDisplayMode.LAST,
        label: 'Dernier prix enregistré',
        help: 'Affiche le dernier prix enregistré en priorité.',
      },
    ],
  },
  {
    key: 'showItemContextOnHover',
    type: 'switch', 
    label: "Affiche les informations détaillées de l’item au survol",
    description:
      'Affiche une infobulle contextuelle avec les informations complètes de l’item (prix, zones, monstres, etc.) lors du survol de l\'image.',
    defaultValue: true,
  },
]
