<script setup lang="ts">
import { onMounted } from 'vue'
import RiotNav from '@/modules/Riot/shared/components/RiotNav.vue'
import ValorantLinkAccountModal from '@/modules/Riot/valorant/components/ValorantLinkAccountModal.vue'
import { useValorantAccounts } from '@/modules/Riot/valorant/composables/useValorantAccounts'

const { ensureLoaded } = useValorantAccounts()

onMounted(() => {
  ensureLoaded()
})
</script>

<template>
  <div id="riot">
    <RiotNav />

    <section class="riot-content">
      <router-view v-slot="{ Component }">
        <Transition name="riot-page" mode="out-in">
          <component :is="Component" />
        </Transition>
      </router-view>
    </section>

    <ValorantLinkAccountModal />
  </div>
</template>

<style lang="scss" scoped>
.riot-page-enter-active,
.riot-page-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.riot-page-enter-from {
  opacity: 0;
  transform: translateY(6px);
}

.riot-page-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}
</style>
