<script setup lang="ts">
import { onMounted } from 'vue'
import TemtemNav from '@/modules/Temtem/shared/components/TemtemNav.vue'
import { useTemtemdexStore } from '@/modules/Temtem/temtemdex/temtemdex.store'

// Catalogue (Temtem + types) partagé par plusieurs onglets : chargé une fois à l'entrée sur
// /temtem et gardé en mémoire, comme le Paldex côté Palworld.
onMounted(() => {
  useTemtemdexStore().ensureLoaded()
})
</script>

<template>
  <div id="temtem">
    <TemtemNav />
    <section class="temtem-content">
      <router-view v-slot="{ Component }">
        <Transition name="temtem-page" mode="out-in">
          <component :is="Component" />
        </Transition>
      </router-view>
    </section>
  </div>
</template>

<style lang="scss" scoped>
#temtem {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.temtem-content {
  flex: 1;
  overflow: auto;
}

.temtem-page-enter-active,
.temtem-page-leave-active {
  transition: opacity 0.15s ease, transform 0.15s ease;
}

.temtem-page-enter-from,
.temtem-page-leave-to {
  opacity: 0;
  transform: translateY(4px);
}
</style>
