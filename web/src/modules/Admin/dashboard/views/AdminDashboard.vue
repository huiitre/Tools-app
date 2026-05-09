<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { fetchAdminStats, type AdminStats } from '../fetch/adminStats.fetch'

const stats = ref<AdminStats | null>(null)
const error = ref<string | null>(null)
const loading = ref(true)

onMounted(async () => {
  try {
    stats.value = await fetchAdminStats()
  } catch {
    error.value = 'Impossible de charger les statistiques.'
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div class="dashboard">
    <div class="dashboard-header">
      <h2 class="dashboard-title">Dashboard</h2>
      <p class="dashboard-subtitle">Vue d'ensemble de la plateforme</p>
    </div>

    <!-- Erreur -->
    <div v-if="error" class="error-banner">
      <i class="mdi mdi-alert-circle-outline" />
      {{ error }}
    </div>

    <!-- KPI cards -->
    <div class="kpi-grid">
      <div class="kpi-card">
        <div class="kpi-icon kpi-icon--blue">
          <i class="mdi mdi-account-group-outline" />
        </div>
        <div class="kpi-body">
          <div class="kpi-label">Utilisateurs</div>
          <div class="kpi-value">
            <template v-if="loading"><span class="skeleton-value" /></template>
            <template v-else>{{ stats?.totalUsers ?? '—' }}</template>
          </div>
        </div>
      </div>

      <div class="kpi-card">
        <div class="kpi-icon kpi-icon--green">
          <i class="mdi mdi-account-check-outline" />
        </div>
        <div class="kpi-body">
          <div class="kpi-label">Comptes actifs</div>
          <div class="kpi-value">
            <template v-if="loading"><span class="skeleton-value" /></template>
            <template v-else>{{ stats?.activeUsers ?? '—' }}</template>
          </div>
          <div v-if="!loading && stats" class="kpi-sub">
            {{ Math.round(stats.activeUsers / stats.totalUsers * 100) }}% du total
          </div>
        </div>
      </div>

      <div class="kpi-card">
        <div class="kpi-icon kpi-icon--purple">
          <i class="mdi mdi-account-plus-outline" />
        </div>
        <div class="kpi-body">
          <div class="kpi-label">Nouveaux cette semaine</div>
          <div class="kpi-value">
            <template v-if="loading"><span class="skeleton-value" /></template>
            <template v-else>{{ stats?.newUsersThisWeek ?? '—' }}</template>
          </div>
        </div>
      </div>
    </div>

    <!-- Modules -->
    <div class="section">
      <div class="section-header">
        <h3 class="section-title">Utilisateurs par module</h3>
      </div>

      <div v-if="loading" class="modules-list">
        <div v-for="i in 3" :key="i" class="module-row skeleton-row">
          <span class="skeleton-line" style="width: 120px" />
          <span class="skeleton-line" style="width: 40px" />
        </div>
      </div>

      <div v-else-if="stats?.usersPerModule.length" class="modules-list">
        <div
          v-for="mod in stats.usersPerModule"
          :key="mod.moduleId"
          class="module-row"
        >
          <div class="module-info">
            <i class="mdi mdi-puzzle-outline module-icon" />
            <span class="module-name">{{ mod.moduleName }}</span>
          </div>
          <div class="module-bar-wrap">
            <div
              class="module-bar"
              :style="{ width: `${Math.round(mod.userCount / stats!.totalUsers * 100)}%` }"
            />
          </div>
          <span class="module-count">{{ mod.userCount }}</span>
        </div>
      </div>

      <p v-else-if="!loading" class="empty">Aucun module disponible.</p>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.dashboard {
  padding: 2rem;
  max-width: 900px;
}

/* ── Header ──────────────────────────────────────────────────────── */
.dashboard-header {
  margin-bottom: 2rem;
}

.dashboard-title {
  font-size: 1.3rem;
  font-weight: 700;
  margin: 0 0 0.25rem;
}

.dashboard-subtitle {
  font-size: 0.875rem;
  color: var(--pico-muted-color);
  margin: 0;
}

/* ── Error ───────────────────────────────────────────────────────── */
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
  margin-bottom: 1.5rem;
}

/* ── KPI grid ────────────────────────────────────────────────────── */
.kpi-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 1rem;
  margin-bottom: 2rem;
}

.kpi-card {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1.25rem;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 12px;
}

.kpi-icon {
  width: 44px;
  height: 44px;
  border-radius: 10px;
  display: grid;
  place-items: center;
  flex-shrink: 0;

  i { font-size: 1.4rem; }

  &--blue   { background: color-mix(in srgb, #3b82f6 12%, transparent); color: #3b82f6; }
  &--green  { background: color-mix(in srgb, #22c55e 12%, transparent); color: #22c55e; }
  &--purple { background: color-mix(in srgb, #a855f7 12%, transparent); color: #a855f7; }
}

.kpi-body {
  min-width: 0;
}

.kpi-label {
  font-size: 0.78rem;
  color: var(--pico-muted-color);
  font-weight: 500;
  margin-bottom: 0.2rem;
}

.kpi-value {
  font-size: 1.75rem;
  font-weight: 700;
  line-height: 1;
}

.kpi-sub {
  font-size: 0.75rem;
  color: var(--pico-muted-color);
  margin-top: 0.25rem;
}

/* ── Section ─────────────────────────────────────────────────────── */
.section {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 12px;
  overflow: hidden;
}

.section-header {
  padding: 1rem 1.25rem;
  border-bottom: 1px solid var(--pico-card-border-color);
}

.section-title {
  font-size: 0.875rem;
  font-weight: 600;
  margin: 0;
}

/* ── Module rows ─────────────────────────────────────────────────── */
.modules-list {
  display: flex;
  flex-direction: column;
}

.module-row {
  display: grid;
  grid-template-columns: 180px 1fr 48px;
  align-items: center;
  gap: 1rem;
  padding: 0.9rem 1.25rem;
  border-bottom: 1px solid var(--pico-card-border-color);

  &:last-child { border-bottom: none; }
}

.module-info {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  min-width: 0;
}

.module-icon {
  font-size: 1rem;
  color: var(--pico-muted-color);
  flex-shrink: 0;
}

.module-name {
  font-size: 0.875rem;
  font-weight: 500;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.module-bar-wrap {
  height: 6px;
  background: var(--pico-card-border-color);
  border-radius: 999px;
  overflow: hidden;
}

.module-bar {
  height: 100%;
  background: var(--pico-primary);
  border-radius: 999px;
  transition: width 0.6s ease;
  min-width: 2px;
}

.module-count {
  font-size: 0.875rem;
  font-weight: 600;
  text-align: right;
  color: var(--pico-muted-color);
}

/* ── Skeleton ────────────────────────────────────────────────────── */
@keyframes shimmer {
  0%   { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

%shimmer {
  background: linear-gradient(
    90deg,
    var(--pico-card-background-color) 0%,
    var(--pico-muted-border-color) 50%,
    var(--pico-card-background-color) 100%
  );
  background-size: 200% 100%;
  animation: shimmer 1.6s ease-in-out infinite;
  border-radius: 4px;
}

.skeleton-value {
  @extend %shimmer;
  display: block;
  width: 60px;
  height: 1.75rem;
}

.skeleton-row {
  pointer-events: none;
}

.skeleton-line {
  @extend %shimmer;
  display: block;
  height: 0.875rem;
}

/* ── Empty ───────────────────────────────────────────────────────── */
.empty {
  padding: 1.5rem 1.25rem;
  color: var(--pico-muted-color);
  font-size: 0.875rem;
  margin: 0;
}
</style>
