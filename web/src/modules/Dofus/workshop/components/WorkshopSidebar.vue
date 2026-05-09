<script setup lang="ts">
import { computed, ref } from 'vue'
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
</script>

<template>
  <aside class="sidebar">
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
</template>

<style lang="scss" scoped>
.sidebar {
  width: 250px;
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
