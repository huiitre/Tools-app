<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue'
import { useDofusStore } from '@/modules/Dofus/dofus.store'
import { useEnv } from '@/composables/useEnv'

const dofusStore = useDofusStore()
const { isElectron } = useEnv()

function openSwitcher() {
  if (dofusStore.switcherOpen) return
  window.electron!.openSwitcher()
  dofusStore.setSwitcherOpen(true)
}

onMounted(() => {
  if (!isElectron) return
  window.electron!.onSwitcherClosed(() => {
    dofusStore.setSwitcherOpen(false)
  })
})

onUnmounted(() => {
  if (!isElectron) return
  window.electron!.offSwitcherClosed()
})
</script>

<template>
  <button
    v-if="isElectron"
    class="dofus-config-button"
    type="button"
    aria-label="Dofus Switcher"
    title="Dofus Switcher"
    :disabled="dofusStore.switcherOpen"
    @click="openSwitcher"
  >
    <i class="mdi mdi-deskphone" aria-hidden="true" />
  </button>
</template>

<style lang="scss" scoped>
.dofus-config-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;

  width: 2.25rem;
  height: 2.25rem;

  padding: 0;
  margin: 0;

  border-radius: var(--pico-border-radius);
  border: 1px solid transparent;

  background: transparent;
  color: var(--pico-muted-color);

  cursor: pointer;

  transition:
    color 0.15s ease,
    background-color 0.15s ease,
    border-color 0.15s ease,
    opacity 0.15s ease;

  &:hover:not(:disabled) {
    color: var(--pico-primary);
    background-color: var(--pico-card-background-color);
    border-color: var(--pico-muted-border-color);
  }

  &:active:not(:disabled) {
    background-color: color-mix(
      in srgb,
      var(--pico-primary) 12%,
      transparent
    );
  }

  &:disabled {
    opacity: 0.35;
    cursor: not-allowed;
  }

  i {
    font-size: 1.1rem;
    line-height: 1;
  }
}
</style>