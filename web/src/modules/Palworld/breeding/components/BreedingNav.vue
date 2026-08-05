<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()

const parentRoute = computed(() =>
  route.matched.find(r => r.name === 'palworld-breeding')
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

function goTo(tabName: string) {
  if (route.name !== tabName) {
    router.push({ name: tabName, query: route.query })
  }
}
</script>

<template>
  <nav class="breeding-nav">
    <button
      v-for="tab in tabs"
      :key="tab.name"
      type="button"
      class="breeding-nav-item"
      :class="{ active: isActive(tab.name) }"
      @click="goTo(tab.name)"
    >
      {{ tab.label }}
    </button>
  </nav>
</template>

<style lang="scss" scoped>
.breeding-nav {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  flex-wrap: wrap;
  padding-bottom: 0.75rem;
  margin-bottom: 0.5rem;
  border-bottom: 1px solid var(--pico-card-border-color);
}

.breeding-nav-item {
  margin: 0;
  width: auto;
  padding: 0.4rem 1rem;
  font-size: 0.8rem;
  font-weight: 600;
  border-radius: 999px;
  border: 1px solid var(--pico-card-border-color);
  background: transparent;
  color: var(--pico-muted-color);
  cursor: pointer;
  transition: all 0.15s ease;

  &:hover {
    color: var(--pico-contrast);
    border-color: var(--pico-primary);
  }

  &.active {
    color: var(--pico-primary);
    border-color: var(--pico-primary);
    background: color-mix(in srgb, var(--pico-primary) 10%, transparent);
  }
}
</style>
