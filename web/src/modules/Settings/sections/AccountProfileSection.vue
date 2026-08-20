<script setup lang="ts">
import type { ModuleType, Role } from '@/modules/Auth/auth.store'

type User = {
  id: string
  email: string
  name: string
  userType: string
  active: boolean
  avatarUrl: string | null
  role: Role | null
  modules: ModuleType[]
}

defineProps<{ user: User | null }>()

function initials(name: string) {
  return name
    .split(' ')
    .map(w => w[0])
    .slice(0, 2)
    .join('')
    .toUpperCase()
}
</script>

<template>
  <div class="profile-page">
    <h2 class="page-title">Profil</h2>

    <div v-if="user" class="profile-grid">

      <!-- ── Identité ──────────────────────────────────────────── -->
      <div class="card identity-card">
        <div class="avatar-wrap">
          <img v-if="user.avatarUrl" :src="user.avatarUrl" :alt="user.name" class="avatar-img" />
          <div v-else class="avatar-initials">{{ initials(user.name) }}</div>
          <span class="status-dot" :class="user.active ? 'active' : 'inactive'" :title="user.active ? 'Compte actif' : 'Compte inactif'" />
        </div>

        <div class="identity-info">
          <div class="user-name">{{ user.name }}</div>
          <div class="user-email">{{ user.email }}</div>
          <div class="user-type-badge">{{ user.userType }}</div>
        </div>
      </div>

      <!-- ── Détails ────────────────────────────────────────────── -->
      <div class="card details-card">
        <div class="card-title">Informations</div>
        <dl class="info-list">
          <div class="info-row">
            <dt>Identifiant</dt>
            <dd class="mono">{{ user.id }}</dd>
          </div>
          <div class="info-row">
            <dt>Nom</dt>
            <dd>{{ user.name }}</dd>
          </div>
          <div class="info-row">
            <dt>Email</dt>
            <dd>{{ user.email }}</dd>
          </div>
          <div class="info-row">
            <dt>Type</dt>
            <dd>{{ user.userType }}</dd>
          </div>
          <div class="info-row">
            <dt>Statut</dt>
            <dd>
              <span class="badge" :class="user.active ? 'badge--green' : 'badge--red'">
                {{ user.active ? 'Actif' : 'Inactif' }}
              </span>
            </dd>
          </div>
        </dl>
      </div>

      <!-- ── Rôle ───────────────────────────────────────────────── -->
      <div v-if="user.role?.active" class="card roles-card">
        <div class="card-title">Rôle</div>
        <div class="roles-list">
          <div class="role-item">
            <i class="mdi mdi-shield-check-outline role-icon" />
            <div>
              <div class="role-name">{{ user.role.name }}</div>
              <div class="role-desc">{{ user.role.description }}</div>
            </div>
          </div>
        </div>
      </div>

      <!-- ── Modules ────────────────────────────────────────────── -->
      <div v-if="user.modules.length" class="card modules-card">
        <div class="card-title">Modules accessibles</div>
        <div class="modules-list">
          <div
            v-for="mod in user.modules.filter(m => m.active)"
            :key="mod.id"
            class="module-item"
          >
            <i class="mdi mdi-puzzle-outline module-icon" />
            <div>
              <div class="module-name">{{ mod.name }}</div>
              <div class="module-desc">{{ mod.description }}</div>
            </div>
          </div>
        </div>
      </div>

    </div>

    <div v-else class="empty-state">Aucune information utilisateur disponible.</div>
  </div>
</template>

<style lang="scss" scoped>
.profile-page {
  max-width: 800px;
}

.page-title {
  font-size: 1.3rem;
  font-weight: 700;
  margin-bottom: 1.5rem;
}

/* ── Grid ───────────────────────────────────────────────────────── */
.profile-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1.25rem;
}

/* ── Card base ──────────────────────────────────────────────────── */
.card {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 12px;
  padding: 1.25rem;
}

.card-title {
  font-size: 0.75rem;
  font-weight: 600;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--pico-muted-color);
  margin-bottom: 1rem;
}

/* ── Identity card ──────────────────────────────────────────────── */
.identity-card {
  grid-column: 1 / -1;
  display: flex;
  align-items: center;
  gap: 1.25rem;
}

.avatar-wrap {
  position: relative;
  flex-shrink: 0;
}

.avatar-img,
.avatar-initials {
  width: 72px;
  height: 72px;
  border-radius: 50%;
  object-fit: cover;
}

.avatar-initials {
  background: color-mix(in srgb, var(--pico-primary) 15%, transparent);
  color: var(--pico-primary);
  display: grid;
  place-items: center;
  font-size: 1.4rem;
  font-weight: 700;
  border: 2px solid color-mix(in srgb, var(--pico-primary) 30%, transparent);
}

.status-dot {
  position: absolute;
  bottom: 3px;
  right: 3px;
  width: 12px;
  height: 12px;
  border-radius: 50%;
  border: 2px solid var(--pico-card-background-color);

  &.active  { background: #22c55e; }
  &.inactive { background: #ef4444; }
}

.user-name {
  font-size: 1.1rem;
  font-weight: 700;
  line-height: 1.2;
}

.user-email {
  font-size: 0.875rem;
  color: var(--pico-muted-color);
  margin-top: 0.2rem;
}

.user-type-badge {
  display: inline-block;
  margin-top: 0.5rem;
  padding: 0.15rem 0.6rem;
  border-radius: 999px;
  background: color-mix(in srgb, var(--pico-primary) 10%, transparent);
  border: 1px solid color-mix(in srgb, var(--pico-primary) 25%, transparent);
  color: var(--pico-primary);
  font-size: 0.72rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

/* ── Info list ──────────────────────────────────────────────────── */
.info-list {
  display: flex;
  flex-direction: column;
  gap: 0;
  margin: 0;
}

.info-row {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  gap: 1rem;
  padding: 0.5rem 0;
  border-bottom: 1px solid var(--pico-card-border-color);

  &:last-child { border-bottom: none; }

  dt {
    font-size: 0.8rem;
    color: var(--pico-muted-color);
    flex-shrink: 0;
  }

  dd {
    font-size: 0.875rem;
    font-weight: 500;
    text-align: right;
    margin: 0;
    overflow-wrap: anywhere;
    min-width: 0;
  }
}

.mono {
  font-family: monospace;
  font-size: 0.75rem !important;
  color: var(--pico-muted-color);
}

/* ── Badge ──────────────────────────────────────────────────────── */
.badge {
  display: inline-block;
  padding: 0.15rem 0.55rem;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 600;

  &--green {
    background: color-mix(in srgb, #22c55e 12%, transparent);
    border: 1px solid color-mix(in srgb, #22c55e 30%, transparent);
    color: #16a34a;
  }

  &--red {
    background: color-mix(in srgb, #ef4444 12%, transparent);
    border: 1px solid color-mix(in srgb, #ef4444 30%, transparent);
    color: #dc2626;
  }
}

/* ── Roles ──────────────────────────────────────────────────────── */
.roles-list,
.modules-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.role-item,
.module-item {
  display: flex;
  align-items: flex-start;
  gap: 0.65rem;
}

.role-icon,
.module-icon {
  font-size: 1.1rem;
  color: var(--pico-primary);
  flex-shrink: 0;
  margin-top: 0.1rem;
}

.role-name,
.module-name {
  font-size: 0.875rem;
  font-weight: 600;
}

.role-desc,
.module-desc {
  font-size: 0.78rem;
  color: var(--pico-muted-color);
  margin-top: 0.1rem;
}

/* ── Empty ──────────────────────────────────────────────────────── */
.empty-state {
  color: var(--pico-muted-color);
  font-size: 0.9rem;
}

/* ── Responsive ─────────────────────────────────────────────────── */
@media (max-width: 640px) {
  .profile-grid {
    grid-template-columns: 1fr;
  }
}
</style>
