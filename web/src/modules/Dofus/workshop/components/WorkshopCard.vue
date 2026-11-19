<script setup lang="ts">
import { computed } from 'vue'
import type { Workshop, WorkshopItem, WorkshopLink } from '@/modules/Dofus/workshop/types/workshop.types'
import { useWorkshopPriceCalculator } from '@/modules/Dofus/workshop/composables/useWorkshopPriceCalculator'
import { workshopCardStats } from '@/modules/Dofus/workshop/utils/workshopCard'
import { getItemImageByResolution } from '@/modules/Dofus/item/utils/itemImageSelector'
import { AssetResolution } from '@/modules/Dofus/item/types/assetResolution.enum'
import { getContrastColor } from '@/utils/color'

//* Nombre d'objets montrés en icône avant le « +N ».
const MAX_VISIBLE_ITEMS = 12

const props = defineProps<{
  workshop: Workshop
  isNew: boolean
}>()

const emit = defineEmits<{
  (e: 'open'): void
  (e: 'pin'): void
  (e: 'edit'): void
  (e: 'archive'): void
  (e: 'remove'): void
  (e: 'edit-tags'): void
  (e: 'open-link', link: WorkshopLink): void
}>()

const { getUnitPrice } = useWorkshopPriceCalculator()

const stats = computed(() => workshopCardStats(props.workshop.items, getUnitPrice))

const levelLabel = computed(() => {
  const { minLevel, maxLevel } = stats.value
  if (minLevel === null) return null
  return minLevel === maxLevel ? `Niv. ${minLevel}` : `Niv. ${minLevel} – ${maxLevel}`
})

//* Une seule classe : grande tête et décor de bannière ; plusieurs : petites têtes côte à côte.
const heroBreed = computed(() => props.workshop.breeds.length === 1 ? props.workshop.breeds[0] : null)

const visibleItems = computed(() => props.workshop.items.slice(0, MAX_VISIBLE_ITEMS))
const hiddenItemCount = computed(() => props.workshop.items.length - visibleItems.value.length)

const itemImage = (workshopItem: WorkshopItem) =>
  getItemImageByResolution(workshopItem.item.images ?? [], AssetResolution.X2)?.url

const kamas = (value: number) => `${value.toLocaleString('fr-FR')} ₭`
</script>

<template>
  <article
    class="workshop-card"
    :class="{ 'is-new': isNew, 'is-pinned': workshop.pinned }"
    @click="emit('open')"
  >
    <span v-if="isNew" class="new-badge">Nouveau</span>

    <!-- BANNIÈRE -->
    <header class="banner">
      <img
        v-if="heroBreed?.iconUrl"
        :src="heroBreed.iconUrl"
        class="banner-backdrop"
        alt=""
        aria-hidden="true"
      >

      <div v-if="workshop.breeds.length" class="banner-breeds" :class="{ single: !!heroBreed }">
        <img
          v-for="breed in workshop.breeds"
          :key="breed.id"
          :src="breed.iconUrl ?? undefined"
          :alt="breed.name"
          :title="breed.name"
        >
      </div>

      <div class="banner-main">
        <h3 :title="workshop.name">{{ workshop.name }}</h3>

        <div class="banner-tags">
          <img
            v-for="job in workshop.jobs"
            :key="`job-${job.id}`"
            :src="job.iconUrl ?? undefined"
            :alt="job.name"
            :title="job.name"
            class="job-icon"
          >
          <span
            v-for="tag in workshop.tags"
            :key="tag.id"
            class="tag"
            :style="{ background: tag.color, color: getContrastColor(tag.color) }"
          >
            {{ tag.name }}
          </span>
          <button
            type="button"
            class="tag-add"
            title="Classes, métiers et tags"
            @click.stop="emit('edit-tags')"
          >
            <i class="mdi mdi-plus" />
          </button>
        </div>
      </div>

      <button
        type="button"
        class="icon-btn pin"
        :class="{ active: workshop.pinned }"
        :title="workshop.pinned ? 'Désépingler' : 'Épingler'"
        @click.stop="emit('pin')"
      >
        <i class="mdi" :class="workshop.pinned ? 'mdi-pin' : 'mdi-pin-outline'" />
      </button>
    </header>

    <!-- CHIFFRES -->
    <div class="stats">
      <span v-if="levelLabel" class="stat">
        <i class="mdi mdi-star-four-points-outline" />
        {{ levelLabel }}
      </span>
      <span class="stat">
        <i class="mdi mdi-cube-outline" />
        {{ stats.itemCount }} objet{{ stats.itemCount > 1 ? 's' : '' }}
      </span>
      <span
        v-if="stats.itemCount"
        class="stat price"
        title="Meilleur prix ajusté / meilleur prix total"
      >
        <strong>{{ kamas(stats.adjusted) }}</strong>
        <span class="price-total">/ {{ kamas(stats.total) }}</span>
      </span>
    </div>

    <!-- OBJETS -->
    <div v-if="workshop.items.length" class="items">
      <div
        v-for="workshopItem in visibleItems"
        :key="workshopItem.id"
        class="item-tile"
        :title="`${workshopItem.item.name} · niv. ${workshopItem.item.level}`"
      >
        <img v-if="itemImage(workshopItem)" :src="itemImage(workshopItem)" :alt="workshopItem.item.name" loading="lazy">
      </div>
      <div v-if="hiddenItemCount > 0" class="item-tile more">+{{ hiddenItemCount }}</div>
    </div>
    <p v-else class="items-empty">Aucun objet pour l'instant</p>

    <!-- PIED -->
    <footer class="card-footer">
      <div class="links" @click.stop>
        <button
          v-for="link in workshop.links"
          :key="link.id"
          type="button"
          class="link"
          :title="link.url"
          @click="emit('open-link', link)"
        >
          <i class="mdi mdi-link-variant" />
          {{ link.label }}
        </button>
      </div>

      <div class="actions">
        <button type="button" class="icon-btn" title="Modifier" @click.stop="emit('edit')">
          <i class="mdi mdi-pencil-outline" />
        </button>
        <button
          type="button"
          class="icon-btn"
          :title="workshop.active ? 'Archiver' : 'Désarchiver'"
          @click.stop="emit('archive')"
        >
          <i class="mdi" :class="workshop.active ? 'mdi-archive-outline' : 'mdi-archive-arrow-up-outline'" />
        </button>
        <button type="button" class="icon-btn danger" title="Supprimer" @click.stop="emit('remove')">
          <i class="mdi mdi-delete-outline" />
        </button>
      </div>
    </footer>
  </article>
</template>

<style scoped lang="scss">
.workshop-card {
  position: relative;
  display: flex;
  flex-direction: column;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 12px;
  overflow: hidden;
  cursor: pointer;
  transition: border-color 0.2s, box-shadow 0.2s, transform 0.2s;

  &:hover {
    border-color: var(--pico-primary-border);
    box-shadow: 0 6px 24px color-mix(in srgb, var(--pico-primary) 18%, transparent);
    transform: translateY(-2px);

    .card-footer .actions {
      opacity: 1;
    }
  }

  &.is-pinned {
    border-color: color-mix(in srgb, var(--pico-primary) 45%, var(--pico-card-border-color));
  }

  &.is-new {
    border-color: #fbbf24;
    box-shadow: 0 0 0.75rem color-mix(in srgb, #fbbf24 45%, transparent);
    overflow: visible;
  }
}

.new-badge {
  position: absolute;
  top: -0.6rem;
  right: 0.75rem;
  z-index: 2;
  padding: 0.1rem 0.5rem;
  border-radius: 999px;
  background: #fbbf24;
  color: #000;
  font-size: 0.65rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

/* BANNIÈRE */
.banner {
  position: relative;
  display: flex;
  align-items: center;
  gap: 0.75rem;
  min-height: 72px;
  padding: 0.75rem 0.75rem 0.75rem 0.85rem;
  overflow: hidden;
  border-radius: 12px 12px 0 0;
  background:
    linear-gradient(100deg, color-mix(in srgb, var(--pico-primary) 22%, transparent), transparent 70%),
    color-mix(in srgb, var(--pico-contrast) 4%, var(--pico-card-background-color));
  border-bottom: 1px solid var(--pico-card-border-color);
}

//* Décor : la tête de la classe, très agrandie et estompée, à droite de la bannière.
.banner-backdrop {
  position: absolute;
  right: -1.5rem;
  top: 50%;
  width: 150px;
  height: 150px;
  transform: translateY(-50%);
  opacity: 0.18;
  filter: blur(1px);
  pointer-events: none;
}

.banner-breeds {
  position: relative;
  display: flex;
  flex-wrap: wrap;
  gap: 0.15rem;
  max-width: 64px;
  flex-shrink: 0;

  img {
    width: 30px;
    height: 30px;
    border-radius: 50%;
    background: color-mix(in srgb, var(--pico-contrast) 8%, transparent);
  }

  &.single img {
    width: 48px;
    height: 48px;
    box-shadow: 0 0 0 2px color-mix(in srgb, var(--pico-primary) 60%, transparent);
  }
}

.banner-main {
  position: relative;
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 0.35rem;

  h3 {
    margin: 0;
    font-size: 1.05rem;
    font-weight: 700;
    color: var(--pico-contrast);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
}

.banner-tags {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.3rem;
}

.job-icon {
  width: 22px;
  height: 22px;
}

.tag {
  display: inline-flex;
  align-items: center;
  height: 1.25rem;
  padding: 0 0.55rem;
  font-size: 0.7rem;
  font-weight: 600;
  border-radius: 999px;
  white-space: nowrap;
}

.tag-add {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 1.25rem;
  height: 1.25rem;
  margin: 0;
  padding: 0;
  border-radius: 50%;
  border: 1px dashed var(--pico-muted-border-color);
  background: transparent;
  color: var(--pico-muted-color);
  font-size: 0.8rem;
  cursor: pointer;

  &:hover {
    border-color: var(--pico-primary);
    color: var(--pico-primary);
  }
}

.icon-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 1.9rem;
  height: 1.9rem;
  margin: 0;
  padding: 0;
  border: none;
  border-radius: 8px;
  background: transparent;
  color: var(--pico-muted-color);
  font-size: 1.05rem;
  cursor: pointer;
  transition: background 0.15s, color 0.15s;

  &:hover {
    background: color-mix(in srgb, var(--pico-contrast) 10%, transparent);
    color: var(--pico-contrast);
  }

  &.danger:hover {
    color: #e53e3e;
  }
}

.pin {
  position: relative;
  align-self: flex-start;

  &.active {
    color: var(--pico-primary);
  }
}

/* CHIFFRES */
.stats {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
  padding: 0.65rem 0.85rem 0;
}

.stat {
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  padding: 0.2rem 0.55rem;
  border-radius: 6px;
  background: color-mix(in srgb, var(--pico-contrast) 6%, transparent);
  font-size: 0.75rem;
  color: var(--pico-muted-color);
  white-space: nowrap;

  i {
    font-size: 0.85rem;
  }

  &.price strong {
    color: var(--pico-primary);
    font-weight: 700;
  }
}

.price-total {
  opacity: 0.8;
}

/* OBJETS */
.items {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(40px, 1fr));
  gap: 4px;
  padding: 0.65rem 0.85rem;
}

.item-tile {
  aspect-ratio: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 6px;
  background: color-mix(in srgb, var(--pico-contrast) 7%, transparent);
  border: 1px solid color-mix(in srgb, var(--pico-contrast) 6%, transparent);

  img {
    width: 82%;
    height: 82%;
    object-fit: contain;
  }

  &.more {
    font-size: 0.75rem;
    font-weight: 700;
    color: var(--pico-muted-color);
  }
}

.items-empty {
  margin: 0;
  padding: 0.85rem;
  font-size: 0.8rem;
  font-style: italic;
  color: var(--pico-muted-color);
}

/* PIED */
.card-footer {
  margin-top: auto;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  padding: 0.35rem 0.5rem 0.45rem 0.85rem;
  border-top: 1px solid color-mix(in srgb, var(--pico-card-border-color) 70%, transparent);
}

.links {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  min-width: 0;
}

.link {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  max-width: 12rem;
  margin: 0;
  padding: 0;
  border: none;
  background: transparent;
  color: var(--pico-primary);
  font-size: 0.75rem;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  cursor: pointer;

  &:hover {
    text-decoration: underline;
  }
}

.actions {
  display: flex;
  gap: 0.15rem;
  opacity: 0.35;
  transition: opacity 0.15s;
}
</style>
