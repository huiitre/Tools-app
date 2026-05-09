<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue';
import { useFloating, offset } from '@floating-ui/vue';
import DofusPreferencesPanel from '../../preferences/components/DofusPreferencesPanel.vue';

const isOpen = ref(false);
const reference = ref<HTMLElement | null>(null);
const floating = ref<HTMLElement | null>(null);

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
  isOpen.value = !isOpen.value
};

onMounted(() => {
  document.addEventListener('click', onClickOutside)
  document.addEventListener('scroll', onScroll, true)
})

onBeforeUnmount(() => {
  document.removeEventListener('click', onClickOutside)
  document.removeEventListener('scroll', onScroll, true)
})

</script>

<template>
  <div class="dofus-config-button">
    <button
      ref="reference"
      class="dofus-config-button"
      type="button"
      aria-label="Paramètres Dofus"
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
      <DofusPreferencesPanel />
    </div>
  </div>
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
    border-color 0.15s ease;

  &:hover {
    color: var(--pico-primary);
    background-color: var(--pico-card-background-color);
    border-color: var(--pico-muted-border-color);
  }

  &:active {
    background-color: color-mix(
      in srgb,
      var(--pico-primary) 12%,
      transparent
    );
  }

  i {
    font-size: 1.1rem;
    line-height: 1;
  }

  & .floating-panel {
      position: absolute;
      z-index: 1000;

      min-width: 220px;
      padding: 0.5rem 0.65rem;

      background: var(--pico-background-color);
      border: 1px solid var(--pico-muted-border-color);
      border-radius: var(--pico-border-radius);
      box-shadow: var(--pico-card-box-shadow);

      font-size: 0.75rem;
      color: var(--pico-color);

      white-space: normal;
    }
}
</style>
