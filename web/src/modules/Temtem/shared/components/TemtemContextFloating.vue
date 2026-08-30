<script setup lang="ts">
import { computed } from 'vue'
import type { TemtemSummary, TemtemStats } from '@/modules/Temtem/shared/types/temtem.types'
import { dexNumber, typesOf } from '../temtem.helpers'
import { useTemtemdexStore } from '@/modules/Temtem/temtemdex/temtemdex.store'

const props = defineProps<{
  temtem: TemtemSummary
  visible: boolean
  x: number
  y: number
}>()

const STATS: { key: keyof TemtemStats; label: string }[] = [
  { key: 'hp', label: 'PV' },
  { key: 'stamina', label: 'Endurance' },
  { key: 'speed', label: 'Vitesse' },
  { key: 'attack', label: 'Attaque' },
  { key: 'defense', label: 'Défense' },
  { key: 'specialAttack', label: 'Att. spéciale' },
  { key: 'specialDefense', label: 'Déf. spéciale' },
]

// L'échelle vient du catalogue entier, pas de ce Temtem : sinon sa meilleure statistique
// remplirait toujours la barre, et une attaque à 55 s'afficherait au maximum.
const store = useTemtemdexStore()

const total = computed(() =>
  STATS.reduce((sum, stat) => sum + props.temtem.stats[stat.key], 0))

const types = computed(() => typesOf(props.temtem))
</script>

<template>
  <div
    v-if="visible"
    class="temtem-context-floating"
    :style="{ left: x + 'px', top: y + 'px' }"
  >
    <!-- HEADER -->
    <div class="header">
      <img v-if="temtem.imageUrl" :src="temtem.imageUrl" :alt="temtem.name" class="temtem-icon">

      <div class="meta">
        <div class="name">{{ temtem.name }}</div>
        <div class="sub">{{ dexNumber(temtem) }}</div>
      </div>
    </div>

    <!-- TYPES -->
    <div class="types-row">
      <span v-for="type in types" :key="type.id" class="type-chip">
        <img v-if="type.imageUrl" :src="type.imageUrl" :alt="type.name" class="type-icon">
        {{ type.name }}
      </span>
    </div>

    <!-- STATS -->
    <div class="section">
      <div class="section-title">Statistiques de base</div>
      <div class="stats-list">
        <div v-for="stat in STATS" :key="stat.key" class="stat-line">
          <span class="stat-label">{{ stat.label }}</span>
          <span class="stat-bar">
            <span
              class="stat-fill"
              :style="{ width: (temtem.stats[stat.key] / store.maxStatValue * 100) + '%' }"
            />
          </span>
          <strong>{{ temtem.stats[stat.key] }}</strong>
        </div>
      </div>
    </div>

    <div class="footer">Total : {{ total }}</div>
  </div>
</template>

<style scoped>
.temtem-context-floating {
  position: fixed;
  z-index: 1000;
  pointer-events: none;

  width: 300px;

  background: var(--pico-card-background-color);
  border: 1px solid color-mix(in srgb, var(--pico-primary) 30%, transparent);
  box-shadow: var(--pico-card-box-shadow);
  border-radius: 10px;

  padding: 0.85rem;
  font-size: 0.88rem;
  color: var(--pico-color);
}

/* HEADER */
.header {
  display: flex;
  gap: 0.7rem;
  align-items: center;
}

.temtem-icon {
  width: 56px;
  height: 56px;
  border-radius: 6px;
}

.meta .name {
  font-weight: 600;
}

.meta .sub {
  font-size: 0.78rem;
  color: var(--pico-muted-color);
}

/* TYPES */
.types-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.35rem;
  margin-top: 0.5rem;
}

.type-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  padding: 0.15rem 0.45rem;
  border-radius: 999px;
  border: 1px solid var(--pico-card-border-color);
  font-size: 0.76rem;
  color: var(--pico-muted-color);
}

.type-icon {
  width: 16px;
  height: 16px;
}

/* SECTIONS */
.section {
  margin-top: 0.7rem;
}

.section-title {
  font-weight: 600;
  font-size: 0.82rem;
  margin-bottom: 0.4rem;
  color: var(--pico-primary);
}

/* STATS */
.stats-list {
  display: flex;
  flex-direction: column;
  gap: 0.22rem;
}

.stat-line {
  display: grid;
  grid-template-columns: 5.5rem 1fr 2rem;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.8rem;
}

.stat-label {
  color: var(--pico-muted-color);
}

.stat-bar {
  height: 5px;
  border-radius: 999px;
  background: color-mix(in srgb, var(--pico-color) 10%, transparent);
  overflow: hidden;
}

.stat-fill {
  display: block;
  height: 100%;
  border-radius: 999px;
  background: var(--pico-primary);
}

.stat-line strong {
  text-align: right;
  font-variant-numeric: tabular-nums;
}

/* FOOTER */
.footer {
  margin-top: 0.6rem;
  padding-top: 0.45rem;
  border-top: 1px dashed var(--pico-muted-border-color);
  font-size: 0.76rem;
  color: var(--pico-muted-color);
}
</style>
