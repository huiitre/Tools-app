import { ref } from 'vue'

const isOpen = ref(false)
const imageUrl = ref('')
const imageAlt = ref('')
const imageMinSize = ref<number | null>(null)

export const useImagePreview = () => {
  const open = (url: string, alt = '', minSize?: number) => {
    imageUrl.value = url
    imageAlt.value = alt
    imageMinSize.value = minSize ?? null
    isOpen.value = true
  }

  const close = () => {
    isOpen.value = false
    imageUrl.value = ''
    imageAlt.value = ''
    imageMinSize.value = null
  }

  return {
    isOpen,
    imageUrl,
    imageAlt,
    imageMinSize,
    open,
    close,
  }
}