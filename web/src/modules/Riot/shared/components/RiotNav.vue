<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()

const parentRoute = computed(() =>
  route.matched.find(r => r.name === 'riot')
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
  <nav class="riot-nav">
    <div class="riot-nav-spacer" />

    <ul class="riot-nav-list">
      <li
        v-for="tab in tabs"
        :key="tab.name"
        class="riot-nav-item"
        :class="{ active: isActive(tab.name) }"
        @click="goTo(tab.name)"
      >
        {{ tab.label }}
      </li>
    </ul>

    <div class="riot-nav-spacer" />
  </nav>
</template>

<style lang="scss" scoped>
.riot-nav {
  top: var(--header-height, 56px);
  left: 0;
  right: 0;
  height: 52px;

  background-color: var(--pico-background-color);

  display: flex;
  align-items: center;
}

.riot-nav-spacer {
  flex: 1 0 0;
}

.riot-nav-list {
  display: flex;
  gap: 2.5rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.riot-nav-item {
  position: relative;
  cursor: pointer;

  font-size: 0.85rem;
  letter-spacing: 0.08em;
  text-transform: uppercase;

  color: var(--pico-muted-color);
  padding: 0.5rem 0;

  transition: color 0.2s ease;
}

.riot-nav-item::after {
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

.riot-nav-item:hover {
  color: var(--pico-color);
}

.riot-nav-item.active {
  color: var(--pico-primary);
}

.riot-nav-item.active::after {
  transform: scaleX(1);
}
</style>
