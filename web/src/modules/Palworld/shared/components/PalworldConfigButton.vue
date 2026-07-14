<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { useFloating, offset } from '@floating-ui/vue'
import { usePalworldConfigStore } from '../palworldConfig.store'

const configStore = usePalworldConfigStore()

const isOpen = ref(false)
const reference = ref<HTMLElement | null>(null)
const floating = ref<HTMLElement | null>(null)
const refreshIntervalInput = ref(configStore.refreshIntervalSeconds)

const { floatingStyles } = useFloating(reference, floating, {
  placement: 'bottom-end',
  middleware: [
    offset(5),
  ],
})

const onClickOutside = (e: MouseEvent) => {
  const target = e.target as HTMLElement

  if (
    reference.value &&
    floating.value &&
    !reference.value.contains(target) &&
    !floating.value.contains(target)
  ) {
    isOpen.value = false
  }
}

const onScroll = () => {
  if (isOpen.value) {
    isOpen.value = false
  }
}

const onClick = () => {
  configStore.hydrate()
  refreshIntervalInput.value = configStore.refreshIntervalSeconds
  isOpen.value = !isOpen.value
}

const applyRefreshInterval = () => {
  configStore.setRefreshIntervalSeconds(refreshIntervalInput.value)
  refreshIntervalInput.value = configStore.refreshIntervalSeconds
}

onMounted(() => {
  configStore.hydrate()
  document.addEventListener('click', onClickOutside)
  document.addEventListener('scroll', onScroll, true)
})

onBeforeUnmount(() => {
  document.removeEventListener('click', onClickOutside)
  document.removeEventListener('scroll', onScroll, true)
})
</script>

<template>
  <div class="palworld-config-container">
    <button
      ref="reference"
      class="palworld-config-button"
      type="button"
      aria-label="Paramètres Palworld"
      title="Paramètres"
      @click="onClick"
    >
      <i class="mdi mdi-tune-variant" aria-hidden="true" />
    </button>

    <div
      v-if="isOpen"
      ref="floating"
      :style="floatingStyles"
      class="floating-panel"
    >
      <div class="palworld-config-panel">
        <label class="field-label" for="palworld-refresh-interval">
          Intervalle de rafraîchissement (s)
        </label>
        <input
          id="palworld-refresh-interval"
          v-model.number="refreshIntervalInput"
          type="number"
          min="1"
          placeholder="5"
          @blur="applyRefreshInterval"
          @keyup.enter="applyRefreshInterval"
        >
        <p class="field-hint">0 ou vide = 5 secondes par défaut.</p>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.palworld-config-container {
  display: inline-flex;
}

.palworld-config-button {
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
    border-color 0.15s ease;

  &:hover {
    color: var(--pico-primary);
    background-color: var(--pico-card-background-color);
    border-color: var(--pico-muted-border-color);
  }

  &:active {
    background-color: color-mix(in srgb, var(--pico-primary) 12%, transparent);
  }

  i {
    font-size: 1.1rem;
    line-height: 1;
  }
}

.floating-panel {
  position: absolute;
  z-index: 1000;

  min-width: 220px;
  padding: 0.65rem 0.75rem;

  background: var(--pico-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  box-shadow: var(--pico-card-box-shadow);

  font-size: 0.75rem;
  color: var(--pico-color);
}

.palworld-config-panel {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.field-label {
  font-size: 0.7rem;
  font-weight: 600;
  color: var(--pico-muted-color);
  text-transform: uppercase;
  letter-spacing: 0.03em;
}

.palworld-config-panel input {
  margin: 0;
  padding: 0.35rem 0.5rem;
  font-size: 0.8rem;
  height: auto;
}

.field-hint {
  margin: 0;
  font-size: 0.68rem;
  color: var(--pico-muted-color);
}
</style>
