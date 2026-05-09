<script setup lang="ts">
import { ref, computed, watchEffect } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useWorkshopStore } from '@/modules/Dofus/workshop/store/workshop.store'
import { Workshop } from '@/modules/Dofus/workshop/types/workshop.types'
import WorkshopTagSelector from '@/modules/Dofus/workshop/components/WorkshopTagSelector.vue'
import WorkshopEditor from '@/modules/Dofus/workshop/components/WorkshopEditor.vue'
import WorkshopLinkViewer from '@/modules/Dofus/workshop/components/WorkshopLinkViewer.vue'
import { getContrastColor } from '@/utils/color'

const router = useRouter()
const store = useWorkshopStore()

const route = useRoute()

watchEffect(() => {
  if (route.name !== 'dofus-workshop-tag') return

  const tagId = Number(route.params.tagId)

  if (Number.isNaN(tagId)) {
    router.replace({ name: 'dofus-workshop-list' })
    return
  }

  //* tag pas encore chargé, on attend
  if (store.tags.length === 0) return

  const exists = store.tags.some(t => t.id === tagId)

  if (!exists) {
    router.replace({ name: 'dofus-workshop-list' })
  }
})

const selectedWorkshop = ref<Workshop | null>(null)
const editedWorkshop = ref<Workshop | null>(null)
const activeLink = ref<{ url: string; label: string } | null>(null)

  const workshops = computed(() => {
  // Archive
  if (route.name === 'dofus-workshop-archive') {
    return store.workshops.filter(w => !w.active)
  }

  // Tag
  if (route.name === 'dofus-workshop-tag') {
    const tagId = Number(route.params.tagId)
    return store.workshops.filter(
      w => w.active && w.tags.some(t => t.id === tagId)
    )
  }

  // Liste par défaut
  return store.workshops.filter(w => w.active)
})

/* =========================
   NAVIGATION
========================= */
const openWorkshop = (workshop: Workshop) => {
  router.push({
    name: 'dofus-workshop-detail',
    params: { workshopId: workshop.id }
  })
}

/* =========================
   ACTIONS
========================= */
const archive = async (id: number, workshop: Workshop) => {
  await store.updateWorkshop(id, {
    active: !workshop.active,
    name: workshop.name
  })
}

const remove = async (id: number) => {
  await store.deleteWorkshop(id)
}

const openTagSelector = (workshop: Workshop) => {
  selectedWorkshop.value = workshop
}

const openEditor = (workshop: Workshop) => {
  editedWorkshop.value = workshop
}

const pinWorkshop = async (workshop: Workshop) => {
  await store.updateWorkshop(workshop.id, {
    pinned: !workshop.pinned,
    name: workshop.name
  })
}
</script>

<template>
  <div class="workshops-grid">
    <article
      v-for="workshop in workshops"
      :key="workshop.id"
      class="workshop-card"
      @click="openWorkshop(workshop)"
    >
      <!-- HEADER -->
      <div class="card-header">
        <h3>{{ workshop.name }}</h3>

        <span
          class="mdi mdi-pin-outline pin action"
          :class="{ pinned: workshop.pinned }"
          title="Épingler"
          @click.stop="pinWorkshop(workshop)"
        ></span>
      </div>

      <!-- LINKS -->
      <div v-if="workshop.links?.length" class="card-links" @click.stop>
        <span
          v-for="link in workshop.links"
          :key="link.id"
          class="card-link"
          @click.stop="activeLink = { url: link.url, label: link.label }"
        >
          <span class="mdi mdi-link-variant"></span>
          {{ link.label }}
        </span>
      </div>

      <!-- CONTENT -->
      <div class="card-content">
        <div class="card-tags">
          <span
            v-for="tag in workshop.tags"
            :key="tag.id"
            class="tag"
            :style="{ background: tag.color, color: getContrastColor(tag.color) }"
          >
            {{ tag.name }}
          </span>

          <span
            class="tag tag-empty"
            title="Ajouter des tags"
            @click.stop="openTagSelector(workshop)"
          >
            <span class="mdi mdi-plus"></span>
          </span>
        </div>
      </div>

      <!-- FOOTER -->
      <div class="card-footer">
        <span
          class="mdi mdi-pencil-outline action"
          @click.stop="openEditor(workshop)"
        ></span>

        <span
          class="mdi action"
          :class="workshop.active
            ? 'mdi-archive-outline'
            : 'mdi-archive-arrow-up-outline'"
          @click.stop="archive(workshop.id, workshop)"
        ></span>

        <span
          class="mdi mdi-delete-outline action"
          @click.stop="remove(workshop.id)"
        ></span>
      </div>
    </article>
  </div>

  <div
    v-if="workshops.length === 0"
    class="empty-state"
  >
    <span class="mdi mdi-archive empty-icon"></span>
    <p class="empty-text">Aucun atelier à afficher</p>
  </div>

  <WorkshopTagSelector
    v-if="selectedWorkshop"
    :workshop="selectedWorkshop"
    @close="selectedWorkshop = null"
  />

  <WorkshopEditor
    v-if="editedWorkshop"
    :workshop="editedWorkshop"
    @close="editedWorkshop = null"
  />

  <WorkshopLinkViewer
    v-if="activeLink"
    :url="activeLink.url"
    :label="activeLink.label"
    @close="activeLink = null"
  />
</template>

<style scoped lang="scss">
.workshops-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 1rem;
}

.workshop-card {
  position: relative;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: var(--pico-border-radius);
  padding: 1rem;
  cursor: pointer;
  transition: box-shadow 0.2s, border-color 0.2s;
  display: flex;
  flex-direction: column;
  justify-content: space-between;

  &:hover {
    border-color: var(--pico-primary-border);
    box-shadow: var(--pico-card-box-shadow);

    .pin,
    .card-footer {
      opacity: 1;
    }
  }
}

/* HEADER */
.card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  margin-bottom: 1rem;

  h3 {
    margin: 0;
    font-size: 1rem;
    color: var(--pico-contrast);
    flex: 1;
  }
}

.pin {
  opacity: 0.7 !important;
  transition: opacity 0.15s;

  &.pinned {
    color: var(--pico-primary);
    opacity: 1 !important;
  }
}

/* LINKS */
.card-links {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  margin-bottom: 0.75rem;
}

.card-link {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.8rem;
  color: var(--pico-primary);
  text-decoration: none;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;

  .mdi {
    flex-shrink: 0;
    font-size: 0.9rem;
  }

  &:hover {
    text-decoration: underline;
  }
}

/* CONTENT */

/* TAGS */
.card-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.tag {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  height: 1.4rem;
  padding: 0 0.75rem;
  font-size: 0.75rem;
  border-radius: 1rem;
  font-weight: 500;
  line-height: 1;
  white-space: nowrap;
}

.tag:not(.tag-empty) {
  color: rgba(0, 0, 0, 0.87);
}

.tag-empty {
  background: transparent;
  border: 2px solid var(--pico-muted-border-color);
  color: var(--pico-muted-color);
  opacity: 0.7;

  &:hover {
    border-color: var(--pico-primary-border);
    color: var(--pico-primary);
    opacity: 1;
  }

  .mdi {
    font-size: 0.9rem;
  }
}

/* FOOTER */
.card-footer {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
  margin-top: 0.5rem;
  opacity: 0;
  transition: opacity 0.15s;
}

.action {
  font-size: 1.1rem;
  cursor: pointer;
  opacity: 0.7;

  &:hover {
    opacity: 1 !important;
  }
}

/* EMPTY */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  text-align: center;
}

.empty-icon {
  font-size: 4rem;
  margin-bottom: 1rem;
  opacity: 0.5;
  color: var(--pico-muted-color);
}

.empty-text {
  color: var(--pico-muted-color);
  font-size: 1.1rem;
}
</style>
