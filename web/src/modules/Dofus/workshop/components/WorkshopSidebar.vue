<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useWorkshopStore } from '@/modules/Dofus/workshop/store/workshop.store'
import WorkshopTagEditor from './WorkshopTagEditor.vue'
import { useRoute } from 'vue-router'

const store = useWorkshopStore()

const route = useRoute()

const pinnedWorkshops = computed(() => store.workshops.filter(w => w.pinned))

const isList = computed(() =>
  route.name === 'dofus-workshop-list'
)

const isArchive = computed(() =>
  route.name === 'dofus-workshop-archive'
)

const activeTagId = computed(() =>
  route.name === 'dofus-workshop-tag'
    ? Number(route.params.tagId)
    : null
)

const activeWorkshopId = computed(() =>
  route.name === 'dofus-workshop-detail'
    ? Number(route.params.workshopId)
    : null
)

const showEditor = ref(false)

//* Sur le détail d'un atelier, la sidebar est repliée sur l'ampoule pour laisser la place au contenu ;
//* au survol, elle s'ouvre en entier par-dessus la page, sans la pousser.
const collapsible = computed(() => route.name === 'dofus-workshop-detail')

const expanded = ref(false)
let closeTimer: ReturnType<typeof setTimeout> | null = null

const cancelClose = () => {
  if (closeTimer) clearTimeout(closeTimer)
  closeTimer = null
}

const open = () => {
  if (!collapsible.value) return
  cancelClose()
  expanded.value = true
}

//* Léger délai : la souris qui dépasse d'un pixel ne referme pas la sidebar.
const scheduleClose = () => {
  if (!collapsible.value) return
  cancelClose()
  closeTimer = setTimeout(() => { expanded.value = false }, 150)
}

watch(() => route.fullPath, () => {
  cancelClose()
  expanded.value = false
})

onBeforeUnmount(cancelClose)
</script>

<template>
  <div class="sidebar-host" :class="{ rail: collapsible }">
    <aside
      class="sidebar"
      :class="{ collapsed: collapsible && !expanded, expanded: collapsible && expanded }"
      @mouseenter="open"
      @mouseleave="scheduleClose"
    >
      <nav>
        <RouterLink
          :to="{ name: 'dofus-workshop-list' }"
          class="nav-item"
          :class="{ active: isList && activeTagId === null }"
        >
          <span class="mdi mdi-lightbulb-outline"></span>
          Ateliers
        </RouterLink>

        <div class="nav-section" v-if="store.tags.length">
          <span class="nav-label">Tags</span>

          <RouterLink
            v-for="tag in store.tags"
            :key="tag.id"
            :to="{ name: 'dofus-workshop-tag', params: { tagId: tag.id } }"
            class="nav-item tag-item"
            :class="{ active: activeTagId === tag.id }"
          >
            <i class="tag-color mdi mdi-tag icon" :style="{ color: tag.color }"></i>
            {{ tag.name }}
          </RouterLink>
        </div>

        <RouterLink
          :to="{ name: 'dofus-workshop-archive' }"
          class="nav-item"
          :class="{ active: isArchive }"
        >
          <span class="mdi mdi-archive"></span>
          Archive
        </RouterLink>

        <a
          href="#"
          class="nav-item"
          @click.prevent="showEditor = true"
        >
          <span class="mdi mdi-pencil"></span>
          Modifier les tags
        </a>

        <div class="nav-section">
          <span class="nav-label">ÉPINGLÉS</span>
          <RouterLink
            v-for="workshop in pinnedWorkshops"
            :key="workshop.id"
            :to="{ name: 'dofus-workshop-detail', params: { workshopId: workshop.id } }"
            class="nav-item"
            :class="{ active: activeWorkshopId === workshop.id }"
          >
            {{ workshop.name }}
          </RouterLink>
        </div>

      </nav>

      <WorkshopTagEditor
        v-if="showEditor"
        @close="showEditor = false"
      />
    </aside>
  </div>
</template>

<style lang="scss" scoped>
.sidebar {
  width: 220px;
  height: 100%;
  flex-shrink: 0;
  border-right: 1px solid var(--pico-muted-border-color);
  padding: 1rem 0;
  background: var(--pico-background-color);
}

.nav-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem 1.5rem;
  text-decoration: none;
  border-radius: 0 1.5rem 1.5rem 0;
  margin: 0.25rem 0.5rem 0.25rem 0;
  transition: background 0.2s;

  &:hover {
    background: var(--pico-muted-border-color);
  }

  &.active {
    background: var(--pico-primary-background);
    color: var(--pico-primary-inverse);
    font-weight: 500;
  }

  &.tag-item {
    padding-left: 2.5rem;
    font-size: 0.9rem;
  }
}

.nav-section {
  margin: 1rem 0;
}

/* Hors détail d'atelier, le conteneur est transparent : la mise en page ne change pas. */
.sidebar-host {
  display: contents;
}

/* Détail d'atelier : colonne fine qui réserve la place ; la sidebar collée en haut la déborde au survol. */
.sidebar-host.rail {
  display: block;
  width: 56px;
  flex-shrink: 0;
  border-right: 1px solid var(--pico-muted-border-color);

  .sidebar {
    position: sticky;
    top: 0;
    z-index: 950;
    height: auto;
    border-right: none;
    white-space: nowrap;
  }

  .sidebar.collapsed {
    width: 56px;
    overflow: hidden;
    background: transparent;

    /* Seule l'ampoule du lien « Ateliers » reste visible. */
    nav > :not(:first-child) {
      display: none;
    }

    /* Texte « Ateliers » masqué ; l'icône garde sa taille et sa place, pour ne pas bouger à l'ouverture. */
    nav > :first-child {
      font-size: 0;

      .mdi {
        font-size: 1rem;
      }
    }
  }

  .sidebar.expanded {
    width: 220px;
    max-height: calc(100vh - 4rem);
    overflow-y: auto;
    border-right: 1px solid var(--pico-muted-border-color);
    box-shadow: var(--pico-card-box-shadow);
  }
}

.nav-label {
  display: block;
  padding: 0.5rem 1.5rem;
  font-size: 0.75rem;
  font-weight: 500;
  color: var(--pico-muted-color);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.tag-color {
}
</style>
