<script setup lang="ts">
import EliteDangerousNav from '@/modules/EliteDangerous/shared/components/EliteDangerousNav.vue'
</script>

<template>
  <div id="elite-dangerous">
    <EliteDangerousNav />
    <section class="ed-content">
      <router-view v-slot="{ Component }">
        <Transition name="ed-page" mode="out-in">
          <component :is="Component" />
        </Transition>
      </router-view>
    </section>
  </div>
</template>

<style lang="scss" scoped>
#elite-dangerous {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.ed-content {
  flex: 1;
  overflow: auto;
}

.ed-page-enter-active,
.ed-page-leave-active {
  transition: opacity 0.15s ease, transform 0.15s ease;
}

.ed-page-enter-from,
.ed-page-leave-to {
  opacity: 0;
  transform: translateY(4px);
}
</style>