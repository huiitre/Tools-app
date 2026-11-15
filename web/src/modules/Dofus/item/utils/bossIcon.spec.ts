import { describe, expect, it } from 'vitest'
import type { Item } from '@/modules/Dofus/item/types/item.types'
import { isBossOnlyDrop } from './bossIcon'

type Zones = NonNullable<Item['farmZones']>

//* Forme réelle de GET /dofus/workshops/{id} (atelier 21, base de dev, 16/11/2026).
const zone = (subarea: string, monsters: { name: string; boss: boolean }[], isPrimary = false): Zones[number] => ({
  area: { id: 1, assetId: 1, name: 'Zone' },
  subarea: { id: 1, areaId: 1, assetId: 1, name: subarea },
  monsters: monsters.map((m, i) => ({ id: i + 1, name: m.name, images: [], boss: m.boss })),
  isPrimary,
})

const item = (farmZones?: Zones) => ({ farmZones })

describe('isBossOnlyDrop', () => {
  it('Hamatum de Glourséleste : tombe uniquement sur Glourséleste (boss)', () => {
    expect(isBossOnlyDrop(item([zone('Antichambre des Glours', [{ name: 'Glourséleste', boss: true }], true)]))).toBe(true)
  })

  it('Volve de Fistulor : tombe sur Fistulor (pas un boss)', () => {
    expect(isBossOnlyDrop(item([zone('Zone', [{ name: 'Fistulor', boss: false }], true)]))).toBe(false)
  })

  it('Sang de Bouftou : le Bouftou Royal (boss) dans son donjon, des Bouftous ailleurs', () => {
    expect(isBossOnlyDrop(item([
      zone('Pâturages', [{ name: 'Bouftou', boss: false }, { name: 'Chef de Guerre Bouftou', boss: false }], true),
      zone('Donjon des Bouftous', [{ name: 'Bouftou Royal', boss: true }]),
    ]))).toBe(false)
  })

  it('boss et monstre normal dans la même zone : pas uniquement boss', () => {
    expect(isBossOnlyDrop(item([zone('Zone', [{ name: 'Boss', boss: true }, { name: 'Normal', boss: false }], true)]))).toBe(false)
  })

  it('plusieurs boss dans plusieurs zones : uniquement boss', () => {
    expect(isBossOnlyDrop(item([
      zone('Donjon A', [{ name: 'Rat Noir', boss: true }], true),
      zone('Donjon B', [{ name: 'Rat Blanc', boss: true }]),
    ]))).toBe(true)
  })

  it('aucune zone, ou zones sans monstre (récolte, craft, Retro sans données) : faux', () => {
    expect(isBossOnlyDrop(item(undefined))).toBe(false)
    expect(isBossOnlyDrop(item([]))).toBe(false)
    expect(isBossOnlyDrop(item([zone('Vide', [])]))).toBe(false)
  })
})
