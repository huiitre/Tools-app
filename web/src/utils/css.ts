// --- Utils couleurs ---
function colorDistance(hex1: string, hex2: string) {
  const c1 = parseInt(hex1.substring(1), 16)
  const r1 = (c1 >> 16) & 0xff
  const g1 = (c1 >> 8) & 0xff
  const b1 = c1 & 0xff

  const c2 = parseInt(hex2.substring(1), 16)
  const r2 = (c2 >> 16) & 0xff
  const g2 = (c2 >> 8) & 0xff
  const b2 = c2 & 0xff

  return Math.sqrt((r1 - r2) ** 2 + (g1 - g2) ** 2 + (b1 - b2) ** 2)
}
export function isDark(hex: string) {
  const c = hex.substring(1)
  const rgb = parseInt(c, 16)
  const r = (rgb >> 16) & 0xff
  const g = (rgb >> 8) & 0xff
  const b = rgb & 0xff
  const yiq = (r * 299 + g * 587 + b * 114) / 1000
  return yiq < 128
}
export function isGoldConflict(hex: string) {
  const goldHex = "#FFD700"
  const threshold = 100
  return colorDistance(hex, goldHex) < threshold
}

export function hexToRgba(hex: string, alpha = 0.15) {
  const bigint = parseInt(hex.slice(1), 16)
  const r = (bigint >> 16) & 255
  const g = (bigint >> 8) & 255
  const b = bigint & 255
  return `rgba(${r}, ${g}, ${b}, ${alpha})`
}