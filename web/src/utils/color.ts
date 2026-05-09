/**
 * Calcule la luminance d'une couleur hexadécimale
 * Retourne "black" ou "white" selon le contraste optimal
 */
export const getContrastColor = (hexColor: string): string => {
  // Enlever le # si présent
  const hex = hexColor.replace('#', '')
  
  // Convertir en RGB
  const r = parseInt(hex.substring(0, 2), 16)
  const g = parseInt(hex.substring(2, 4), 16)
  const b = parseInt(hex.substring(4, 6), 16)
  
  // Calculer la luminance (formule WCAG)
  const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255
  
  // Si luminance > 0.5 → fond clair → texte noir
  return luminance > 0.5 ? 'rgba(0, 0, 0, 0.87)' : 'white'
}