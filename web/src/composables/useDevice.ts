// src/composables/useDevice.ts
const isMobileDevice = /Android|iPhone|iPad|iPod|webOS|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent)

console.log("%c useDevice.ts #4 || isMobileDevice : ", 'background:red;color:#fff;font-weight:bold;', isMobileDevice);
console.log("%c useDevice.ts #5 || navigator.userAgent : ", 'background:red;color:#fff;font-weight:bold;', navigator.userAgent);

export function useDevice() {
  return { isMobileDevice }
}