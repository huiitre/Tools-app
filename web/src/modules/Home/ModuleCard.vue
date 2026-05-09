<script setup lang="ts">
import { useRouter } from 'vue-router'
import type { ModuleType } from '@/modules/Auth/auth.store'

const props = defineProps<{
  module: ModuleType
  enabled: boolean
}>()

const router = useRouter()

const onClick = () => {
  if (!props.enabled) return
  router.push({ name: props.module.code })
}
</script>

<template>
  <article
    class="module-card"
    :aria-disabled="!enabled"
    @click="onClick"
  >
    <!-- Header -->
    <header class="module-header">
      <h3>{{ module.name }}</h3>
      <span
        class="status-dot"
        :class="enabled ? 'active' : 'inactive'"
        :title="enabled ? 'Module actif' : 'Module indisponible'"
      />
    </header>

    <!-- Icon -->
    <div class="module-icon">
      <i class="mdi mdi-image" />
    </div>

    <!-- Content -->
    <div class="module-content">
      <!-- Description -->
      <p class="module-description">
        {{ module.description }}
      </p>

      <!-- Roles -->
      <section
        v-if="module.roles.length"
        class="module-roles"
      >
        <h5 class="roles-title">Rôles</h5>

        <ul class="roles-list">
          <li
            v-for="role in module.roles"
            :key="role.id"
            class="role-item"
          >
            <strong class="role-name">{{ role.name }}</strong>
            <p class="role-description">
              {{ role.description }}
            </p>
          </li>
        </ul>
      </section>
    </div>
  </article>
</template>

<style scoped lang="scss">
/* Card */
.module-card {
  cursor: pointer;
  border: 1px solid transparent;
  transition:
    border-color 0.15s ease,
    background-color 0.15s ease,
    box-shadow 0.15s ease;
}

/* Active module */
.module-card {
  border-color: var(--pico-muted-border-color);
}

.module-card[aria-disabled='false']:hover {
  // background-color: var(--pico-muted-background-color);
  box-shadow: 0 0 0 2px var(--pico-primary-border);
}

.module-card[aria-disabled='false'] .role-title {
  color: var(--pico-primary);
  opacity: 0.85;
}

/* Disabled module */
.module-card[aria-disabled='true'] {
  cursor: default;
  opacity: 0.6;
  background-color: var(--pico-muted-background-color);
}

/* Header (intouché) */
.module-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;

  & h3 {
    font-size: 1.5rem;
    font-weight: bold;
  }
}

.module-card[aria-disabled='false']:hover h3 {
  color: var(--pico-primary);
}

/* Status dot */
.status-dot {
  width: 1rem;
  height: 1rem;
  border-radius: 50%;
  display: inline-block;
}

.status-dot.active {
  background-color: var(--pico-color-green-550);
}

.status-dot.inactive {
  background-color: var(--pico-color-red-550);
}

/* Icon (intouchée) */
.module-icon {
  text-align: center;
  opacity: 0.35;

  & img,
  & i {
    width: 5rem;
    height: 5rem;
    font-size: 10rem;
  }
}

/* Content */
.module-content {
  padding: 1rem 1.25rem 1.25rem;
}

/* Description */
.module-description {
  margin-bottom: 1.25rem;
}

/* Roles section */
.module-roles {
  margin-top: 1.5rem;
}

/* Roles title */
.roles-title {
  font-size: 0.85rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

/* Roles list */
.roles-list {
  list-style: none;
  margin: 0;
  padding: 0;

  & li::marker {
    content: none;
  }
}

/* Role item */
.role-item {
  padding: 0.75rem 0;
}

.role-item + .role-item {
  border-top: 1px solid var(--pico-muted-border-color);
}

/* Role name */
.role-name {
  display: block;
  margin-bottom: 0.25rem;
}

/* Role description */
.role-description {
  margin: 0;
  font-size: 0.9rem;
  color: var(--pico-muted-color);
}
</style>
