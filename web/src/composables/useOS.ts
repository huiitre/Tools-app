// src/composables/useOS.ts
const userAgent = navigator.userAgent.toLowerCase()

const isWindows = userAgent.includes('win')
const isLinux = userAgent.includes('linux') && !userAgent.includes('android')

export function useOS() {
  return { isWindows, isLinux }
}