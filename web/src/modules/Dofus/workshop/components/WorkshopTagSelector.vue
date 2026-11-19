<script setup lang="ts">
import { ref } from 'vue'
import { storeToRefs } from 'pinia'
import { useWorkshopStore } from '@/modules/Dofus/workshop/store/workshop.store'
import { Workshop } from '@/modules/Dofus/workshop/types/workshop.types'

const props = defineProps<{
  workshop: Workshop
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const store = useWorkshopStore()
const { tags, breeds, jobs } = storeToRefs(store)

/* =========================
   STATE
========================= */
const selectedTagIds = ref<number[]>(
  props.workshop.tags.map(t => t.id)
)
const selectedBreedIds = ref<number[]>(props.workshop.breeds.map(b => b.id))
const selectedJobIds = ref<number[]>(props.workshop.jobs.map(j => j.id))

/* =========================
   ACTIONS
========================= */
const toggleIn = (ids: number[], id: number) =>
  ids.includes(id) ? ids.filter(x => x !== id) : [...ids, id]

const toggle = (tagId: number) => {
  selectedTagIds.value = toggleIn(selectedTagIds.value, tagId)
}

//* Ce qui a été coché et décoché par rapport à l'état d'origine de l'atelier.
const diff = (initial: number[], selected: number[]) => ({
  toAdd: selected.filter(id => !initial.includes(id)),
  toRemove: initial.filter(id => !selected.includes(id))
})

const save = async () => {
  const tagsDiff = diff(props.workshop.tags.map(t => t.id), selectedTagIds.value)
  const breedsDiff = diff(props.workshop.breeds.map(b => b.id), selectedBreedIds.value)
  const jobsDiff = diff(props.workshop.jobs.map(j => j.id), selectedJobIds.value)

  if (tagsDiff.toAdd.length) await store.addTagsToWorkshop(props.workshop.id, tagsDiff.toAdd)
  for (const tagId of tagsDiff.toRemove) await store.removeTagFromWorkshop(props.workshop.id, tagId)

  if (breedsDiff.toAdd.length) await store.addBreedsToWorkshop(props.workshop.id, breedsDiff.toAdd)
  for (const breedId of breedsDiff.toRemove) await store.removeBreedFromWorkshop(props.workshop.id, breedId)

  if (jobsDiff.toAdd.length) await store.addJobsToWorkshop(props.workshop.id, jobsDiff.toAdd)
  for (const jobId of jobsDiff.toRemove) await store.removeJobFromWorkshop(props.workshop.id, jobId)

  emit('close')
}
</script>

<template>
  <div class="overlay" @click.self="emit('close')">
    <div class="popup">
      <div class="content">
        <template v-if="breeds.length">
          <span class="section-label">Classes</span>
          <div class="icon-grid">
            <button
              v-for="breed in breeds"
              :key="breed.id"
              type="button"
              class="icon-option"
              :class="{ selected: selectedBreedIds.includes(breed.id) }"
              :title="breed.name"
              @click="selectedBreedIds = toggleIn(selectedBreedIds, breed.id)"
            >
              <img :src="breed.iconUrl ?? undefined" :alt="breed.name">
            </button>
          </div>
        </template>

        <template v-if="jobs.length">
          <span class="section-label">Métiers</span>
          <div class="icon-grid">
            <button
              v-for="job in jobs"
              :key="job.id"
              type="button"
              class="icon-option"
              :class="{ selected: selectedJobIds.includes(job.id) }"
              :title="job.name"
              @click="selectedJobIds = toggleIn(selectedJobIds, job.id)"
            >
              <img :src="job.iconUrl ?? undefined" :alt="job.name">
            </button>
          </div>
        </template>

        <span v-if="tags.length" class="section-label">Tags</span>
        <div
          v-for="tag in tags"
          :key="tag.id"
          class="tag-row"
          :class="{ selected: selectedTagIds.includes(tag.id) }"
          @click="toggle(tag.id)"
        >
          <span
            class="dot"
            :style="{ background: tag.color }"
          ></span>
          <span class="label">{{ tag.name }}</span>
          <span
            v-if="selectedTagIds.includes(tag.id)"
            class="mdi mdi-check"
          ></span>
        </div>
      </div>

      <div class="footer">
        <button type="button" @click="save">Enregistrer</button>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.45);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.popup {
  width: 360px;
  background: var(--pico-card-background-color);
  border-radius: var(--pico-border-radius);
  padding: 0.5rem;
}

.content {
  max-height: 70vh;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
}

.tag-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem;
  border-radius: var(--pico-border-radius);
  cursor: pointer;

  &:hover {
    background: var(--pico-muted-border-color);
  }

  &.selected {
    background: var(--pico-primary-background);
    color: var(--pico-primary-inverse);
  }
}

.dot {
  width: 12px;
  height: 12px;
  border-radius: 50%;
}

.label {
  flex: 1;
  font-size: 0.9rem;
}

.section-label {
  font-size: 0.75rem;
  font-weight: 500;
  color: var(--pico-muted-color);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  padding: 0.5rem 0.5rem 0.1rem;
}

.icon-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem;
  padding: 0 0.5rem;
}

.icon-option {
  margin: 0;
  padding: 2px;
  width: auto;
  background: transparent;
  border: 2px solid transparent;
  border-radius: var(--pico-border-radius);
  opacity: 0.55;
  cursor: pointer;
  transition: opacity 0.15s, border-color 0.15s;

  img {
    display: block;
    width: 32px;
    height: 32px;
  }

  &:hover {
    opacity: 1;
  }

  &.selected {
    opacity: 1;
    border-color: var(--pico-primary);
  }
}

.footer {
  display: flex;
  justify-content: flex-end;
  padding-top: 0.5rem;

  button {
    width: auto;
    margin: 0;
  }
}
</style>
