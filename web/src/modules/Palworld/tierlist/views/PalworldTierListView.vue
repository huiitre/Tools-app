<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { usePalworldTierListStore } from '../palworldTierList.store'
import type { PalworldPal } from '../types/palworldTierList.types'

const store = usePalworldTierListStore()

const CATEGORIES: { id: string; label: string }[] = [
  { id: 'best', label: 'Meilleurs' },
  { id: 'base-work', label: 'Meilleur Travailleur' },
  { id: 'flying-mounts', label: 'Montures Volantes' },
  { id: 'ground-mounts', label: 'Montures Terrestres' },
  { id: 'combat', label: 'Combat' },
]

const activeCategory = ref('best')
const searchQuery = ref('')

const currentTiers = computed(() => store.data?.[activeCategory.value] ?? [])

const normalizedQuery = computed(() => searchQuery.value.trim().toLowerCase())

const matches = (pal: PalworldPal) => !normalizedQuery.value || pal.name.toLowerCase().includes(normalizedQuery.value)

const totalCount = computed(() => currentTiers.value.reduce((sum, group) => sum + group.pals.length, 0))
const visibleCount = computed(() => currentTiers.value.reduce(
  (sum, group) => sum + group.pals.filter(matches).length, 0,
))

const searchResults = computed(() => {
  if (!normalizedQuery.value) return []
  return currentTiers.value.flatMap(group =>
    group.pals.filter(matches).map(pal => ({ ...pal, tier: group.tier })),
  )
})

const isRowAllDimmed = (pals: PalworldPal[]) =>
  !!normalizedQuery.value && pals.every(pal => !matches(pal))

onMounted(() => {
  store.ensureLoaded()
})
</script>

<template>
  <div class="tierlist">
    <div class="tierlist-header">
      <nav class="category-tabs">
        <button
          v-for="cat in CATEGORIES"
          :key="cat.id"
          type="button"
          class="category-tab"
          :class="{ active: activeCategory === cat.id }"
          @click="activeCategory = cat.id"
        >
          {{ cat.label }}
        </button>
      </nav>

      <div class="search-bar">
        <div class="search-wrapper">
          <i class="mdi mdi-magnify" />
          <input v-model="searchQuery" type="text" placeholder="Rechercher un pal…" autocomplete="off">
        </div>
        <span v-if="normalizedQuery" class="search-counter">
          <strong>{{ visibleCount }}</strong> / {{ totalCount }} pals trouvés
        </span>
        <button type="button" class="clear-btn" @click="searchQuery = ''">
          Effacer
        </button>
      </div>
    </div>

    <div v-if="store.error" class="error-banner">
      <i class="mdi mdi-alert-circle-outline" />
      {{ store.error }}
    </div>

    <div v-else-if="store.loading" class="status">
      <span class="spinner" />
      Chargement de la tier list…
    </div>

    <template v-else>
      <div v-if="normalizedQuery" class="search-results">
        <a
          v-for="pal in searchResults"
          :key="pal.name + pal.tier"
          :href="pal.href"
          target="_blank"
          rel="noopener noreferrer"
          class="search-result-row"
        >
          <img :src="pal.image" :alt="pal.name" loading="lazy">
          <span class="result-name">{{ pal.name }}</span>
          <span class="result-tier" :class="`tier-${pal.tier}`">{{ pal.tier }}</span>
        </a>

        <p v-if="searchResults.length === 0" class="empty">Aucun pal trouvé.</p>
      </div>

      <div class="tier-list">
        <div
          v-for="group in currentTiers"
          :key="group.tier"
          class="tier-row"
          :class="{ 'all-dimmed': isRowAllDimmed(group.pals) }"
        >
          <div class="tier-badge" :class="`tier-${group.tier}`">{{ group.tier }}</div>
          <div class="tier-pals">
            <a
              v-for="pal in group.pals"
              :key="pal.name"
              :href="pal.href"
              target="_blank"
              rel="noopener noreferrer"
              class="pal-card"
              :class="{ dimmed: !matches(pal), wide: !!pal.workSkills?.length }"
            >
              <img :src="pal.image" :alt="pal.name" width="72" height="72" loading="lazy">
              <span class="pal-name">{{ pal.name }}</span>
              <span v-if="pal.speed" class="pal-speed">{{ pal.speed.min }} - {{ pal.speed.max }}</span>
              <span v-if="pal.workSkills?.length" class="pal-workskills">
                <span v-for="skill in pal.workSkills" :key="skill.name" class="workskill" :title="skill.name">
                  <img :src="skill.icon" :alt="skill.name" width="16" height="16" loading="lazy">
                  <span class="workskill-level">{{ skill.level }}</span>
                </span>
              </span>
            </a>
          </div>
        </div>

        <p v-if="currentTiers.length === 0" class="empty">Aucune donnée pour cette catégorie.</p>
      </div>
    </template>
  </div>
</template>

<style lang="scss" scoped>
.tierlist {
  padding: 2rem;
  max-width: 1100px;
  margin: 0 auto;
  width: 100%;
  box-sizing: border-box;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

@media (max-width: 640px) {
  .tierlist {
    padding: 1rem;
  }
}

/* ── Header ──────────────────────────────────────────────────────── */
.tierlist-header {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.category-tabs {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.category-tab {
  padding: 0.4rem 0.9rem;
  border-radius: 999px;
  border: 1px solid var(--pico-card-border-color);
  background: transparent;
  color: var(--pico-muted-color);
  font-size: 0.8rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.15s ease;
  margin: 0;
  width: auto;

  &:hover {
    color: var(--pico-color);
    border-color: var(--pico-primary);
  }

  &.active {
    background: var(--pico-primary);
    border-color: var(--pico-primary);
    color: var(--pico-primary-inverse, #000);
    font-weight: 700;
  }
}

/* ── Search bar ──────────────────────────────────────────────────── */
.search-bar {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.search-wrapper {
  position: relative;
  flex: 0 0 280px;

  i {
    position: absolute;
    left: 0.6rem;
    top: 50%;
    transform: translateY(-50%);
    color: var(--pico-muted-color);
    font-size: 1rem;
    pointer-events: none;
  }

  input {
    width: 100%;
    margin: 0;
    padding: 0.45rem 0.7rem 0.45rem 2rem;
    font-size: 0.85rem;
  }
}

.search-counter {
  font-size: 0.78rem;
  color: var(--pico-muted-color);

  strong { color: var(--pico-primary); }
}

.clear-btn {
  padding: 0.4rem 0.8rem;
  font-size: 0.78rem;
  width: auto;
  margin: 0;
  border-radius: 6px;
  border: 1px solid var(--pico-card-border-color);
  background: transparent;
  color: var(--pico-muted-color);
  cursor: pointer;

  &:hover {
    color: var(--pico-color);
    border-color: var(--pico-color);
  }
}

/* ── Status / error ──────────────────────────────────────────────── */
.status {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  padding: 3rem 0;
  color: var(--pico-muted-color);
  font-size: 0.9rem;
}

.spinner {
  width: 20px;
  height: 20px;
  border: 2px solid var(--pico-card-border-color);
  border-top-color: var(--pico-primary);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.error-banner {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  border-radius: 8px;
  background: color-mix(in srgb, #e53e3e 10%, transparent);
  border: 1px solid color-mix(in srgb, #e53e3e 25%, transparent);
  color: #e53e3e;
  font-size: 0.875rem;
}

.empty {
  padding: 1.5rem;
  text-align: center;
  color: var(--pico-muted-color);
  font-size: 0.875rem;
  margin: 0;
}

/* ── Tier colors ─────────────────────────────────────────────────── */
.tier-S { background: #ff6b00; }
.tier-A { background: #ffb300; }
.tier-B { background: #3dba5e; }
.tier-C { background: #3a9fd6; }
.tier-D { background: #8878c0; }

/* ── Tier list ───────────────────────────────────────────────────── */
.tier-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.tier-row {
  display: flex;
  align-items: stretch;
  border-radius: 10px;
  overflow: hidden;
  border: 1px solid var(--pico-card-border-color);
  background: var(--pico-card-background-color);
}

.tier-badge {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 48px;
  min-height: 88px;
  font-size: 22px;
  font-weight: 900;
  color: #000;
  flex-shrink: 0;
  transition: opacity 0.2s ease;
}

.tier-row.all-dimmed .tier-badge {
  opacity: 0.35;
}

.tier-pals {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  padding: 8px 10px;
  flex: 1;
  align-content: flex-start;
}

/* ── Pal card ────────────────────────────────────────────────────── */
.pal-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  padding: 6px;
  border-radius: 8px;
  text-decoration: none;
  width: 84px;
  transition: background 0.15s ease, opacity 0.2s ease;
  border: 1px solid transparent;

  &:hover {
    background: color-mix(in srgb, var(--pico-color) 8%, transparent);
    border-color: var(--pico-card-border-color);
  }

  &.dimmed {
    opacity: 0.15;
    pointer-events: none;
  }

  img {
    width: 72px;
    height: 72px;
    border-radius: 6px;
    display: block;
  }

  &.wide {
    width: 96px;
  }
}

.pal-name {
  font-size: 10.5px;
  color: var(--pico-color);
  text-align: center;
  line-height: 1.3;
  word-break: break-word;
}

.pal-speed {
  font-size: 9.5px;
  font-weight: 700;
  color: var(--pico-primary);
}

.pal-workskills {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 3px;
}

.workskill {
  display: flex;
  align-items: center;
  gap: 2px;
  padding: 1px 3px;
  border-radius: 4px;
  background: color-mix(in srgb, var(--pico-color) 8%, transparent);

  img {
    width: 14px;
    height: 14px;
    border-radius: 0;
  }
}

.workskill-level {
  font-size: 9px;
  font-weight: 700;
  color: var(--pico-color);
}

/* ── Search results ──────────────────────────────────────────────── */
.search-results {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.search-result-row {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 12px;
  border-radius: 8px;
  border: 1px solid var(--pico-card-border-color);
  background: var(--pico-card-background-color);
  text-decoration: none;
  color: var(--pico-color);
  transition: background 0.15s ease, border-color 0.15s ease;

  &:hover {
    background: color-mix(in srgb, var(--pico-color) 8%, transparent);
    border-color: var(--pico-primary);
  }

  img {
    width: 36px;
    height: 36px;
    border-radius: 6px;
    flex-shrink: 0;
  }
}

.result-name {
  flex: 1;
  font-size: 0.85rem;
}

.result-tier {
  width: 26px;
  height: 26px;
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.75rem;
  font-weight: 900;
  color: #000;
  flex-shrink: 0;
}
</style>
