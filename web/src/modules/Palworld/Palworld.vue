<script setup lang="ts">
import { onMounted, onUnmounted, watch } from 'vue'
import PalworldNav from '@/modules/Palworld/shared/components/PalworldNav.vue'
import { usePaldexStore } from '@/modules/Palworld/paldex/paldex.store'
import { usePalworldServerDataStore } from '@/modules/Palworld/server/serverData.store'
import { usePassiveSkillsStore } from '@/modules/Palworld/passives/passiveSkills.store'
import { usePalworldConfigStore } from '@/modules/Palworld/shared/palworldConfig.store'

const serverDataStore = usePalworldServerDataStore()
const configStore = usePalworldConfigStore()
let serverDataRefreshInterval: number | undefined

function restartServerDataRefresh() {
  if (serverDataRefreshInterval) clearInterval(serverDataRefreshInterval)
  serverDataRefreshInterval = window.setInterval(() => serverDataStore.refresh(), configStore.refreshIntervalSeconds * 1000)
}

// Catalogue Paldex (pals/éléments/aptitudes) partagé par plusieurs onglets (Paldex, Tierlist) :
// chargé une fois à l'entrée sur /palworld, gardé en mémoire (comme le cache prix Dofus).
onMounted(() => {
  configStore.hydrate()
  usePaldexStore().ensureLoaded()
  usePassiveSkillsStore().ensureLoaded()
  serverDataStore.ensureLoaded()
  restartServerDataRefresh()
})

watch(() => configStore.refreshIntervalSeconds, restartServerDataRefresh)

onUnmounted(() => {
  if (serverDataRefreshInterval) clearInterval(serverDataRefreshInterval)
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
  min-height: 0;
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
