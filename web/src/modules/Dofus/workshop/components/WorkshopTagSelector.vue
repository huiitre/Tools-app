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
const { tags } = storeToRefs(store)

/* =========================
   STATE
========================= */
const selectedTagIds = ref<number[]>(
  props.workshop.tags.map(t => t.id)
)

/* =========================
   ACTIONS
========================= */
const toggle = (tagId: number) => {
  if (selectedTagIds.value.includes(tagId)) {
    selectedTagIds.value = selectedTagIds.value.filter(id => id !== tagId)
  } else {
    selectedTagIds.value.push(tagId)
  }
}

const save = async () => {
  const initialTagIds = props.workshop.tags.map(t => t.id)

  const toAdd = selectedTagIds.value.filter(
    id => !initialTagIds.includes(id)
  )

  const toRemove = initialTagIds.filter(
    id => !selectedTagIds.value.includes(id)
  )

  if (toAdd.length > 0) {
    await store.addTagsToWorkshop(props.workshop.id, toAdd)
  }

  for (const tagId of toRemove) {
    await store.removeTagFromWorkshop(props.workshop.id, tagId)
  }

  emit('close')
}
</script>

<template>
  <div class="overlay">
    <div class="popup">
      <div class="content">
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
        <button @click="save">Enregistrer</button>
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
  width: 320px;
  background: var(--pico-card-background-color);
  border-radius: var(--pico-border-radius);
  padding: 0.5rem;
}

.content {
  max-height: 300px;
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

.footer {
  display: flex;
  justify-content: flex-end;
  padding-top: 0.5rem;
}
</style>
