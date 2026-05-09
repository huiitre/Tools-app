const isElectron = navigator.userAgent.includes('Electron')

export function useEnv() {
  return {
    isElectron,
    isWeb: !isElectron,
  }
}
