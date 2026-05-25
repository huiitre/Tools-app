<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import CodenameNav from '@/modules/Codename/shared/components/CodenameNav.vue'

const route = useRoute()
const showNav = computed(() => !route.meta.hideNav)
</script>

<template>
  <div id="codename">
    <CodenameNav v-if="showNav" />

    <section class="codename-content" :class="{ 'no-nav': !showNav }">
      <router-view v-slot="{ Component }">
        <Transition name="codename-page" mode="out-in">
          <component :is="Component" />
        </Transition>
      </router-view>
    </section>
  </div>
</template>

<style lang="scss" scoped>
.codename-page-enter-active,
.codename-page-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.codename-page-enter-from {
  opacity: 0;
  transform: translateY(6px);
}

.codename-page-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}
</style>
