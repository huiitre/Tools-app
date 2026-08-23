<script setup lang="ts">
import { computed } from 'vue'
import type { R2rExpeditionSummary } from '../types/r2r.types'

const props = defineProps<{
  expeditions: R2rExpeditionSummary[]
  loading: boolean
}>()

const emit = defineEmits<{
  (e: 'select', id: string): void
  (e: 'delete', id: string): void
  (e: 'import'): void
}>()

function progress(exp: R2rExpeditionSummary) {
  if (!exp.totalSystems) return 0
  return Math.round((exp.currentSystemIndex / exp.totalSystems) * 100)
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('fr-FR', {
    day: '2-digit', month: 'short', year: 'numeric',
  })
}

const sorted = computed(() =>
  [...props.expeditions].sort(
    (a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()
  )
)
</script>

<template>
  <div class="expedition-page">
    <!-- TOOLBAR -->
    <div class="toolbar">
      <button class="btn-import" @click="emit('import')">
        <span class="mdi mdi-upload" />
        Importer une expédition
      </button>
    </div>

    <!-- LOADING -->
    <div v-if="loading" class="empty-state">
      <span aria-busy="true" />
    </div>

    <!-- EMPTY -->
    <div v-else-if="!sorted.length" class="empty-state">
      <span class="mdi mdi-rocket-launch empty-icon" />
      <p class="empty-text">Aucune expédition enregistrée</p>
      <p class="empty-hint">Importez un fichier JSON depuis Spansh pour commencer</p>
    </div>

    <!-- GRID -->
    <div v-else class="expedition-grid">
      <article
        v-for="exp in sorted"
        :key="exp.id"
        class="expedition-card"
        @click="emit('select', exp.id)"
      >
        <div class="card-header">
          <span class="card-name">{{ exp.name }}</span>
          <span class="card-source">{{ exp.source }}</span>
        </div>

        <div class="card-progress">
          <progress :value="progress(exp)" max="100" />
          <span class="card-progress-label">
            {{ exp.currentSystemIndex }} / {{ exp.totalSystems }} systèmes · {{ progress(exp) }}%
          </span>
        </div>

        <div class="card-footer">
          <span class="card-date">{{ formatDate(exp.updatedAt) }}</span>
          <div class="card-actions">
            <span
              class="mdi mdi-delete-outline action"
              title="Supprimer"
              @click.stop="emit('delete', exp.id)"
            />
          </div>
        </div>
      </article>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.expedition-page {
  padding: 1.5rem;
}

.toolbar {
  display: flex;
  justify-content: flex-end;
  margin-bottom: 1.5rem;
}

.btn-import {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.5rem 1rem;
  font-size: 0.875rem;
  border-radius: var(--pico-border-radius);
  background: var(--pico-form-element-background-color);
  color: var(--pico-form-element-color);
  border: 1px solid var(--pico-form-element-border-color);
  cursor: pointer;
  font-weight: 500;
  transition: border-color 0.2s, color 0.2s;

  &:hover {
    border-color: var(--pico-primary);
    color: var(--pico-primary);
  }
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 5rem 2rem;
  text-align: center;
}

.empty-icon {
  font-size: 3.5rem;
  margin-bottom: 1rem;
  opacity: 0.4;
  color: var(--pico-muted-color);
}

.empty-text {
  color: var(--pico-color);
  font-size: 1.1rem;
  margin-bottom: 0.25rem;
}

.empty-hint {
  color: var(--pico-muted-color);
  font-size: 0.875rem;
}

.expedition-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 1rem;
}

.expedition-card {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: var(--pico-border-radius);
  padding: 1rem;
  cursor: pointer;
  transition: border-color 0.2s, box-shadow 0.2s;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;

  &:hover {
    border-color: var(--pico-primary-border);
    box-shadow: var(--pico-card-box-shadow);

    .card-actions { opacity: 1; }
  }
}

.card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
}

.card-name {
  font-weight: 600;
  font-size: 0.95rem;
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.card-source {
  font-size: 0.7rem;
  color: var(--pico-muted-color);
  background: var(--pico-card-sectioning-background-color);
  padding: 0.1rem 0.45rem;
  border-radius: 0.75rem;
  text-transform: capitalize;
  flex-shrink: 0;
}

.card-progress {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;

  progress { width: 100%; margin: 0; }
}

.card-progress-label {
  font-size: 0.75rem;
  color: var(--pico-muted-color);
}

.card-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.card-date {
  font-size: 0.75rem;
  color: var(--pico-muted-color);
}

.card-actions {
  display: flex;
  gap: 0.5rem;
  opacity: 0;
  transition: opacity 0.15s;
}

.action {
  font-size: 1.1rem;
  cursor: pointer;
  color: var(--pico-muted-color);
  opacity: 0.7;
  transition: color 0.15s, opacity 0.15s;

  &:hover {
    opacity: 1;
    color: var(--pico-del-color);
  }
}
</style>
