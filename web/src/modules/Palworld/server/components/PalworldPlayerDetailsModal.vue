<script setup lang="ts">
import { computed } from 'vue'
import type { PalworldBase, PalworldBasePal, PalworldGamePlayer } from '../types/palworldServer.types'
import PalworldMiniMap from './PalworldMiniMap.vue'

const props = defineProps<{
  loading: boolean
  error: string | null
  player: PalworldGamePlayer | null
  bases: PalworldBase[]
  basePals: PalworldBasePal[]
}>()

const emit = defineEmits<{ close: [] }>()

function hpPercent(hp: number, maxHp: number): number {
  return maxHp > 0 ? Math.round((hp / maxHp) * 100) : 0
}

// L'API Palworld ne fournit pas d'identifiant reliant un pal à sa base précise
// (uniquement le GuildID, partagé par toutes les bases de la guilde) : on
// rapproche donc chaque pal de la base la plus proche géographiquement.
const basesWithPals = computed(() => {
  return props.bases.map(base => {
    const pals = props.basePals.filter(pal => {
      let closest = base
      let closestDist = Infinity
      for (const candidate of props.bases) {
        const dist = (pal.locationX - candidate.locationX) ** 2 + (pal.locationY - candidate.locationY) ** 2
        if (dist < closestDist) {
          closestDist = dist
          closest = candidate
        }
      }
      return closest === base
    })
    return { base, pals }
  })
})
</script>

<template>
  <div class="details-overlay" @click.self="emit('close')">
    <div class="details-popup">
      <div class="details-header">
        <span class="details-title">{{ player?.name ?? 'Détails du joueur' }}</span>
        <i class="mdi mdi-close details-close" @click="emit('close')" />
      </div>

      <div class="details-body">
        <p v-if="loading" class="details-status">Chargement…</p>
        <p v-else-if="error" class="details-status details-status--error">{{ error }}</p>

        <template v-else-if="player">
          <div class="details-section">
            <div class="hp-row">
              <span class="hp-label">Vie</span>
              <div class="hp-bar">
                <div class="hp-bar-fill" :style="{ width: hpPercent(player.hp, player.maxHp) + '%' }" />
              </div>
              <span class="hp-value">{{ player.hp }} / {{ player.maxHp }}</span>
            </div>

            <div class="info-grid">
              <div class="info-item">
                <div class="info-label">Niveau</div>
                <div class="info-value">{{ player.level }}</div>
              </div>
              <div class="info-item">
                <div class="info-label">Guilde</div>
                <div class="info-value">{{ player.guildName }}</div>
              </div>
              <div class="info-item">
                <div class="info-label">Position</div>
                <div class="info-value">{{ player.mapX }}, {{ player.mapY }}</div>
              </div>
            </div>
          </div>

          <div class="details-section">
            <h4 class="section-title">Carte</h4>
            <PalworldMiniMap :player="player" :bases="bases" />
          </div>

          <div class="details-section">
            <h4 class="section-title">Pal actif</h4>
            <div v-if="player.activePal" class="pal-row">
              <span class="pal-name">{{ player.activePal.name }}</span>
              <span class="pal-level">Nv. {{ player.activePal.level }}</span>
            </div>
            <p v-else class="empty">Aucun pal actif.</p>
          </div>

          <div class="details-section">
            <h4 class="section-title">Bases ({{ bases.length }})</h4>
            <div v-if="basesWithPals.length" class="base-group-list">
              <div v-for="({ base, pals }, i) in basesWithPals" :key="i" class="base-group">
                <div class="base-item">
                  <span class="base-name">{{ base.name }}</span>
                  <span class="base-item-right">
                    <span class="base-pal-count">{{ pals.length }} pal{{ pals.length > 1 ? 's' : '' }}</span>
                    <span class="base-position">{{ base.mapX }}, {{ base.mapY }}</span>
                  </span>
                </div>
                <ul v-if="pals.length" class="pal-list">
                  <li v-for="(pal, j) in pals" :key="j" class="pal-row">
                    <span class="pal-name">{{ pal.name }}</span>
                    <span class="pal-level">Nv. {{ pal.level }}</span>
                  </li>
                </ul>
                <p v-else class="empty">Aucun pal détecté.</p>
              </div>
            </div>
            <p v-else class="empty">Aucune base détectée.</p>
          </div>
        </template>
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
  max-width: 720px;
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

  &:hover {
    color: var(--pico-color);
  }
}

.details-body {
  padding: 1.25rem;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.details-status {
  margin: 0;
  color: var(--pico-muted-color);
  font-size: 0.9rem;
}

.details-status--error {
  color: #e53e3e;
}

.details-section {
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
}

.section-title {
  margin: 0;
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--pico-muted-color);
  text-transform: uppercase;
  letter-spacing: 0.03em;
}

.hp-row {
  display: flex;
  align-items: center;
  gap: 0.6rem;
}

.hp-label {
  font-size: 0.85rem;
  font-weight: 600;
  flex-shrink: 0;
}

.hp-value {
  font-size: 0.8rem;
  color: var(--pico-muted-color);
  flex-shrink: 0;
}

.hp-bar {
  flex: 1;
  height: 10px;
  border-radius: 5px;
  background: var(--pico-muted-border-color);
  overflow: hidden;
}

.hp-bar--small {
  height: 6px;
  width: 100px;
  flex: none;
}

.hp-bar-fill {
  height: 100%;
  background: #22c55e;
  border-radius: 5px;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0.75rem;
}

.info-item {
  min-width: 0;
}

.info-label {
  font-size: 0.72rem;
  color: var(--pico-muted-color);
  margin-bottom: 0.15rem;
}

.info-value {
  font-size: 0.9rem;
  font-weight: 600;
  overflow-wrap: break-word;
}

.pal-list {
  list-style: none;
  margin: 0;
  padding: 0.6rem 0 0 0.9rem;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  border-left: 2px solid var(--pico-muted-border-color);
}

.base-group-list {
  display: flex;
  flex-direction: column;
}

.base-group {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  padding: 0.75rem 0;

  &:not(:last-child) {
    border-bottom: 1px solid var(--pico-muted-border-color);
  }

  &:first-child {
    padding-top: 0;
  }

  &:last-child {
    padding-bottom: 0;
  }
}

.base-item {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  font-size: 0.9rem;
  font-weight: 600;
}

.base-item-right {
  display: flex;
  align-items: baseline;
  gap: 0.5rem;
  flex-shrink: 0;
}

.base-pal-count {
  font-weight: 500;
  font-size: 0.75rem;
  color: var(--pico-primary);
  background: color-mix(in srgb, var(--pico-primary) 12%, transparent);
  border-radius: 999px;
  padding: 0.1rem 0.55rem;
}

.base-position {
  color: var(--pico-muted-color);
  font-size: 0.78rem;
  font-weight: 400;
}

.pal-row {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  font-size: 0.85rem;
}

.pal-name {
  flex: 1;
  min-width: 0;
}

.pal-level {
  color: var(--pico-muted-color);
  font-size: 0.78rem;
  flex-shrink: 0;
}

.empty {
  margin: 0;
  color: var(--pico-muted-color);
  font-size: 0.85rem;
}
</style>
