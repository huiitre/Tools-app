<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import type { PalworldPassiveSkill } from '../../passives/types/passiveSkills.types'

const props = defineProps<{
  open: boolean
  passiveSkills: PalworldPassiveSkill[]
  availablePassiveIds: string[]
  modelValue: string[]
  maxSelections?: number
}>()

const emit = defineEmits<{
  apply: [passiveIds: string[]]
  close: []
}>()

const query = ref('')
const pendingIds = ref<string[]>([])
const selectionLimit = computed(() => props.maxSelections ?? 4)

const availablePassives = computed(() => {
  const availableIds = new Set(props.availablePassiveIds)
  const normalizedQuery = query.value.trim().toLocaleLowerCase()

  return props.passiveSkills.filter(passiveSkill =>
    availableIds.has(passiveSkill.id)
    && (!normalizedQuery || passiveSkill.name.toLocaleLowerCase().includes(normalizedQuery)))
})

function togglePassive(passiveId: string) {
  if (pendingIds.value.includes(passiveId)) {
    pendingIds.value = pendingIds.value.filter(id => id !== passiveId)
    return
  }
  if (selectionLimit.value > 0 && pendingIds.value.length === selectionLimit.value) return
  pendingIds.value = [...pendingIds.value, passiveId]
}

function applySelection() {
  emit('apply', pendingIds.value)
}

watch(() => props.open, isOpen => {
  if (!isOpen) return
  pendingIds.value = [...props.modelValue]
  query.value = ''
})
</script>

<template>
  <Teleport to="body">
    <Transition name="passive-modal">
      <div v-if="open" class="passive-modal-backdrop" @mousedown.self="emit('close')">
        <section class="passive-modal" role="dialog" aria-modal="true" aria-label="Choisir les passifs">
          <header class="passive-modal__header">
            <div>
              <strong>Choisir les passifs</strong>
              <small>{{ selectionLimit > 0 ? `Sélectionne jusqu’à ${selectionLimit} passifs présents sur tes Pals.` : 'Sélectionne les passifs présents sur tes Pals.' }}</small>
            </div>
            <div class="passive-modal__actions">
              <span>{{ selectionLimit > 0 ? `${pendingIds.length}/${selectionLimit}` : pendingIds.length }}</span>
              <button type="button" aria-label="Fermer" @click="emit('close')">
                <i class="mdi mdi-close" />
              </button>
            </div>
          </header>

          <div class="passive-modal__toolbar">
            <label for="passive-search">Rechercher les passifs</label>
            <button type="button" :disabled="pendingIds.length === 0" @click="pendingIds = []">Effacer</button>
          </div>

          <div class="passive-modal__search">
            <i class="mdi mdi-magnify" aria-hidden="true" />
            <input id="passive-search" v-model="query" type="search" placeholder="Rechercher un passif">
          </div>

          <div class="passive-modal__list custom-scrollbar">
            <button
              v-for="passiveSkill in availablePassives"
              :key="passiveSkill.id"
              type="button"
              class="passive-option"
              :class="[
                `rank-${passiveSkill.rank}`,
                { selected: pendingIds.includes(passiveSkill.id), disabled: selectionLimit > 0 && !pendingIds.includes(passiveSkill.id) && pendingIds.length === selectionLimit },
              ]"
              :title="passiveSkill.description ?? passiveSkill.name"
              @click="togglePassive(passiveSkill.id)"
            >
              <span>{{ passiveSkill.name }}</span>
              <span class="passive-option__icons">
                <img v-if="passiveSkill.rankIconUrl" :src="passiveSkill.rankIconUrl" alt="" aria-hidden="true">
                <i v-else class="mdi mdi-chevron-double-up" aria-hidden="true" />
                <i v-if="pendingIds.includes(passiveSkill.id)" class="mdi mdi-check" aria-hidden="true" />
              </span>
            </button>
            <p v-if="availablePassives.length === 0" class="passive-modal__empty">Aucun passif correspondant.</p>
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
.passive-modal-backdrop {
  position: fixed;
  z-index: 1100;
  inset: 0;
  display: grid;
  place-items: center;
  padding: 1rem;
  background: rgb(0 0 0 / 58%);
  backdrop-filter: blur(3px);
}

.passive-modal {
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

.passive-modal__header,
.passive-modal__actions,
.passive-modal__toolbar,
footer {
  display: flex;
  align-items: center;
}

.passive-modal__header {
  justify-content: space-between;
  gap: 1rem;

  strong,
  small { display: block; }
  strong { font-size: .86rem; }
  small { margin-top: .2rem; color: var(--pico-muted-color); font-size: .66rem; }
}

.passive-modal__actions { gap: .65rem; }
.passive-modal__actions span { padding: .15rem .42rem; border-radius: 999px; background: color-mix(in srgb, var(--pico-primary) 16%, transparent); color: var(--pico-primary); font-size: .65rem; font-weight: 700; }
.passive-modal__actions button { display: grid; place-items: center; width: 2rem; height: 2rem; margin: 0; padding: 0; border-color: var(--pico-muted-border-color); background: transparent; color: var(--pico-muted-color); }

.passive-modal__toolbar { justify-content: space-between; margin-top: 1rem; }
.passive-modal__toolbar label { font-size: .72rem; font-weight: 600; }
.passive-modal__toolbar button { width: auto; margin: 0; padding: 0; border: 0; background: transparent; color: var(--pico-muted-color); font-size: .65rem; }

.passive-modal__search { display: flex; align-items: center; gap: .45rem; margin-top: .45rem; padding: 0 .6rem; border: 1px solid var(--pico-muted-border-color); border-radius: var(--pico-border-radius); color: var(--pico-muted-color); }
.passive-modal__search input { height: 2.15rem; margin: 0; padding: 0; border: 0; background: transparent; box-shadow: none; font-size: .78rem; }

.passive-modal__list { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: .5rem 1.4rem; min-height: 0; margin-top: .7rem; overflow-y: auto; padding: .15rem .25rem .15rem .15rem; }
.passive-option { display: grid; grid-template-columns: minmax(0, 1fr) auto; align-items: center; gap: .35rem; width: 100%; min-height: 30px; margin: 0; padding: .35rem .5rem; border: 1px solid var(--pico-muted-border-color); border-left: 3px solid var(--passive-rank-color); border-radius: 0; background: color-mix(in srgb, var(--passive-rank-color) 7%, var(--pico-card-background-color)); color: var(--pico-color); font-size: .66rem; font-weight: 600; text-align: left; }
.passive-option:hover { background: color-mix(in srgb, var(--passive-rank-color) 18%, var(--pico-card-background-color)); box-shadow: 0 0 10px color-mix(in srgb, var(--passive-rank-color) 25%, transparent); }
.passive-option.selected { border-color: #22c55e; border-left-color: #22c55e; background: color-mix(in srgb, #22c55e 13%, var(--pico-card-background-color)); box-shadow: 0 0 0 1px #22c55e; }
.passive-option.disabled { opacity: .5; cursor: not-allowed; }
.passive-option span { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.passive-option img, .passive-option i { width: 18px; height: 18px; object-fit: contain; color: var(--passive-rank-color); }
.passive-option__icons { display: flex; align-items: center; gap: .2rem; }
.passive-option.selected .passive-option__icons .mdi-check { color: #22c55e; }
.rank-5, .rank-4 { --passive-rank-color: #42d9ff; }
.rank-3, .rank-2 { --passive-rank-color: #f5df39; }
.rank-1 { --passive-rank-color: #dceaf0; }
.rank--1, .rank--2, .rank--3 { --passive-rank-color: #ff4d63; }
.passive-modal__empty { grid-column: 1 / -1; margin: 1rem 0; color: var(--pico-muted-color); font-size: .72rem; text-align: center; }

footer { justify-content: flex-end; padding-top: 1rem; }
footer button { width: 110px; margin: 0; padding: .5rem; font-size: .72rem; font-weight: 700; }

.custom-scrollbar::-webkit-scrollbar { width: 4px; }
.custom-scrollbar::-webkit-scrollbar-track { background: transparent; }
.custom-scrollbar::-webkit-scrollbar-thumb { background: var(--pico-muted-border-color); border-radius: 10px; }

.passive-modal-enter-active, .passive-modal-leave-active { transition: opacity .16s ease; }
.passive-modal-enter-active .passive-modal, .passive-modal-leave-active .passive-modal { transition: transform .16s ease; }
.passive-modal-enter-from, .passive-modal-leave-to { opacity: 0; }
.passive-modal-enter-from .passive-modal, .passive-modal-leave-to .passive-modal { transform: translateY(8px) scale(.98); }

@media (max-width: 520px) {
  .passive-modal__list { grid-template-columns: 1fr; }
}
</style>
