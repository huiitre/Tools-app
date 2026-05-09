<script setup lang="ts">
import { onBeforeUnmount, onMounted, watch } from 'vue'

import DofusNav from '@/modules/Dofus/shared/components/DofusNav.vue'
import toast from '@/services/toast'
import { useFetchGameVersions } from '@/modules/Dofus/game/fetch/game.fetch'
import { useFetchGameServers } from '@/modules/Dofus/game/fetch/game.fetch'
import { useDofusStore } from '@/modules/Dofus/dofus.store'
import { useDofusConfigStore } from '@/modules/Dofus/preferences/preferences.store'
import { useSnifferConfigStore } from '@/modules/Dofus/shared/snifferConfig.store'
import { useItemPrices } from '@/modules/Dofus/almanax/composables/useItemPrices'
import { useWorkshopStore } from '@/modules/Dofus/workshop/store/workshop.store'

const { startAutoRefresh, stopAutoRefresh } = useItemPrices()

const dofusStore = useDofusStore()
const dofusConfig = useDofusConfigStore()
const snifferConfig = useSnifferConfigStore()

const loadGameServers = async () => {
  if (dofusStore.currentGameVersionId === null) return

  const { data: gameServers } = await useFetchGameServers()
  dofusStore.setGameServers(gameServers)

  if (
    dofusStore.currentGameServerId === null &&
    gameServers.length > 0
  ) {
    dofusStore.setCurrentGameServer(gameServers[0].id)
  }
}

const loadDofusModuleData = async () => {
  try {
    dofusStore.hydrateFromStorage()
    dofusConfig.hydrateFromStorage()

    const { data: gameVersions } = await useFetchGameVersions()
    dofusStore.setGameVersions(gameVersions)

    if (dofusStore.currentGameVersionId === null && gameVersions.length > 0) {
      dofusStore.setCurrentGameVersion(gameVersions[0].id)
    }

  } catch (e: any) {
    console.error('Dofus.vue | loadDofusModuleData', e)
    toast.error(e?.message || 'Erreur lors du chargement des données du module Dofus')
  }
}

watch(
  () => dofusStore.currentGameVersionId,
  async () => {
    if (dofusStore.currentGameVersionId === null) return

    dofusStore.setGameServers([])
    await loadGameServers()

    const workshopStore = useWorkshopStore()
    await Promise.all([
      workshopStore.fetchTags(),
      workshopStore.fetchWorkshops()
    ])
  },
  { immediate: true }
)

onMounted(() => {
  loadDofusModuleData()
  snifferConfig.hydrate()
  startAutoRefresh() 
})

onBeforeUnmount(() => {
  stopAutoRefresh()
})

</script>

<template>
  <div id="dofus">
    <DofusNav />

    <section class="dofus-content">
      <router-view v-slot="{ Component }">
        <Transition name="dofus-page" mode="out-in">
          <component :is="Component" />
        </Transition>
      </router-view>
    </section>
  </div>
</template>

<style lang="scss" scoped>
/* Transitions */
.dofus-page-enter-active,
.dofus-page-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.dofus-page-enter-from {
  opacity: 0;
  transform: translateY(6px);
}

.dofus-page-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}
</style>
