export const pwa = {
  updateSW: null as ((reloadPage?: boolean) => Promise<void>) | null,
  onNeedRefresh: null as (() => void) | null,
}
