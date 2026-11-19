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
          :class="{ active: isList }"
        >
          <span class="nav-icon mdi mdi-lightbulb-outline"></span>
          Ateliers
        </RouterLink>

        <RouterLink
          :to="{ name: 'dofus-workshop-archive' }"
          class="nav-item"
          :class="{ active: isArchive }"
        >
          <span class="nav-icon mdi mdi-archive-outline"></span>
          Archive
        </RouterLink>

        <a
          href="#"
          class="nav-item"
          @click.prevent="showEditor = true"
        >
          <span class="nav-icon mdi mdi-tag-multiple-outline"></span>
          Modifier les tags
        </a>

        <div class="nav-section">
          <span class="nav-label">Épinglés</span>
          <RouterLink
            v-for="workshop in pinnedWorkshops"
            :key="workshop.id"
            :to="{ name: 'dofus-workshop-detail', params: { workshopId: workshop.id } }"
            class="nav-item pinned-item"
            :class="{ active: activeWorkshopId === workshop.id }"
            :title="workshop.name"
          >
            <span class="mdi mdi-pin pinned-icon"></span>
            <span class="pinned-name">{{ workshop.name }}</span>
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
  padding: 1rem 0;
  border-right: 1px solid var(--pico-muted-border-color);
  background:
    linear-gradient(180deg, color-mix(in srgb, var(--pico-primary) 6%, transparent), transparent 220px),
    var(--pico-background-color);
}

nav {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}

.nav-item {
  position: relative;
  display: flex;
  align-items: center;
  gap: 0.65rem;
  margin: 0 0.6rem;
  padding: 0.45rem 0.6rem;
  border-radius: 10px;
  font-size: 0.88rem;
  color: var(--pico-color);
  text-decoration: none;
  transition: background 0.15s, color 0.15s;

  &:hover {
    background: color-mix(in srgb, var(--pico-contrast) 7%, transparent);
  }

  &.active {
    background: color-mix(in srgb, var(--pico-primary) 16%, transparent);
    color: var(--pico-primary);
    font-weight: 600;

    //* Liseré d'accent à gauche de l'entrée active.
    &::before {
      content: '';
      position: absolute;
      left: -0.6rem;
      top: 22%;
      bottom: 22%;
      width: 3px;
      border-radius: 0 3px 3px 0;
      background: var(--pico-primary);
    }

    .nav-icon {
      background: var(--pico-primary);
      color: var(--pico-primary-inverse);
    }
  }
}

.nav-icon {
  display: inline-grid;
  place-items: center;
  flex-shrink: 0;
  width: 1.75rem;
  height: 1.75rem;
  border-radius: 8px;
  background: color-mix(in srgb, var(--pico-contrast) 7%, transparent);
  font-size: 1rem;
  transition: background 0.15s, color 0.15s;
}

.nav-section {
  display: flex;
  flex-direction: column;
  gap: 0.1rem;
  margin-top: 1rem;
}

.nav-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.4rem 1.2rem;
  font-size: 0.68rem;
  font-weight: 600;
  color: var(--pico-muted-color);
  text-transform: uppercase;
  letter-spacing: 0.08em;

  &::after {
    content: '';
    flex: 1;
    height: 1px;
    background: var(--pico-muted-border-color);
  }
}

.pinned-item {
  padding: 0.35rem 0.6rem;
  font-size: 0.82rem;
}

.pinned-icon {
  flex-shrink: 0;
  font-size: 0.8rem;
  color: var(--pico-muted-color);
}

.pinned-item.active .pinned-icon {
  color: var(--pico-primary);
}

.pinned-name {
  min-width: 0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
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

      &::before {
        display: none;
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
</style>
