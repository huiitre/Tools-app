// Messages de la capture réseau Dofus 3 (electron/capture/).

export interface CaptureStatus {
  running: boolean
  consumers: string[]
  requirements: { tsharkInstalled: boolean; tsharkPermissions: boolean } | null
  error: string | null
  lastConnectionAt: number | null
  lastPriceListAt?: number | null
  lastPriceListCount?: number | null
}

export interface CapturePriceList {
  entries: { id: number; price: number | null }[]
  receivedAt: number
}

export interface CaptureItemOffers {
  itemId: number
  lots: [number, number, number, number]
  knownAverage: number | null
  receivedAt: number
}
