<script setup lang="ts">
import { useEnv } from '@/composables/useEnv'

defineProps<{ url: string; label: string }>()
const emit = defineEmits<{ (e: 'close'): void }>()

const { isElectron } = useEnv()
</script>

<template>
  <div class="overlay" @click.self="emit('close')">
    <div class="viewer-popup">
      <div class="viewer-header">
        <span class="viewer-title">{{ label }}</span>
        <a
          :href="url"
          target="_blank"
          rel="noopener noreferrer"
          class="header-external"
          title="Ouvrir dans un nouvel onglet"
          @click="emit('close')"
        >
          <span class="mdi mdi-open-in-new"></span>
        </a>
        <span class="mdi mdi-close action" @click="emit('close')"></span>
      </div>

      <!-- Electron : webview (contourne X-Frame-Options) -->
      <webview v-if="isElectron" :src="url" class="viewer-frame"></webview>

      <!-- Web : fallback, les sites bloquent généralement les iframes -->
      <div v-else class="viewer-fallback">
        <span class="mdi mdi-web-off fallback-icon"></span>
        <p>Ce site bloque l'affichage dans une fenêtre intégrée.</p>
        <a :href="url" target="_blank" rel="noopener noreferrer" @click="emit('close')">
          Ouvrir dans un nouvel onglet
        </a>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.viewer-popup {
  width: 85vw;
  height: 85vh;
  background: var(--pico-card-background-color);
  border-radius: var(--pico-border-radius);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.viewer-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.6rem 0.75rem;
  border-bottom: 1px solid var(--pico-muted-border-color);
  flex-shrink: 0;
}

.viewer-title {
  font-size: 0.9rem;
  font-weight: 500;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  flex: 1;
}

.header-external {
  font-size: 1.1rem;
  opacity: 0.5;
  color: inherit;
  flex-shrink: 0;

  &:hover {
    opacity: 1;
    color: var(--pico-primary);
  }
}

.viewer-frame {
  flex: 1;
  width: 100%;
  border: none;
}

.viewer-fallback {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  color: var(--pico-muted-color);

  p {
    margin: 0;
    font-size: 0.9rem;
  }

  a {
    font-size: 0.9rem;

    &:hover {
      color: var(--pico-primary);
    }
  }
}

.fallback-icon {
  font-size: 3rem;
  opacity: 0.4;
}

.action {
  font-size: 1.2rem;
  cursor: pointer;
  opacity: 0.6;
  flex-shrink: 0;

  &:hover {
    opacity: 1;
  }
}
</style>
