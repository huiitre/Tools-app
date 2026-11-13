import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'

//* Base simulée : les objets d'id >= UNKNOWN_FROM n'existent pas.
const UNKNOWN_FROM = 990

const selection = { code: 'dofus3', versionId: 3, serverId: 1 as number | null }

const mutation = vi.fn(async (_items: { itemId: number; price: number }[]) => ({}))

vi.mock('@/modules/Dofus/item/fetch/item.fetch', () => ({
  useFetchItemsByAssetIds: vi.fn(async (ids: number[]) => ({
    data: ids.filter(id => id < UNKNOWN_FROM).map(id => ({ id: id + 100_000, assetId: id, name: `Objet ${id}` })),
  })),
  useMutationItemPrices: (items: { itemId: number; price: number }[]) => mutation(items),
}))

vi.mock('@/modules/Dofus/dofus.store', () => ({
  useDofusStore: () => ({
    get currentGameVersion() { return { code: selection.code } },
    get currentGameVersionId() { return selection.versionId },
    get currentGameServerId() { return selection.serverId },
  }),
}))

vi.mock('@/modules/Dofus/almanax/composables/useItemPrices', () => ({
  useItemPrices: () => ({ get: () => undefined, refreshRecursive: vi.fn() }),
}))

import { useHdvSnifferStore, type SnifferBatchCapture } from './hdvSniffer.store'

//* 1 200 objets reçus, dont 1 000 avec un prix (ids 0 à 999) ; 990 à 999 sont absents de la base.
const priceList = (receivedAt: number) => ({
  receivedAt,
  entries: Array.from({ length: 1200 }, (_, id) => ({ id, price: id < 1000 ? 10 + id : null })),
})

describe('hdvSniffer — liste des prix moyens Dofus 3', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    mutation.mockClear()
    selection.code = 'dofus3'
    selection.serverId = 1
    vi.useFakeTimers()
  })

  it('compte les prix réellement enregistrés et ceux absents de la base', async () => {
    const store = useHdvSnifferStore()
    store._onPriceList(priceList(1_000))
    await store.syncAll()

    const batch = store.captures[0] as SnifferBatchCapture
    expect(batch.kind).toBe('batch')
    expect(batch.count).toBe(1200)
    expect(batch.pricedCount).toBe(1000)
    expect(batch.savedCount).toBe(990)
    expect(batch.unknownCount).toBe(10)

    //* Envoi par paquets de 500 au plus.
    expect(mutation.mock.calls.map(c => c[0].length)).toEqual([500, 490])
  })

  it('ignore une liste identique reçue moins de 60 s plus tard', async () => {
    const store = useHdvSnifferStore()
    store._onPriceList(priceList(1_000))
    store._onPriceList(priceList(30_000))
    expect(store.captures).toHaveLength(1)

    store._onPriceList(priceList(70_000))
    expect(store.captures).toHaveLength(2)
  })

  it('n\'enregistre rien quand le sélecteur n\'est pas sur Dofus 3', async () => {
    selection.code = 'retro'
    const store = useHdvSnifferStore()
    store._onPriceList(priceList(1_000))
    await store.syncAll()

    expect(store.captures).toHaveLength(0)
    expect(store.lastIgnored).toMatchObject({ source: 'dofus3', count: 1000 })
    expect(mutation).not.toHaveBeenCalled()
  })

  it('compte comme absents, sans les redemander, les objets déjà connus comme absents', async () => {
    const store = useHdvSnifferStore()
    store._onPriceList(priceList(1_000))
    await store.syncAll()

    //* Liste différente (un prix change) reçue après 60 s : 990 à 999 sont déjà connus absents.
    const next = priceList(100_000)
    next.entries[0].price = 999_999
    store._onPriceList(next)
    await store.syncAll()

    const latest = store.captures[0] as SnifferBatchCapture
    expect(latest.savedCount).toBe(990)
    expect(latest.unknownCount).toBe(10)
  })
})
