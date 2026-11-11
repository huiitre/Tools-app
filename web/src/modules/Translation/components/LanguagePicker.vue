<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import { autoUpdate, flip, offset, shift, useFloating } from '@floating-ui/vue'
import { languageName } from '../utils/languageName'

const props = defineProps<{
  modelValue: string
  codes: string[]
  // Ajoute « Détecter la langue » en tête (côté source).
  allowAuto?: boolean
  // Langue trouvée par la détection, affichée à côté de « Détecter ».
  detected?: string | null
  label: string
}>()

const emit = defineEmits<{ 'update:modelValue': [code: string] }>()

const open = ref(false)
const query = ref('')
const reference = ref<HTMLElement | null>(null)
const floating = ref<HTMLElement | null>(null)
const searchInput = ref<HTMLInputElement | null>(null)

const { floatingStyles } = useFloating(reference, floating, {
  placement: 'bottom-start',
  middleware: [offset(8), flip(), shift({ padding: 12 })],
  whileElementsMounted: autoUpdate,
})

const buttonLabel = computed(() => {
  if (props.modelValue !== 'auto') return languageName(props.modelValue)
  return props.detected ? languageName(props.detected) : 'Détecter la langue'
})

// L'icône baguette signale déjà la détection ; le détail reste au survol.
const buttonTitle = computed(() =>
  props.modelValue === 'auto' && props.detected ? `Langue détectée : ${languageName(props.detected)}` : props.label,
)

const options = computed(() => {
  const q = query.value.trim().toLocaleLowerCase('fr')
  return props.codes
    .map(code => ({ code, name: languageName(code) }))
    .filter(option => !q || option.name.toLocaleLowerCase('fr').includes(q) || option.code.startsWith(q))
    .sort((a, b) => a.name.localeCompare(b.name, 'fr'))
})

async function toggle() {
  open.value = !open.value
  if (open.value) {
    query.value = ''
    await nextTick()
    searchInput.value?.focus()
  }
}

function pick(code: string) {
  emit('update:modelValue', code)
  open.value = false
}

function onClickOutside(event: MouseEvent) {
  const target = event.target as Node
  if (open.value && !reference.value?.contains(target) && !floating.value?.contains(target)) open.value = false
}

function onKeydown(event: KeyboardEvent) {
  if (open.value && event.key === 'Escape') open.value = false
}

onMounted(() => {
  document.addEventListener('mousedown', onClickOutside)
  document.addEventListener('keydown', onKeydown)
})
onBeforeUnmount(() => {
  document.removeEventListener('mousedown', onClickOutside)
  document.removeEventListener('keydown', onKeydown)
})
</script>

<template>
  <button
    ref="reference"
    type="button"
    class="language-button"
    :class="{ open }"
    :aria-label="label"
    :title="buttonTitle"
    :aria-expanded="open"
    @click="toggle"
  >
    <i v-if="modelValue === 'auto'" class="mdi mdi-auto-fix" aria-hidden="true" />
    <span>{{ buttonLabel }}</span>
    <i class="mdi mdi-chevron-down chevron" aria-hidden="true" />
  </button>

  <Teleport to="body">
    <Transition name="language-panel">
      <div v-if="open" ref="floating" class="language-panel" :style="floatingStyles" role="listbox" :aria-label="label">
        <div class="language-search">
          <i class="mdi mdi-magnify" aria-hidden="true" />
          <input ref="searchInput" v-model="query" type="search" placeholder="Rechercher une langue" />
        </div>

        <button
          v-if="allowAuto"
          type="button"
          class="language-option language-option--auto"
          :class="{ selected: modelValue === 'auto' }"
          role="option"
          :aria-selected="modelValue === 'auto'"
          @click="pick('auto')"
        >
          <i class="mdi mdi-auto-fix" aria-hidden="true" />
          Détecter la langue
        </button>

        <div class="language-grid">
          <button
            v-for="option in options"
            :key="option.code"
            type="button"
            class="language-option"
            :class="{ selected: modelValue === option.code }"
            role="option"
            :aria-selected="modelValue === option.code"
            @click="pick(option.code)"
          >
            <span>{{ option.name }}</span>
            <i v-if="modelValue === option.code" class="mdi mdi-check" aria-hidden="true" />
          </button>
        </div>

        <p v-if="!options.length" class="language-empty">Aucune langue ne correspond.</p>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped lang="scss">
.language-button {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  width: auto;
  max-width: 100%;
  margin: 0;
  padding: 0.45rem 0.8rem;
  border: 1px solid transparent;
  border-radius: 999px;
  background: transparent;
  color: inherit;
  font-size: 0.95rem;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.15s ease, border-color 0.15s ease;

  span { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
  .chevron { color: var(--pico-muted-color); transition: transform 0.15s ease; }

  &:hover, &.open {
    background: color-mix(in srgb, var(--pico-primary) 10%, transparent);
    border-color: color-mix(in srgb, var(--pico-primary) 35%, transparent);
  }

  &.open .chevron { transform: rotate(180deg); }
}

.language-panel {
  z-index: 1200;
  width: min(420px, calc(100vw - 24px));
  padding: 0.6rem;
  border: 1px solid var(--pico-muted-border-color);
  border-radius: 14px;
  background: var(--pico-card-background-color);
  box-shadow: var(--pico-card-box-shadow);
}

.language-search {
  display: flex;
  align-items: center;
  gap: 0.45rem;
  padding: 0 0.6rem;
  margin-bottom: 0.5rem;
  border: 1px solid var(--pico-muted-border-color);
  border-radius: 10px;
  color: var(--pico-muted-color);

  input {
    height: 2.1rem;
    margin: 0;
    padding: 0;
    border: 0;
    background: transparent;
    box-shadow: none;
    font-size: 0.85rem;
  }
}

.language-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.2rem;
  max-height: 280px;
  overflow-y: auto;
}

.language-option {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.4rem;
  width: 100%;
  margin: 0;
  padding: 0.5rem 0.65rem;
  border: 0;
  border-radius: 8px;
  background: transparent;
  color: inherit;
  font-size: 0.85rem;
  text-align: left;
  cursor: pointer;

  &:hover { background: color-mix(in srgb, var(--pico-primary) 9%, transparent); }

  &.selected {
    background: color-mix(in srgb, var(--pico-primary) 16%, transparent);
    color: var(--pico-primary);
    font-weight: 700;
  }

  &--auto {
    justify-content: flex-start;
    margin-bottom: 0.35rem;
    border-bottom: 1px solid var(--pico-muted-border-color);
    border-radius: 8px 8px 0 0;
  }
}

.language-empty {
  margin: 0.75rem 0;
  color: var(--pico-muted-color);
  font-size: 0.8rem;
  text-align: center;
}

.language-panel-enter-active, .language-panel-leave-active { transition: opacity 0.12s ease, transform 0.12s ease; }
.language-panel-enter-from, .language-panel-leave-to { opacity: 0; transform: translateY(-4px); }
</style>
