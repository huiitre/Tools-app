<script setup lang="ts">
import { computed, ref } from 'vue'
import { useWorkshopDetailStore } from '@/modules/Dofus/workshop/store/workshopDetail.store'
import { getItemImageByResolution } from '@/modules/Dofus/item/utils/itemImageSelector'
import { AssetResolution } from '@/modules/Dofus/item/types/assetResolution.enum'

//* Colonne flottante des objets principaux de l'atelier (pas les sous-crafts), toujours visible.
//* Clic sur un objet → scroll jusqu'à sa carte (attribut data-workshop-item-id posé par WorkshopItemsList).

type Entry = { id: number; name: string; type: string; level: number; image: string }

const store = useWorkshopDetailStore()

const entries = computed<Entry[]>(() =>
  store.items.map(workshopItem => ({
    id: workshopItem.id,
    name: workshopItem.item.name,
    type: workshopItem.item.type?.name ?? '',
    level: workshopItem.item.level,
    image: getItemImageByResolution(workshopItem.item.images ?? [], AssetResolution.X2)?.url ?? '',
  }))
)

//* Encart de l'objet survolé : en position fixe, calé sur l'entrée, pour n'élargir que lui et ne pas
//* être coupé par le scroll de la colonne.
const hovered = ref<{ entry: Entry; top: number; right: number } | null>(null)

const onEnter = (entry: Entry, event: MouseEvent) => {
  const rect = (event.currentTarget as HTMLElement).getBoundingClientRect()
  hovered.value = {
    entry,
    top: rect.top + rect.height / 2,
    right: window.innerWidth - rect.right,
  }
}

const scrollTo = (workshopItemId: number) => {
  document
    .querySelector(`[data-workshop-item-id="${workshopItemId}"]`)
    ?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}
</script>

<template>
  <nav v-if="entries.length" class="item-navigator" @scroll="hovered = null">
    <button
      v-for="entry in entries"
      :key="entry.id"
      type="button"
      class="navigator-entry"
      :aria-label="entry.name"
      @mouseenter="onEnter(entry, $event)"
      @mouseleave="hovered = null"
      @click="scrollTo(entry.id)"
    >
      <img :src="entry.image" :alt="entry.name" />
    </button>
  </nav>

  <div
    v-if="hovered"
    class="navigator-hover"
    :style="{ top: `${hovered.top}px`, right: `${hovered.right}px` }"
  >
    <span class="hover-label">
      <strong>{{ hovered.entry.name }}</strong>
      <small>{{ hovered.entry.type }} · Niv {{ hovered.entry.level }}</small>
    </span>
    <img :src="hovered.entry.image" :alt="hovered.entry.name" />
  </div>
</template>

<style scoped>
.item-navigator {
  position: fixed;
  top: 50%;
  right: 1rem;
  transform: translateY(-50%);
  z-index: 900;
  max-height: 80vh;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  padding: 0.3rem;
  background: var(--pico-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  box-shadow: var(--pico-card-box-shadow);
}

.navigator-entry {
  display: grid;
  place-items: center;
  margin: 0;
  padding: 0.2rem;
  width: auto;
  height: auto;
  background: transparent;
  border: 1px solid transparent;
  border-radius: var(--pico-border-radius);
  cursor: pointer;

  img {
    width: 28px;
    height: 28px;
    object-fit: contain;
  }
}

/* Recouvre exactement l'entrée survolée et s'étend vers la gauche avec le libellé. */
.navigator-hover {
  position: fixed;
  z-index: 901;
  transform: translateY(-50%);
  pointer-events: none;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.2rem 0.2rem 0.2rem 0.5rem;
  background: var(--pico-card-sectioning-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  box-shadow: var(--pico-card-box-shadow);
  color: var(--pico-color);
  white-space: nowrap;

  img {
    width: 28px;
    height: 28px;
    object-fit: contain;
  }
}

.hover-label {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  line-height: 1.2;

  strong {
    font-size: 0.75rem;
  }

  small {
    font-size: 0.65rem;
    color: var(--pico-muted-color);
  }
}
</style>
