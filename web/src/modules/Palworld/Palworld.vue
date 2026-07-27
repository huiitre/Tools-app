<script setup lang="ts">
import { onMounted } from 'vue'
import PalworldNav from '@/modules/Palworld/shared/components/PalworldNav.vue'
import { usePaldexStore } from '@/modules/Palworld/paldex/paldex.store'

// Catalogue Paldex (pals/éléments/aptitudes) partagé par plusieurs onglets (Paldex, Tierlist) :
// chargé une fois à l'entrée sur /palworld, gardé en mémoire (comme le cache prix Dofus).
onMounted(() => {
  usePaldexStore().ensureLoaded()
})
</script>

<template>
  <div id="palworld">
    <PalworldNav />
    <section class="palworld-content">
      <router-view v-slot="{ Component }">
        <Transition name="palworld-page" mode="out-in">
          <component :is="Component" />
        </Transition>
      </router-view>
    </section>
  </div>
</template>

<style lang="scss" scoped>
#palworld {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.palworld-content {
  flex: 1;
  overflow: auto;
}

.palworld-page-enter-active,
.palworld-page-leave-active {
  transition: opacity 0.15s ease, transform 0.15s ease;
}

.palworld-page-enter-from,
.palworld-page-leave-to {
  opacity: 0;
  transform: translateY(4px);
}
</style>
