<script setup lang="ts">
import { computed, ref, watch } from 'vue'

// Calqué sur BreedingPassiveSelectorModal (Palworld) : sélection en attente, validée d'un bloc.
const props = defineProps<{
  open: boolean
  scopes: string[]
  modelValue: string[]
}>()

const emit = defineEmits<{
  apply: [scopes: string[]]
  close: []
}>()

const query = ref('')
const pending = ref<string[]>([])

const visibleScopes = computed(() => {
  const normalizedQuery = query.value.trim().toLocaleLowerCase()
  return props.scopes.filter(scope => !normalizedQuery || scope.includes(normalizedQuery))
})

function toggleScope(scope: string) {
  pending.value = pending.value.includes(scope)
    ? pending.value.filter(s => s !== scope)
    : [...pending.value, scope]
}

function applySelection() {
  // Dans l'ordre du catalogue, quel que soit l'ordre des clics.
  emit('apply', props.scopes.filter(scope => pending.value.includes(scope)))
}

watch(() => props.open, isOpen => {
  if (!isOpen) return
  pending.value = [...props.modelValue]
  query.value = ''
})
</script>

<template>
  <Teleport to="body">
    <Transition name="scope-modal">
      <div v-if="open" class="scope-modal-backdrop" @mousedown.self="emit('close')">
        <section class="scope-modal" role="dialog" aria-modal="true" aria-label="Choisir les scopes">
          <header class="scope-modal__header">
            <div>
              <strong>Choisir les scopes</strong>
              <small>Les opérations que cette clé pourra exécuter.</small>
            </div>
            <div class="scope-modal__actions">
              <span>{{ pending.length }}/{{ scopes.length }}</span>
              <button type="button" aria-label="Fermer" @click="emit('close')">
                <i class="mdi mdi-close" />
              </button>
            </div>
          </header>

          <div class="scope-modal__toolbar">
            <label for="scope-search">Rechercher les scopes</label>
            <button type="button" :disabled="pending.length === 0" @click="pending = []">Effacer</button>
          </div>

          <div class="scope-modal__search">
            <i class="mdi mdi-magnify" aria-hidden="true" />
            <input id="scope-search" v-model="query" type="search" placeholder="Rechercher un scope">
          </div>

          <div class="scope-modal__list custom-scrollbar">
            <button
              v-for="scope in visibleScopes"
              :key="scope"
              type="button"
              class="scope-option"
              :class="{ selected: pending.includes(scope) }"
              :title="scope"
              @click="toggleScope(scope)"
            >
              <span>{{ scope }}</span>
              <i v-if="pending.includes(scope)" class="mdi mdi-check" aria-hidden="true" />
            </button>
            <p v-if="visibleScopes.length === 0" class="scope-modal__empty">Aucun scope correspondant.</p>
          </div>

          <footer>
            <button type="button" @click="applySelection">Valider</button>
          </footer>
        </section>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped lang="scss">
.scope-modal-backdrop {
  position: fixed;
  z-index: 9100;
  inset: 0;
  display: grid;
  place-items: center;
  padding: 1rem;
  background: rgb(0 0 0 / 58%);
  backdrop-filter: blur(3px);
}

.scope-modal {
  display: flex;
  flex-direction: column;
  width: min(560px, 100%);
  max-height: min(640px, calc(100vh - 2rem));
  padding: 1rem;
  margin: 0;
  overflow: hidden;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: 12px;
  box-shadow: 0 18px 50px rgb(0 0 0 / 42%);
}

.scope-modal__header,
.scope-modal__actions,
.scope-modal__toolbar,
footer {
  display: flex;
  align-items: center;
}

.scope-modal__header {
  justify-content: space-between;
  gap: 1rem;

  strong,
  small { display: block; }
  strong { font-size: .86rem; }
  small { margin-top: .2rem; color: var(--pico-muted-color); font-size: .66rem; }
}

.scope-modal__actions { gap: .65rem; }
.scope-modal__actions span { padding: .15rem .42rem; border-radius: 999px; background: color-mix(in srgb, var(--pico-primary) 16%, transparent); color: var(--pico-primary); font-size: .65rem; font-weight: 700; }
.scope-modal__actions button { display: grid; place-items: center; width: 2rem; height: 2rem; margin: 0; padding: 0; border-color: var(--pico-muted-border-color); background: transparent; color: var(--pico-muted-color); }

.scope-modal__toolbar { justify-content: space-between; margin-top: 1rem; }
.scope-modal__toolbar label { font-size: .72rem; font-weight: 600; }
.scope-modal__toolbar button { width: auto; margin: 0; padding: 0; border: 0; background: transparent; color: var(--pico-muted-color); font-size: .65rem; }

.scope-modal__search { display: flex; align-items: center; gap: .45rem; margin-top: .45rem; padding: 0 .6rem; border: 1px solid var(--pico-muted-border-color); border-radius: var(--pico-border-radius); color: var(--pico-muted-color); }
.scope-modal__search input { height: 2.15rem; margin: 0; padding: 0; border: 0; background: transparent; box-shadow: none; font-size: .78rem; }

.scope-modal__list { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: .5rem 1.4rem; min-height: 0; margin-top: .7rem; overflow-y: auto; padding: .15rem .25rem .15rem .15rem; }
.scope-option { display: grid; grid-template-columns: minmax(0, 1fr) auto; align-items: center; gap: .35rem; width: 100%; min-height: 30px; margin: 0; padding: .35rem .5rem; border: 1px solid var(--pico-muted-border-color); border-left: 3px solid var(--pico-primary); border-radius: 0; background: color-mix(in srgb, var(--pico-primary) 7%, var(--pico-card-background-color)); color: inherit; font-family: monospace; font-size: .7rem; font-weight: 600; text-align: left; }
.scope-option:hover { background: color-mix(in srgb, var(--pico-primary) 18%, var(--pico-card-background-color)); }
.scope-option.selected { border-color: #22c55e; border-left-color: #22c55e; background: color-mix(in srgb, #22c55e 13%, var(--pico-card-background-color)); box-shadow: 0 0 0 1px #22c55e; }
.scope-option span { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.scope-option i { width: 18px; height: 18px; color: #22c55e; }
.scope-modal__empty { grid-column: 1 / -1; margin: 1rem 0; color: var(--pico-muted-color); font-size: .72rem; text-align: center; }

footer { justify-content: flex-end; padding-top: 1rem; }
footer button { width: 110px; margin: 0; padding: .5rem; font-size: .72rem; font-weight: 700; }

.custom-scrollbar::-webkit-scrollbar { width: 4px; }
.custom-scrollbar::-webkit-scrollbar-track { background: transparent; }
.custom-scrollbar::-webkit-scrollbar-thumb { background: var(--pico-muted-border-color); border-radius: 10px; }

.scope-modal-enter-active, .scope-modal-leave-active { transition: opacity .16s ease; }
.scope-modal-enter-active .scope-modal, .scope-modal-leave-active .scope-modal { transition: transform .16s ease; }
.scope-modal-enter-from, .scope-modal-leave-to { opacity: 0; }
.scope-modal-enter-from .scope-modal, .scope-modal-leave-to .scope-modal { transform: translateY(8px) scale(.98); }

@media (max-width: 520px) {
  .scope-modal__list { grid-template-columns: 1fr; }
}
</style>
