export interface IWindowFocuser {
  focus(windowId: string): Promise<void>
}