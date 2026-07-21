<script setup lang="ts">
import type { PalworldBase, PalworldBasePal, PalworldGamePlayer } from '../types/palworldServer.types'
import PalworldMiniMap from './PalworldMiniMap.vue'

defineProps<{
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
              <div class="hp-bar hp-bar--small">
                <div class="hp-bar-fill" :style="{ width: hpPercent(player.activePal.hp, player.activePal.maxHp) + '%' }" />
              </div>
            </div>
            <p v-else class="empty">Aucun pal actif.</p>
          </div>

          <div class="details-section">
            <h4 class="section-title">Bases ({{ bases.length }})</h4>
            <ul v-if="bases.length" class="base-list">
              <li v-for="(base, i) in bases" :key="i" class="base-item">
                <span class="base-name">{{ base.name }}</span>
                <span class="base-position">{{ base.mapX }}, {{ base.mapY }}</span>
              </li>
            </ul>
            <p v-else class="empty">Aucune base détectée.</p>
          </div>

          <div class="details-section">
            <h4 class="section-title">Pals de la guilde ({{ basePals.length }})</h4>
            <ul v-if="basePals.length" class="pal-list">
              <li v-for="(pal, i) in basePals" :key="i" class="pal-row">
                <span class="pal-name">{{ pal.name }}</span>
                <span class="pal-level">Nv. {{ pal.level }}</span>
                <div class="hp-bar hp-bar--small">
                  <div class="hp-bar-fill" :style="{ width: hpPercent(pal.hp, pal.maxHp) + '%' }" />
                </div>
              </li>
            </ul>
            <p v-else class="empty">Aucun pal détecté.</p>
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
  max-width: 480px;
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

.base-list, .pal-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.base-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 0.85rem;
}

.base-position {
  color: var(--pico-muted-color);
  font-size: 0.78rem;
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
