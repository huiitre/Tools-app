// src/composables/useScreen.ts
import { ref, onMounted, onBeforeUnmount } from 'vue'

const DESKTOP_MIN_WIDTH = 1280;

export function useScreen() {
  const isDesktop = ref(window.innerWidth >= DESKTOP_MIN_WIDTH)

  const onResize = () => {
    isDesktop.value = window.innerWidth >= DESKTOP_MIN_WIDTH
  }

  onMounted(() => window.addEventListener('resize', onResize))
  onBeforeUnmount(() => window.removeEventListener('resize', onResize))

  return { isDesktop }
}
