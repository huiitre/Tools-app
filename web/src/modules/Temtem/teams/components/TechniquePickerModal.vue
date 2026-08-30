<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useTemtemdexStore } from '@/modules/Temtem/temtemdex/temtemdex.store'
import type { TemtemCategory, TemtemLearnedTechnique } from '@/modules/Temtem/shared/types/temtem.types'
import type { TemtemTeamMember } from '@/modules/Temtem/teams/types/teams.types'

const props = defineProps<{
  member: TemtemTeamMember
}>()

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'save', techniqueIds: number[]): void
}>()

const MAX_TECHNIQUES = 4

const SOURCE_ORDER = ['LEVEL', 'BREEDING', 'TRAINING']
const SOURCE_LABELS: Record<string, string> = {
  LEVEL: 'Par niveau',
  BREEDING: 'Par élevage',
  TRAINING: 'Par entraînement',
}

const dexStore = useTemtemdexStore()

const learned = ref<TemtemLearnedTechnique[]>([])
const loading = ref(true)
const failed = ref(false)
const query = ref('')
const pendingIds = ref<number[]>(props.member.techniques.map(technique => technique.id))

onMounted(async () => {
  try {
    const detail = await dexStore.ensureDetail(props.member.temtem.slug)
    learned.value = detail.techniques
  } catch {
    failed.value = true
  } finally {
    loading.value = false
  }
})

const atLimit = computed(() => pendingIds.value.length === MAX_TECHNIQUES)

const isSelected = (techniqueId: number) => pendingIds.value.includes(techniqueId)

// Arrivé à quatre, les autres options sont grisées et inertes : rien à refuser, donc rien à
// signaler. Il faut décocher pour reprendre la main.
const isDisabled = (techniqueId: number) => atLimit.value && !isSelected(techniqueId)

function toggle(techniqueId: number) {
  if (isSelected(techniqueId)) {
    pendingIds.value = pendingIds.value.filter(id => id !== techniqueId)
    return
  }
  if (atLimit.value) return
  pendingIds.value = [...pendingIds.value, techniqueId]
}

// Un même couple (Temtem, technique) apparaît deux fois quand elle s'apprend par deux moyens :
// on ne propose qu'une entrée par technique dans chaque groupe.
const groups = computed(() => {
  const needle = query.value.trim().toLocaleLowerCase()
  const bySource = new Map<string, TemtemLearnedTechnique[]>()

  for (const entry of learned.value) {
    if (needle && !entry.technique.name.toLocaleLowerCase().includes(needle)) continue

    const list = bySource.get(entry.source) ?? []
    if (!list.some(existing => existing.technique.id === entry.technique.id)) list.push(entry)
    bySource.set(entry.source, list)
  }

  return [...bySource.entries()]
    .sort(([a], [b]) => SOURCE_ORDER.indexOf(a) - SOURCE_ORDER.indexOf(b))
    .map(([source, entries]) => ({ source, label: SOURCE_LABELS[source] ?? source, entries }))
})

const isEmpty = computed(() => groups.value.every(group => group.entries.length === 0))

const CATEGORY_ORDER = ['PHYSICAL', 'SPECIAL', 'STATUS']

const legend = computed(() => {
  const categories = new Map<string, TemtemCategory>()
  for (const entry of learned.value) categories.set(entry.technique.category.code, entry.technique.category)
  return [...categories.values()].sort((a, b) => CATEGORY_ORDER.indexOf(a.code) - CATEGORY_ORDER.indexOf(b.code))
})

</script>

<template>
  <Teleport to="body">
    <Transition name="technique-modal" appear>
      <div class="technique-modal-backdrop" @mousedown.self="emit('close')">
        <section class="technique-modal" role="dialog" aria-modal="true" aria-label="Choisir les techniques">
          <header class="technique-modal__header">
            <div>
              <strong>Techniques de {{ member.temtem.name }}</strong>
              <small>Retiens jusqu’à {{ MAX_TECHNIQUES }} techniques parmi celles qu’il apprend.</small>
            </div>
            <div class="technique-modal__actions">
              <span>{{ pendingIds.length }}/{{ MAX_TECHNIQUES }}</span>
              <button type="button" aria-label="Fermer" @click="emit('close')">
                <i class="mdi mdi-close" />
              </button>
            </div>
          </header>

          <ul v-if="legend.length" class="technique-modal__legend">
            <li v-for="category in legend" :key="category.code">
              <img v-if="category.imageUrl" :src="category.imageUrl" alt="" aria-hidden="true">
              {{ category.label }}
            </li>
          </ul>

          <div class="technique-modal__toolbar">
            <label for="technique-search">Rechercher une technique</label>
            <button type="button" :disabled="pendingIds.length === 0" @click="pendingIds = []">Effacer</button>
          </div>

          <div class="technique-modal__search">
            <i class="mdi mdi-magnify" aria-hidden="true" />
            <input id="technique-search" v-model="query" type="search" placeholder="Rechercher une technique">
          </div>

          <div class="technique-modal__list custom-scrollbar">
            <p v-if="loading" class="technique-modal__empty">Chargement des techniques…</p>
            <p v-else-if="failed" class="technique-modal__empty">Impossible de charger les techniques de ce Temtem.</p>
            <p v-else-if="isEmpty" class="technique-modal__empty">Aucune technique correspondante.</p>

            <template v-else>
              <template v-for="group in groups" :key="group.source">
                <p class="technique-modal__group">
                  {{ group.label }}
                </p>

                <button
                  v-for="entry in group.entries"
                  :key="entry.technique.id"
                  type="button"
                  class="technique-option"
                  :class="{ selected: isSelected(entry.technique.id), disabled: isDisabled(entry.technique.id) }"
                  :title="entry.technique.effect ?? entry.technique.name"
                  @click="toggle(entry.technique.id)"
                >
                  <img
                    v-if="entry.technique.type.imageUrl"
                    class="technique-option__type"
                    :src="entry.technique.type.imageUrl"
                    :alt="entry.technique.type.name"
                    :title="entry.technique.type.name"
                  >
                  <span class="technique-option__name">
                    {{ entry.technique.name }}
                    <small v-if="entry.level">Nv. {{ entry.level }}</small>
                  </span>

                  <span class="technique-option__stats">
                    <!-- Étiquetés : un « 32 » et un « 4 » nus ne se distinguent pas. -->
                    <span class="stat atk" :title="entry.technique.damage === null ? 'Technique de statut, aucun dégât' : 'Dégâts de base'">
                      ATQ {{ entry.technique.damage ?? '—' }}
                    </span>
                    <span class="stat end" title="Coût en endurance">
                      END {{ entry.technique.stamina ?? '—' }}
                    </span>
                  </span>

                  <span class="technique-option__icons">
                    <img
                      v-if="entry.technique.category.imageUrl"
                      :src="entry.technique.category.imageUrl"
                      :alt="entry.technique.category.label"
                      :title="entry.technique.category.label"
                    >
                    <i v-if="isSelected(entry.technique.id)" class="mdi mdi-check" aria-hidden="true" />
                  </span>
                </button>
              </template>
            </template>
          </div>

          <footer>
            <button type="button" :disabled="loading || failed" @click="emit('save', pendingIds)">Valider</button>
          </footer>
        </section>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped lang="scss">
.technique-modal-backdrop {
  position: fixed;
  z-index: 1100;
  inset: 0;
  display: grid;
  place-items: center;
  padding: 1rem;
  background: rgb(0 0 0 / 58%);
  backdrop-filter: blur(3px);
}

.technique-modal {
  display: flex;
  flex-direction: column;
  width: min(620px, 100%);
  max-height: min(680px, calc(100vh - 2rem));
  padding: 1rem;
  margin: 0;
  overflow: hidden;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: 12px;
  box-shadow: 0 18px 50px rgb(0 0 0 / 42%);
}

.technique-modal__header,
.technique-modal__actions,
.technique-modal__toolbar,
footer {
  display: flex;
  align-items: center;
}

.technique-modal__header {
  justify-content: space-between;
  gap: 1rem;

  strong,
  small { display: block; }
  strong { font-size: .86rem; }
  small { margin-top: .2rem; color: var(--pico-muted-color); font-size: .66rem; }
}

.technique-modal__actions { gap: .65rem; }
.technique-modal__actions span { padding: .15rem .42rem; border-radius: 999px; background: color-mix(in srgb, var(--pico-primary) 16%, transparent); color: var(--pico-primary); font-size: .65rem; font-weight: 700; }
.technique-modal__actions button { display: grid; place-items: center; width: 2rem; height: 2rem; margin: 0; padding: 0; border-color: var(--pico-muted-border-color); background: transparent; color: var(--pico-muted-color); }

.technique-modal__legend {
  display: flex;
  flex-wrap: wrap;
  gap: .4rem;
  margin: .7rem 0 0;
  padding: 0;
  list-style: none;
}

.technique-modal__legend li {
  display: inline-flex;
  align-items: center;
  gap: .3rem;
  color: var(--pico-muted-color);
  font-size: .62rem;
  font-weight: 700;
}

.technique-modal__legend img { width: 14px; height: 14px; object-fit: contain; }

.technique-modal__toolbar { justify-content: space-between; margin-top: .8rem; }
.technique-modal__toolbar label { font-size: .72rem; font-weight: 600; }
.technique-modal__toolbar button { width: auto; margin: 0; padding: 0; border: 0; background: transparent; color: var(--pico-muted-color); font-size: .65rem; }

.technique-modal__search { display: flex; align-items: center; gap: .45rem; margin-top: .45rem; padding: 0 .6rem; border: 1px solid var(--pico-muted-border-color); border-radius: var(--pico-border-radius); color: var(--pico-muted-color); }
.technique-modal__search input { height: 2.15rem; margin: 0; padding: 0; border: 0; background: transparent; box-shadow: none; font-size: .78rem; }

.technique-modal__list { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: .5rem 1.4rem; min-height: 0; margin-top: .7rem; overflow-y: auto; padding: .15rem .25rem .15rem .15rem; }

.technique-modal__group {
  grid-column: 1 / -1;
  margin: .35rem 0 0;
  color: var(--pico-muted-color);
  font-size: .62rem;
  font-weight: 700;
  letter-spacing: .08em;
  text-transform: uppercase;
}

.technique-option {
  display: grid;
  grid-template-columns: 18px minmax(0, 1fr) auto auto;
  align-items: center;
  gap: .4rem;
  width: 100%;
  min-height: 30px;
  margin: 0;
  padding: .35rem .5rem;
  border: 1px solid var(--pico-muted-border-color);
  border-left: 1px solid var(--pico-muted-border-color);
  border-radius: 0;
  background: var(--pico-card-background-color);
  color: inherit;
  font-size: .66rem;
  font-weight: 600;
  text-align: left;
}

.technique-option:hover { background: var(--pico-card-sectioning-background-color); }
.technique-option.selected { border-color: #22c55e; box-shadow: 0 0 0 1px #22c55e; }
.technique-option.disabled { opacity: .5; cursor: not-allowed; }

.technique-option__type { width: 18px; height: 18px; object-fit: contain; }

.technique-option__name {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;

  small { margin-left: .3rem; color: var(--pico-muted-color); font-size: .6rem; font-weight: 500; }
}

.technique-option__stats { display: flex; gap: .35rem; font-size: .6rem; font-weight: 700; font-variant-numeric: tabular-nums; }
.technique-option__stats .stat { padding: .1rem .28rem; border-radius: 3px; background: color-mix(in srgb, var(--pico-color) 8%, transparent); }
.technique-option__stats .atk { color: #f5934d; }
.technique-option__stats .end { color: #4dc3f5; }

.technique-option__icons { display: flex; align-items: center; gap: .2rem; }
.technique-option__icons img, .technique-option__icons i { width: 18px; height: 18px; object-fit: contain; }
.technique-option.selected .technique-option__icons .mdi-check { color: #22c55e; }

.technique-modal__empty { grid-column: 1 / -1; margin: 1rem 0; color: var(--pico-muted-color); font-size: .72rem; text-align: center; }

footer { justify-content: flex-end; padding-top: 1rem; }
footer button { width: 110px; margin: 0; padding: .5rem; font-size: .72rem; font-weight: 700; }

.custom-scrollbar::-webkit-scrollbar { width: 4px; }
.custom-scrollbar::-webkit-scrollbar-track { background: transparent; }
.custom-scrollbar::-webkit-scrollbar-thumb { background: var(--pico-muted-border-color); border-radius: 10px; }

.technique-modal-enter-active, .technique-modal-leave-active { transition: opacity .16s ease; }
.technique-modal-enter-active .technique-modal, .technique-modal-leave-active .technique-modal { transition: transform .16s ease; }
.technique-modal-enter-from, .technique-modal-leave-to { opacity: 0; }
.technique-modal-enter-from .technique-modal, .technique-modal-leave-to .technique-modal { transform: translateY(8px) scale(.98); }

@media (max-width: 560px) {
  .technique-modal__list { grid-template-columns: 1fr; }
}
</style>
