export interface R2rExpeditionSummary {
  id: string
  name: string
  source: string
  currentSystemIndex: number
  totalSystems: number
  createdAt: string
  updatedAt: string
}

export interface R2rLandmark {
  count: number
  subtype: string
  type: string
  value: number
}

export interface R2rBody {
  id64: number
  name: string
  subtype: string
  is_terraformable?: boolean
  distance_to_arrival: number
  estimated_scan_value: number
  estimated_mapping_value: number
  landmark_value?: number | null
  landmarks?: R2rLandmark[]
}

export interface R2rSystem {
  id64: number
  name: string
  jumps: number
  bodies: R2rBody[]
}

export interface R2rRouteData {
  result: R2rSystem[]
  parameters: {
    source?: string
    destination?: string
    range?: string | number
    min_value?: string | number
    radius?: string | number
    use_mapping_value?: string
  }
  job?: string
}

export interface R2rExpeditionDetail {
  id: string
  name: string
  source: string
  routeData: string
  parsedRouteData?: R2rRouteData
  currentSystemIndex: number
  currentBodiesDone: number[]
  createdAt: string
  updatedAt: string
}

export interface R2rSource {
  id: string
  label: string
  accept: string
}

export const R2R_SOURCES: R2rSource[] = [
  { id: 'spansh', label: 'Spansh', accept: '.json' },
]