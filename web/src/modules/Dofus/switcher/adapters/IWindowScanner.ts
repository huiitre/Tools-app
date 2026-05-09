export interface DofusWindow {
  windowId: string
  pid: number
  characterName: string | null
  enabled: boolean
}

export interface IWindowScanner {
  scan(): Promise<DofusWindow[]>
}