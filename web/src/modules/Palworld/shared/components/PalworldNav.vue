<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import PalworldConfigButton from './PalworldConfigButton.vue'
import PalworldServerPlayerSelector from '../../server/components/PalworldServerPlayerSelector.vue'

const route = useRoute()
const router = useRouter()

const parentRoute = computed(() =>
  route.matched.find(r => r.name === 'palworld')
)

const tabs = computed(() => {
  if (!parentRoute.value?.children) return []
  return parentRoute.value.children.map(child => ({
    name: child.name as string,
    label: child.meta?.label as string,
  }))
})

const isActive = (tabName: string) =>
  route.matched.some(r => r.name === tabName)

const goTo = (tabName: string) => {
  if (route.name !== tabName) {
    router.push({ name: tabName })
  }
}
</script>

<template>
  <nav class="palworld-nav">
    <div class="palworld-nav-spacer palworld-nav-spacer--left">
      <PalworldServerPlayerSelector />
    </div>

    <ul class="palworld-nav-list">
      <li
        v-for="tab in tabs"
        :key="tab.name"
        class="palworld-nav-item"
        :class="{ active: isActive(tab.name) }"
        @click="goTo(tab.name)"
      >
        {{ tab.label }}
      </li>
    </ul>

    <div class="palworld-nav-spacer palworld-nav-spacer--right">
      <PalworldConfigButton />
    </div>
  </nav>
</template>

<style lang="scss" scoped>
.palworld-nav {
  top: var(--header-height, 56px);
  left: 0;
  right: 0;
  height: 52px;

  background-color: var(--pico-background-color);

  display: flex;
  align-items: center;
}

.palworld-nav-spacer {
  flex: 1 0 0;
}

.palworld-nav-spacer--left {
  padding-left: .75rem;
}

.palworld-nav-spacer--right {
  display: flex;
  gap: .5rem;
  justify-content: flex-end;
  padding-right: 0.75rem;
}

.palworld-nav-list {
  display: flex;
  gap: 2.5rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.palworld-nav-item {
  position: relative;
  cursor: pointer;

  font-size: 0.85rem;
  letter-spacing: 0.08em;
  text-transform: uppercase;

  color: var(--pico-muted-color);
  padding: 0.5rem 0;

  transition: color 0.2s ease;
}

.palworld-nav-item::after {
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

.palworld-nav-item:hover {
  color: var(--pico-color);
}

.palworld-nav-item.active {
  color: var(--pico-primary);
}

.palworld-nav-item.active::after {
  transform: scaleX(1);
}
</style>
