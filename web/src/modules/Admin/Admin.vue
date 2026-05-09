<script setup lang="ts">
import AdminNav from '@/modules/Admin/shared/components/AdminNav.vue'
</script>

<template>
  <div id="admin">
    <AdminNav />

    <section class="admin-content">
      <router-view v-slot="{ Component }">
        <Transition name="admin-page" mode="out-in">
          <component :is="Component" />
        </Transition>
      </router-view>
    </section>
  </div>
</template>

<style lang="scss" scoped>
.admin-page-enter-active,
.admin-page-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.admin-page-enter-from {
  opacity: 0;
  transform: translateY(6px);
}

.admin-page-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}
</style>
