const LANDSCAPE_MIN_X = -1099400
const LANDSCAPE_MIN_Y = -724400
const LANDSCAPE_MAX_X = 349400
const LANDSCAPE_MAX_Y = 724400

const WORLD_TREE_MIN_X = 347351.5
const WORLD_TREE_MIN_Y = -818197
const WORLD_TREE_MAX_X = 689148.5
const WORLD_TREE_MAX_Y = -476400

export type PalworldMapId = 'palpagos' | 'worldTree'

export interface MapPixelPosition {
  xPercent: number
  yPercent: number
}

export interface PalworldMapResolution {
  mapId: PalworldMapId
  position: MapPixelPosition
}

function normalize(
  locationX: number,
  locationY: number,
  minX: number,
  minY: number,
  maxX: number,
  maxY: number,
): MapPixelPosition | null {
  const nx = (locationX - minX) / (maxX - minX)
  const ny = (locationY - minY) / (maxY - minY)

  if (nx < 0 || nx > 1 || ny < 0 || ny > 1) {
    return null
  }

  // L'axe écran X suit locationY, l'axe écran Y suit locationX inversé.
  return { xPercent: ny * 100, yPercent: (1 - nx) * 100 }
}

/**
 * Convertit une position brute (LocationX/LocationY) en pourcentage sur l'image
 * de la carte principale (Palpagos Islands). Retourne null si la position tombe
 * hors des bornes connues de cette carte (ex: joueur dans The World Tree).
 * Des bases flottantes en mer sont possibles depuis les mises à jour récentes,
 * un point proche du bord/dans l'eau n'est donc pas une anomalie de calibration.
 */
export function toMapPixel(locationX: number, locationY: number): MapPixelPosition | null {
  return normalize(locationX, locationY, LANDSCAPE_MIN_X, LANDSCAPE_MIN_Y, LANDSCAPE_MAX_X, LANDSCAPE_MAX_Y)
}

/**
 * Convertit une position brute (LocationX/LocationY) en pourcentage sur l'image
 * de la carte de The World Tree. Retourne null si hors des bornes connues.
 */
export function toWorldTreeMapPixel(locationX: number, locationY: number): MapPixelPosition | null {
  return normalize(locationX, locationY, WORLD_TREE_MIN_X, WORLD_TREE_MIN_Y, WORLD_TREE_MAX_X, WORLD_TREE_MAX_Y)
}

/**
 * Détecte automatiquement à quelle carte appartient une position brute (LocationX/LocationY)
 * et renvoie sa projection en pourcentage sur cette carte. La carte principale est testée
 * en priorité (les deux jeux de bornes se recoupent sur une infime marge inatteignable en jeu).
 * Retourne null si la position ne tombe sur aucune carte connue.
 */
export function resolvePalworldMap(locationX: number, locationY: number): PalworldMapResolution | null {
  const palpagos = toMapPixel(locationX, locationY)
  if (palpagos) {
    return { mapId: 'palpagos', position: palpagos }
  }

  const worldTree = toWorldTreeMapPixel(locationX, locationY)
  if (worldTree) {
    return { mapId: 'worldTree', position: worldTree }
  }

  return null
}
