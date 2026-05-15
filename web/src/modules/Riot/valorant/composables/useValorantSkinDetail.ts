import { ref } from 'vue'
import type { ValorantSkin } from '../valorant.types'

const isOpen = ref(false)
const skin = ref<ValorantSkin | null>(null)

export function useValorantSkinDetail() {
  function open(s: ValorantSkin) {
    skin.value = s
    isOpen.value = true
  }

  function close() {
    isOpen.value = false
    skin.value = null
  }

  return { isOpen, skin, open, close }
}
