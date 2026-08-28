import type { GameServerMapAdapter } from './mapAdapter'
import { palworldMapAdapter } from '@/modules/Palworld/server/map/palworldMapAdapter'

// Un jeu absent d'ici n'a pas de carte : le dashboard n'en affiche simplement aucune.
//
// Seul endroit du Core qui connaisse un module de jeu : l'adaptateur Palworld a besoin de données
// servies par son propre module. Un import visible ici vaut mieux qu'une dépendance cachée côté
// API, où le Core n'a le droit de dépendre d'aucun module métier.
const ADAPTERS: Record<string, GameServerMapAdapter> = {
  PALWORLD: palworldMapAdapter,
}

export function mapAdapterFor(gameCode: string): GameServerMapAdapter | null {
  return ADAPTERS[gameCode] ?? null
}
