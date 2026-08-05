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

    <div class="page-content">
      <NotAvailableOnScreen v-if="isBlocked" />
      <slot v-else />
    </div>

    <Footer />
  </div>
</template>

<style lang="scss" scoped>
.page {
  height: 100dvh;
  min-height: 0;
  width: 100%;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.page-content {
  flex: 1;
  min-height: 0;
  overflow: auto;
}
</style>
