<script setup lang="ts">
import type { ModuleType } from '@/modules/Auth/auth.store'
import { RouterLink, useRouter } from 'vue-router'

defineProps<{
  modules?: Array<ModuleType>
}>()

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'logout'): void
}>()

const router = useRouter()

const onLogoutClick = () => {
  emit('logout')
}

const isModuleEnabled = (module: ModuleType) => {
  return module.active && router.hasRoute(module.code)
}
</script>

<template>
  <nav class="burger-nav">
    <!-- CORE NAV -->
    <RouterLink
      to="/"
      class="nav-item"
      @click="emit('close')"
    >
      <i class="mdi mdi-home"></i>
      Home
    </RouterLink>

    <RouterLink
      to="/settings"
      class="nav-item"
      @click="emit('close')"
    >
      <i class="mdi mdi-cog"></i>
      Paramètres
    </RouterLink>

    <button
      class="nav-item"
      @click="onLogoutClick"
    >
      <i class="mdi mdi-logout"></i>
      Déconnexion
    </button>

    <!-- MODULES -->
    <template v-if="modules?.length">
      <hr />

      <div class="modules-title">Modules</div>

      <template
        v-for="module in modules"
        :key="module.id"
      >
        <!-- Actif -->
        <RouterLink
          v-if="isModuleEnabled(module)"
          :to="`/${module.code}`"
          class="nav-item"
          @click="emit('close')"
        >
          <i class="mdi mdi-apps"></i>
          {{ module.name }}
        </RouterLink>

        <!-- Inactif -->
        <div
          v-else
          class="nav-item nav-item--disabled"
          aria-disabled="true"
        >
          <i class="mdi mdi-apps"></i>
          {{ module.name }}
        </div>
      </template>
    </template>
  </nav>
</template>

<style lang="scss" scoped>
.burger-nav {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.nav-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;

  padding: 0.5rem 0.6rem;
  border-radius: 0.35rem;

  background: transparent;
  border: 0;

  color: inherit;
  text-decoration: none;
  cursor: pointer;

  transition: background-color 120ms ease, color 120ms ease;
}

.nav-item:hover {
  background: var(--pico-muted-border-color);
}

/* Désactivé */
.nav-item--disabled {
  opacity: 0.5;
  cursor: default;
}

.nav-item--disabled:hover {
  background: transparent;
}

.modules-title {
  font-weight: 600;
  margin: 0.25rem 0;
}

.nav-item.router-link-active {
  color: var(--pico-primary);
}

.nav-item.router-link-active i {
  color: var(--pico-primary);
}
</style>
