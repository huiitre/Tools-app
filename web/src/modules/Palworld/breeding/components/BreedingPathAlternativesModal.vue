<script setup lang="ts">
import { computed } from 'vue'
import type { PalworldPalListItem } from '../../paldex/types/paldex.types'
import type { BreedingPathNode, BreedingPathRoute } from '../types/breeding.types'

const props = defineProps<{
  open: boolean
  routes: BreedingPathRoute[]
  selectedRouteId: string | null
  resolvePal: (id: number) => PalworldPalListItem | null
}>()

const emit = defineEmits<{
  select: [routeId: string]
  close: []
}>()

const selectedRoute = computed(() => props.routes.find(route => route.id === props.selectedRouteId) ?? props.routes[0] ?? null)
const alternativeRoutes = computed(() => props.routes.filter(route => route.id !== selectedRoute.value?.id))

function collectPals(node: BreedingPathNode, ids = new Set<number>()) {
  ids.add(node.species.id)
  if (node.step) {
    collectPals(node.step.parentA, ids)
    collectPals(node.step.parentB, ids)
  }
  return [...ids]
}

function breedDifference(route: BreedingPathRoute) {
  return route.breeds - (selectedRoute.value?.breeds ?? route.breeds)
}

function differenceLabel(route: BreedingPathRoute) {
  const difference = breedDifference(route)
  return difference > 0 ? `+${difference}` : `${difference}`
}
</script>

<template>
  <Teleport to="body">
    <Transition name="alternatives-modal">
      <div v-if="open" class="alternatives-modal-backdrop" @mousedown.self="emit('close')">
        <section class="alternatives-modal" role="dialog" aria-modal="true" aria-label="Routes alternatives">
          <header>
            <div>
              <strong>Routes alternatives</strong>
              <small>Choisis une autre route pour l’afficher dans l’arbre.</small>
            </div>
            <button type="button" aria-label="Fermer" @click="emit('close')"><i class="mdi mdi-close" /></button>
          </header>

          <div class="alternatives-modal__list custom-scrollbar">
            <button
              v-for="route in alternativeRoutes"
              :key="route.id"
              type="button"
              class="route-option"
              @click="emit('select', route.id)"
            >
              <span class="route-option__heading">
                <strong :class="{ better: breedDifference(route) < 0 }">{{ differenceLabel(route) }}</strong>
                <small>accouplement{{ Math.abs(breedDifference(route)) > 1 ? 's' : '' }} · {{ route.breeds }} au total</small>
              </span>
              <span class="route-option__pals" :aria-label="`${collectPals(route.root).length} Pals dans cette route`">
                <img
                  v-for="palId in collectPals(route.root)"
                  :key="palId"
                  :src="resolvePal(palId)?.imageUrl ?? ''"
                  :alt="resolvePal(palId)?.name ?? ''"
                  :title="resolvePal(palId)?.name"
                >
              </span>
            </button>
            <p v-if="alternativeRoutes.length === 0">Aucune autre route retenue.</p>
          </div>
        </section>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped lang="scss">
.alternatives-modal-backdrop { position: fixed; z-index: 1100; inset: 0; display: grid; place-items: center; padding: 1rem; background: rgb(0 0 0 / 58%); backdrop-filter: blur(3px); }
.alternatives-modal { display: flex; flex-direction: column; width: min(540px, 100%); max-height: min(580px, calc(100vh - 2rem)); margin: 0; padding: 1rem; overflow: hidden; border: 1px solid var(--pico-muted-border-color); border-radius: 12px; background: var(--pico-card-background-color); box-shadow: 0 18px 50px rgb(0 0 0 / 42%); }
.alternatives-modal header { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; }
.alternatives-modal header strong, .alternatives-modal header small { display: block; }
.alternatives-modal header strong { font-size: .86rem; }
.alternatives-modal header small { margin-top: .2rem; color: var(--pico-muted-color); font-size: .66rem; }
.alternatives-modal header button { display: grid; place-items: center; width: 2rem; height: 2rem; margin: 0; padding: 0; border-color: var(--pico-muted-border-color); background: transparent; color: var(--pico-muted-color); }
.alternatives-modal__list { display: grid; gap: .55rem; min-height: 0; margin-top: 1rem; overflow-y: auto; padding-right: .2rem; }
.route-option { display: flex; align-items: center; justify-content: space-between; gap: 1rem; width: 100%; margin: 0; padding: .65rem; border-color: var(--pico-muted-border-color); background: color-mix(in srgb, var(--pico-card-background-color) 92%, var(--pico-muted-color)); color: var(--pico-color); text-align: left; }
.route-option:hover { border-color: var(--pico-primary); background: color-mix(in srgb, var(--pico-primary) 10%, var(--pico-card-background-color)); }
.route-option__heading { display: grid; gap: .12rem; flex: 0 0 auto; }
.route-option__heading strong { color: var(--pico-color); font-size: .85rem; }
.route-option__heading strong.better { color: #22c55e; }
.route-option__heading small { color: var(--pico-muted-color); font-size: .64rem; }
.route-option__pals { display: flex; justify-content: flex-end; min-width: 0; padding-left: .5rem; }
.route-option__pals img { width: 30px; height: 30px; margin-left: -.5rem; object-fit: contain; border: 1px solid var(--pico-primary); border-radius: 50%; background: #fff; }
.route-option__pals img:first-child { margin-left: 0; }
.alternatives-modal__list > p { margin: 1rem 0; color: var(--pico-muted-color); font-size: .72rem; text-align: center; }
.alternatives-modal-enter-active, .alternatives-modal-leave-active { transition: opacity .15s ease; }
.alternatives-modal-enter-from, .alternatives-modal-leave-to { opacity: 0; }
</style>
