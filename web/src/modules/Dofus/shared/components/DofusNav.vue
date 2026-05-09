<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import DofusGameSelect from './DofusGameSelect.vue'
import DofusConfigButton from './DofusConfigButton.vue'
import ProxyConfigButton from './ProxyConfigButton.vue'
import { useAuthStore } from '@/modules/Auth/auth.store'
import { RoleCode } from '@/modules/Auth/types/auth.types'
import DofusSwitcherButton from '@/modules/Dofus/switcher/components/DofusSwitcherButton.vue'

const route = useRoute()
const router = useRouter()

const authStore = useAuthStore()

const canAccessWorkshop = computed(() =>
  authStore.hasModuleAccess('DOFUS', RoleCode.USER)
)

const parentRoute = computed(() =>
  route.matched.find(r => r.name === 'dofus')
)

const tabs = computed(() => {
  if (!parentRoute.value?.children) return []

  return parentRoute.value.children.map(child => ({
    name: child.name as string,
    label: child.meta?.label as string,
    disabled: child.name === 'dofus-workshop' && !canAccessWorkshop.value
  }))
})

const isActive = (tabName: string) =>
  route.matched.some(r => r.name === tabName)

const goTo = (tabName: string) => {

  const tab = tabs.value.find(t => t.name === tabName)
  if (tab?.disabled) return

  if (route.name !== tabName) {
    router.push({ name: tabName })
  }
}
</script>

<template>
  <nav class="dofus-nav">
    <!-- LEFT -->
    <DofusGameSelect />

    <!-- CENTER -->
    <ul class="dofus-nav-list">
      <li
        v-for="tab in tabs"
        :key="tab.name"
        class="dofus-nav-item"
        :class="{ active: isActive(tab.name), disabled: tab.disabled }"
        @click="goTo(tab.name)"
      >
        {{ tab.label }}
      </li>
    </ul>

    <!-- RIGHT spacer (symétrie) -->
    <div class="dofus-nav-spacer">
      <DofusConfigButton />
      <ProxyConfigButton />
      <DofusSwitcherButton />
    </div>
  </nav>
</template>

<style lang="scss" scoped>
.dofus-nav {
  top: var(--header-height, 56px);
  left: 0;
  right: 0;
  height: 52px;

  background-color: var(--pico-background-color);

  display: flex;
  align-items: center;
}

.dofus-nav-spacer {
  display: flex;
  justify-content: flex-end;
  padding-right: 0.75rem; // optionnel, confort visuel
}

/* Left / Right symmetry for perfect centering */
.dofus-nav > :first-child,
.dofus-nav-spacer {
  flex: 1 0 0;
}

.dofus-nav-list {
  display: flex;
  gap: 2.5rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.dofus-nav-item {
  position: relative;
  cursor: pointer;

  font-size: 0.85rem;
  letter-spacing: 0.08em;
  text-transform: uppercase;

  color: var(--pico-muted-color);
  padding: 0.5rem 0;

  transition: color 0.2s ease;
}

.dofus-nav-item::after {
  content: '';
  position: absolute;
  left: 0;
  bottom: 0;

  width: 100%;
  height: 1px;

  background-color: var(--pico-primary);
  transform: scaleX(0);
  transform-origin: center;
  transition: transform 0.25s ease;
}

.dofus-nav-item:hover {
  color: var(--pico-color);
}

.dofus-nav-item.active {
  color: var(--pico-primary);
}

.dofus-nav-item.active::after {
  transform: scaleX(1);
}

.dofus-nav-item.disabled {
  opacity: 0.4;
  cursor: not-allowed;
  pointer-events: none;
}

</style>
