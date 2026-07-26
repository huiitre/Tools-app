import type { PalworldPalListItem } from '../types/paldex.types'

export function paldexLabel(pal: Pick<PalworldPalListItem, 'paldexIndex' | 'paldexSuffix'>): string {
  if (pal.paldexIndex === null) return '—'
  return `#${String(pal.paldexIndex).padStart(3, '0')}${pal.paldexSuffix ?? ''}`
}
