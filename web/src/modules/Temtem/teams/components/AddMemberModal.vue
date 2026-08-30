<script setup lang="ts">
import { computed, ref } from 'vue'
import { useTemtemdexStore } from '@/modules/Temtem/temtemdex/temtemdex.store'
import { dexNumber, typesOf } from '@/modules/Temtem/shared/temtem.helpers'
import TemtemContextTrigger from '@/modules/Temtem/shared/components/TemtemContextTrigger.vue'
import type { TemtemSummary } from '@/modules/Temtem/shared/types/temtem.types'

const props = defineProps<{
  teamName: string
}>()

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'pick', temtem: TemtemSummary): void
}>()

const dexStore = useTemtemdexStore()

const query = ref('')
const busy = ref(false)

// Le catalogue est déjà en mémoire (chargé à l'entrée sur /temtem) : la recherche est locale,
// il n'y a pas d'appel réseau par frappe.
const results = computed(() => {
  const needle = query.value.trim().toLowerCase()
  const source = needle
    ? dexStore.temtem.filter(temtem => temtem.name.toLowerCase().includes(needle))
    : dexStore.temtem
  return source.slice(0, 60)
})

function pick(temtem: TemtemSummary) {
  busy.value = true
  emit('pick', temtem)
}
</script>

<template>
  <div class="overlay" @click.self="emit('close')">
    <div class="modal">
      <h3 class="modal-title">Ajouter un Temtem à « {{ teamName }} »</h3>

      <div class="modal-search">
        <input
          v-model="query"
          type="search"
          placeholder="Rechercher un Temtem..."
          autofocus
        >
      </div>

      <div class="modal-content">
        <TemtemContextTrigger
          v-for="temtem in results"
          :key="temtem.id"
          :temtem="temtem"
        >
          <button type="button" class="result" :disabled="busy" @click="pick(temtem)">
            <img v-if="temtem.imageUrl" :src="temtem.imageUrl" :alt="temtem.name" loading="lazy">
            <span class="result-name">{{ temtem.name }}</span>
            <span class="result-types">
              <img
                v-for="type in typesOf(temtem)"
                :key="type.id"
                :src="type.imageUrl ?? ''"
                :alt="type.name"
                :title="type.name"
                class="type-icon"
                loading="lazy"
              >
            </span>
            <span class="result-index">{{ dexNumber(temtem) }}</span>
          </button>
        </TemtemContextTrigger>

        <div v-if="!results.length" class="empty">Aucun Temtem ne correspond.</div>
      </div>

      <div class="modal-actions">
        <button type="button" class="secondary" @click="emit('close')">Fermer</button>
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

.modal {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  width: 440px;
  max-width: 92vw;
  max-height: 80vh;
  display: flex;
  flex-direction: column;
  font-size: 0.85rem;
}

.modal-title {
  font-size: 1rem;
  font-weight: 600;
  margin: 0;
  padding: 0.75rem 1rem;
  border-bottom: 1px solid var(--pico-muted-border-color);
}

.modal-search {
  padding: 0.6rem 1rem;
  border-bottom: 1px solid var(--pico-muted-border-color);

  input {
    margin: 0;
    height: 2.2rem;
    font-size: 0.85rem;
  }
}

.modal-content {
  padding: 0.4rem;
  overflow-y: auto;
  flex: 1;
}

.result {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  width: 100%;
  margin: 0;
  padding: 0.35rem 0.5rem;
  border: none;
  border-radius: var(--pico-border-radius);
  background: transparent;
  color: var(--pico-color);
  text-align: left;
  cursor: pointer;
  transition: background 0.15s;

  &:hover:not(:disabled) { background: var(--pico-card-sectioning-background-color); }
  &:disabled { opacity: 0.5; cursor: progress; }

  img {
    width: 34px;
    height: 34px;
    border-radius: 4px;
    flex-shrink: 0;
  }
}

.result-name {
  flex: 1;
  font-size: 0.85rem;
}

.result-types {
  display: flex;
  gap: 0.25rem;

  .type-icon {
    width: 18px;
    height: 18px;
    border-radius: 3px;
  }
}

.result-index {
  font-size: 0.74rem;
  color: var(--pico-muted-color);
  font-variant-numeric: tabular-nums;
}

.empty {
  padding: 1.5rem;
  text-align: center;
  color: var(--pico-muted-color);
}

.modal-actions {
  display: flex;
  padding: 0.75rem 1rem;
  border-top: 1px solid var(--pico-muted-border-color);

  button {
    flex: 1;
    margin: 0;
    font-size: 0.85rem;
  }
}
</style>
