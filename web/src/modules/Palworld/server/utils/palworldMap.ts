const LANDSCAPE_MIN_X = -1099400
const LANDSCAPE_MIN_Y = -724400
const LANDSCAPE_MAX_X = 349400
const LANDSCAPE_MAX_Y = 724400

export interface MapPixelPosition {
  xPercent: number
  yPercent: number
}

/**
 * Convertit une position brute (LocationX/LocationY) en pourcentage sur l'image
 * de la carte principale (Palpagos Islands). Retourne null si la position tombe
 * hors des bornes connues (autre carte, ex: The World Tree — pas encore supportée).
 * Des bases flottantes en mer sont possibles depuis les mises à jour récentes,
 * un point proche du bord/dans l'eau n'est donc pas une anomalie de calibration.
 */
export function toMapPixel(locationX: number, locationY: number): MapPixelPosition | null {
  const nx = (locationX - LANDSCAPE_MIN_X) / (LANDSCAPE_MAX_X - LANDSCAPE_MIN_X)
  const ny = (locationY - LANDSCAPE_MIN_Y) / (LANDSCAPE_MAX_Y - LANDSCAPE_MIN_Y)

  if (nx < 0 || nx > 1 || ny < 0 || ny > 1) {
    return null
  }

  // L'axe écran X suit locationY, l'axe écran Y suit locationX inversé.
  return { xPercent: ny * 100, yPercent: (1 - nx) * 100 }
}
