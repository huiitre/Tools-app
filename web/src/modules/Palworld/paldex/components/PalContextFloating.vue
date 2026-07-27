<script setup lang="ts">
import type { PalworldPalListItem } from '../types/paldex.types'
import { paldexLabel } from '../utils/paldexLabel'

const props = defineProps<{
  pal: PalworldPalListItem
  visible: boolean
  x: number
  y: number
}>()

const STATS: { key: keyof PalworldPalListItem; label: string }[] = [
  { key: 'baseHp', label: 'PV' },
  { key: 'baseAttack', label: 'Attaque' },
  { key: 'baseDefense', label: 'Défense' },
  { key: 'baseWorkSpeed', label: 'Vitesse de travail' },
  { key: 'baseSupport', label: 'Support' },
  { key: 'foodAmount', label: 'Capacité de faim' },
  { key: 'runSpeed', label: 'Vitesse de course' },
  { key: 'rideSprintSpeed', label: 'Vitesse en sprint (monture)' },
  { key: 'rarity', label: 'Rareté' },
]

function statValue(key: keyof PalworldPalListItem): string {
  const value = props.pal[key]
  return value === null || value === undefined ? '—' : String(value)
}
</script>

<template>
  <div
    v-if="visible"
    class="pal-context-floating"
    :style="{ left: x + 'px', top: y + 'px' }"
  >
    <!-- HEADER -->
    <div class="header">
      <img v-if="pal.imageUrl" :src="pal.imageUrl" :alt="pal.name" class="pal-icon">

      <div class="meta">
        <div class="name">{{ pal.name }}</div>
        <div class="sub">
          {{ paldexLabel(pal) }} · {{ pal.tribe }}<span v-if="pal.size"> · {{ pal.size }}</span>
        </div>
      </div>
    </div>

    <!-- DESCRIPTION -->
    <p v-if="pal.description" class="description">{{ pal.description }}</p>

    <!-- ELEMENTS -->
    <div v-if="pal.elements.length" class="elements-row">
      <span
        v-for="element in pal.elements"
        :key="element.id"
        class="element-icon-crop"
        :title="element.name"
        :style="element.iconUrl ? { backgroundImage: `url(${element.iconUrl})` } : {}"
      />
      <span v-for="element in pal.elements" :key="`name-${element.id}`" class="element-name">{{ element.name }}</span>
    </div>

    <!-- STATS -->
    <div class="section">
      <div class="section-title">Statistiques de base</div>
      <div class="stats-grid">
        <div v-for="stat in STATS" :key="stat.key" class="stat-line">
          <span>{{ stat.label }}</span>
          <strong>{{ statValue(stat.key) }}</strong>
        </div>
      </div>
    </div>

    <!-- WORK SUITABILITIES -->
    <div v-if="pal.workSuitabilities.length" class="section">
      <div class="section-title">Aptitudes de travail</div>
      <div class="ws-list">
        <div v-for="ws in pal.workSuitabilities" :key="ws.id" class="ws-line">
          <img v-if="ws.iconUrl" :src="ws.iconUrl" :alt="ws.name" class="ws-icon">
          <span class="ws-name">{{ ws.name }}</span>
          <strong>Nv. {{ ws.level }}</strong>
        </div>
      </div>
    </div>

    <!-- COMPÉTENCE DE PARTENAIRE -->
    <div v-if="pal.partnerSkill" class="section">
      <div class="section-title">Compétence de partenaire</div>
      <div class="partner-skill">
        <img v-if="pal.partnerSkill.iconUrl" :src="pal.partnerSkill.iconUrl" :alt="pal.partnerSkill.title" class="partner-icon">
        <div class="partner-info">
          <div class="partner-title">{{ pal.partnerSkill.title }}</div>
          <p v-if="pal.partnerSkill.description" class="partner-description">{{ pal.partnerSkill.description }}</p>
        </div>
      </div>
    </div>

    <!-- BEST WORK SUITABILITY (source brute du jeu) -->
    <div v-if="pal.bestWorkSuitabilityLabel" class="footer">
      Meilleure aptitude (jeu) : {{ pal.bestWorkSuitabilityLabel }}
    </div>
  </div>
</template>

<style scoped>
.pal-context-floating {
  position: fixed;
  z-index: 1000;
  pointer-events: none;

  width: 300px;

  background: var(--pico-card-background-color);
  border: 1px solid color-mix(in srgb, var(--pico-primary) 30%, transparent);
  box-shadow: var(--pico-card-box-shadow);
  border-radius: 10px;

  padding: 0.75rem;
  font-size: 0.8rem;
  color: var(--pico-color);
}

/* HEADER */
.header {
  display: flex;
  gap: 0.6rem;
  align-items: center;
}

.pal-icon {
  width: 48px;
  height: 48px;
  border-radius: 6px;
}

.meta .name {
  font-weight: 600;
}

.meta .sub {
  font-size: 0.72rem;
  color: var(--pico-muted-color);
}

/* ELEMENTS */
.elements-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.3rem;
  margin-top: 0.5rem;
}

.element-icon-crop {
  display: inline-block;
  flex-shrink: 0;
  width: 20px;
  height: 20px;
  border-radius: 4px;
  background-repeat: no-repeat;
  background-position: left center;
  background-size: auto 100%;
}

.element-name {
  font-size: 0.72rem;
  color: var(--pico-muted-color);
}

/* SECTIONS */
.section {
  margin-top: 0.7rem;
}

.section-title {
  font-weight: 600;
  font-size: 0.75rem;
  margin-bottom: 0.4rem;
  color: var(--pico-primary);
}

/* STATS */
.stats-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.2rem 0.75rem;
}

.stat-line {
  display: flex;
  justify-content: space-between;
  gap: 0.5rem;
  font-size: 0.75rem;

  span { color: var(--pico-muted-color); }
}

/* WORK SUITABILITIES */
.ws-list {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.ws-line {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  font-size: 0.75rem;

  strong { margin-left: auto; }
}

.ws-icon {
  width: 16px;
  height: 16px;
}

.ws-name {
  color: var(--pico-muted-color);
}

/* DESCRIPTION */
.description {
  margin: 0.5rem 0 0;
  font-size: 0.75rem;
  color: var(--pico-muted-color);
}

/* PARTNER SKILL */
.partner-skill {
  display: flex;
  gap: 0.5rem;
  align-items: flex-start;
}

.partner-icon {
  width: 28px;
  height: 28px;
  flex-shrink: 0;
}

.partner-info {
  min-width: 0;
  flex: 1;
}

.partner-title {
  font-weight: 600;
  font-size: 0.78rem;
  overflow-wrap: break-word;
}

.partner-description {
  margin: 0.2rem 0 0;
  font-size: 0.72rem;
  color: var(--pico-muted-color);
  overflow-wrap: break-word;
}

/* FOOTER */
.footer {
  margin-top: 0.6rem;
  padding-top: 0.45rem;
  border-top: 1px dashed var(--pico-muted-border-color);
  font-size: 0.7rem;
  color: var(--pico-muted-color);
}
</style>
