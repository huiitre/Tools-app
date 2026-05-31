<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()

const parentRoute = computed(() =>
  route.matched.find(r => r.name === 'elite_dangerous')
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
  <nav class="ed-nav">
    <div class="ed-nav-spacer" />

    <ul class="ed-nav-list">
      <li
        v-for="tab in tabs"
        :key="tab.name"
        class="ed-nav-item"
        :class="{ active: isActive(tab.name) }"
        @click="goTo(tab.name)"
      >
        {{ tab.label }}
      </li>
    </ul>

    <div class="ed-nav-spacer" />
  </nav>
</template>

<style lang="scss" scoped>
.ed-nav {
  top: var(--header-height, 56px);
  left: 0;
  right: 0;
  height: 52px;

  background-color: var(--pico-background-color);

  display: flex;
  align-items: center;
}

.ed-nav-spacer {
  flex: 1 0 0;
}

.ed-nav-list {
  display: flex;
  gap: 2.5rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.ed-nav-item {
  position: relative;
  cursor: pointer;

  font-size: 0.85rem;
  letter-spacing: 0.08em;
  text-transform: uppercase;

  color: var(--pico-muted-color);
  padding: 0.5rem 0;

  transition: color 0.2s ease;
}

.ed-nav-item::after {
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

.ed-nav-item:hover {
  color: var(--pico-color);
}

.ed-nav-item.active {
  color: var(--pico-primary);
}

.ed-nav-item.active::after {
  transform: scaleX(1);
}
</style>
