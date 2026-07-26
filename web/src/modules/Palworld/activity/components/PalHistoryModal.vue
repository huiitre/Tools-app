<script setup lang="ts">
import type { PalworldPalInstanceSnapshot, PalworldPalInstanceSummary } from '../../server/types/palworldServerData.types'

const props = defineProps<{
  loading: boolean
  error: string | null
  pal: PalworldPalInstanceSummary | null
  snapshots: PalworldPalInstanceSnapshot[]
}>()

const emit = defineEmits<{ close: [] }>()

function formatWorkableType(raw: string | null): string {
  if (!raw) return '—'
  return raw.replace('EPalWorkableType::', '')
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString('fr-FR', { dateStyle: 'short', timeStyle: 'short' })
}

// full_stomach est une valeur brute (pas déjà un %) : la capacité max (foodAmount) est
// une stat du Pal, pas du snapshot — on utilise celle du pal sélectionné pour tous les snapshots.
function hungerPercent(fullStomach: number | null): number | null {
  const foodAmount = props.pal?.palFoodAmount
  if (fullStomach === null || !foodAmount) return null
  return Math.min(100, Math.max(0, Math.round((fullStomach / foodAmount) * 100)))
}
</script>

<template>
  <div class="details-overlay" @click.self="emit('close')">
    <div class="details-popup">
      <div class="details-header">
        <span class="details-title">{{ pal?.palName ?? pal?.characterId ?? 'Historique du Pal' }}</span>
        <i class="mdi mdi-close details-close" @click="emit('close')" />
      </div>

      <div class="details-body">
        <p v-if="loading" class="details-status">Chargement…</p>
        <p v-else-if="error" class="details-status details-status--error">{{ error }}</p>
        <p v-else-if="!snapshots.length" class="details-status">Aucun historique disponible pour ce Pal.</p>

        <div v-else class="history-table-wrap">
          <table class="history-table">
            <thead>
              <tr>
                <th>Date</th>
                <th>Niveau</th>
                <th>Faim</th>
                <th>Malade</th>
                <th>Travail</th>
                <th>Avancement</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(snap, i) in snapshots" :key="i">
                <td>{{ formatDate(snap.capturedAt) }}</td>
                <td>{{ snap.level ?? '—' }}</td>
                <td>
                  <div v-if="hungerPercent(snap.fullStomach) !== null" class="hunger-cell">
                    <div class="hunger-cell-fill" :style="{ width: hungerPercent(snap.fullStomach) + '%' }" />
                    <span class="hunger-cell-text">{{ hungerPercent(snap.fullStomach) }}%</span>
                  </div>
                  <span v-else class="muted">—</span>
                </td>
                <td>
                  <span v-if="snap.isSick" class="sick-badge">Oui</span>
                  <span v-else class="muted">Non</span>
                </td>
                <td>{{ formatWorkableType(snap.workableType) }}</td>
                <td>
                  <span v-if="snap.currentWorkAmount !== null && snap.requiredWorkAmount !== null">
                    {{ snap.currentWorkAmount }} / {{ snap.requiredWorkAmount }}
                  </span>
                  <span v-else class="muted">—</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.details-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.6);
  z-index: 1000;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
}

.details-popup {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: var(--pico-border-radius);
  box-shadow: var(--pico-card-box-shadow);
  width: 100%;
  max-width: 820px;
  max-height: 85vh;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.details-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem 1.25rem;
  border-bottom: 1px solid var(--pico-muted-border-color);
}

.details-title {
  font-weight: 700;
  font-size: 1.05rem;
}

.details-close {
  cursor: pointer;
  color: var(--pico-muted-color);
  font-size: 1.2rem;

  &:hover { color: var(--pico-color); }
}

.details-body {
  padding: 1.25rem;
  overflow-y: auto;
}

.details-status {
  margin: 0;
  color: var(--pico-muted-color);
  font-size: 0.9rem;
}

.details-status--error {
  color: #e53e3e;
}

.history-table-wrap {
  overflow-x: auto;
}

.history-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.85rem;

  th, td {
    text-align: left;
    padding: 0.6rem 1rem;
    white-space: nowrap;
  }

  th {
    color: var(--pico-muted-color);
    font-weight: 500;
    font-size: 0.75rem;
    border-bottom: 1px solid var(--pico-card-border-color);
  }

  tbody tr:not(:last-child) td {
    border-bottom: 1px solid var(--pico-card-border-color);
  }
}

.hunger-cell {
  position: relative;
  width: 90px;
  height: 16px;
  border-radius: 4px;
  background: var(--pico-muted-border-color);
  overflow: hidden;
}

.hunger-cell-fill {
  position: absolute;
  inset: 0;
  height: 100%;
  background: #f59e0b;
}

.hunger-cell-text {
  position: relative;
  z-index: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  font-size: 0.65rem;
  font-weight: 700;
  color: white;
  text-shadow: 0 0 2px rgba(0, 0, 0, 0.8);
}

.sick-badge {
  color: #e53e3e;
  font-weight: 600;
}

.muted {
  color: var(--pico-muted-color);
}
</style>
