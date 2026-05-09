<script setup lang="ts">
import Header from '@/components/Header/Header.vue'
import Footer from '@/components/Footer/Footer.vue'
import { useRoute } from 'vue-router';
import { useScreen } from '@/composables/useScreen';
import NotAvailableOnScreen from '@/components/NotAvailableOnScreen.vue';
import { computed, watch } from 'vue';

const route = useRoute();
const { isDesktop } = useScreen();

const isBlocked = computed(() => {
  return route.meta.desktopOnly === true && !isDesktop.value
})

const isStandalone = computed(() => route.meta.standalone === true)

</script>

<template>
  <template v-if="isStandalone">
    <slot />
  </template>
  <div v-else class="page">
    <Header />

    <NotAvailableOnScreen v-if="isBlocked" />
    <slot v-else />

    <Footer />
  </div>
</template>

<style lang="scss" scoped>
.page {
  min-height: 100dvh;
  width: 100%;

  display: flex;
  flex-direction: column;
}
</style>
