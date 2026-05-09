<script setup lang="ts">
import { computed, watch, onBeforeUnmount } from 'vue'
import { useImagePreview } from '@/composables/useImagePreview'

const { isOpen, imageUrl, imageAlt, imageMinSize, close } = useImagePreview()

const imgStyle = computed(() => imageMinSize.value
  ? { minWidth: `${imageMinSize.value}px`, minHeight: `${imageMinSize.value}px` }
  : {}
)

watch(isOpen, (value) => {
  if (value) {
    window.addEventListener('scroll', close, { passive: true })
  } else {
    window.removeEventListener('scroll', close)
  }
})

onBeforeUnmount(() => {
  window.removeEventListener('scroll', close)
})
</script>

<template>
  <Teleport to="body">
    <div v-if="isOpen" class="image-preview-overlay" @click="close">
      <img :src="imageUrl" :alt="imageAlt" :style="imgStyle" class="image-preview-content" />
    </div>
  </Teleport>
</template>

<style scoped lang="scss">
.image-preview-overlay {
  position: fixed;
  inset: 0;
  z-index: 9999;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(0, 0, 0, 0.85);
  cursor: pointer;
}

.image-preview-content {
  max-width: 90vw;
  max-height: 90vh;
  object-fit: contain;
  border-radius: 0.5rem;
}
</style>
