<script setup lang="ts">
import { useValorantAccounts } from '../composables/useValorantAccounts'
import { REGIONS } from '../composables/useValorantShop'
import ValorantAuthCard from './ValorantAuthCard.vue'

const { showLinkForm, linkError, closeLinkForm, submitLink } = useValorantAccounts()
</script>

<template>
  <Teleport to="body">
    <Transition name="modal">
      <div v-if="showLinkForm" class="overlay" @click.self="closeLinkForm">
        <ValorantAuthCard
          :error="linkError"
          :regions="REGIONS"
          @submit="({ token, region }) => submitLink(token, region)"
        />
      </div>
    </Transition>
  </Teleport>
</template>

<style lang="scss" scoped>
.overlay {
  position: fixed;
  inset: 0;
  z-index: 9500;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(0, 0, 0, 0.7);
  padding: 1rem;
}

.modal-enter-active { transition: opacity 0.2s ease, transform 0.2s cubic-bezier(0.22, 1, 0.36, 1); }
.modal-leave-active { transition: opacity 0.15s ease; }
.modal-enter-from { opacity: 0; transform: scale(0.96); }
.modal-leave-to { opacity: 0; }
</style>
