<script setup lang="ts">
import { Dialog, DialogPanel, TransitionChild, TransitionRoot } from '@headlessui/vue'

import BurgerUser from '@/components/Header/BurgerUser.vue'
import BurgerNav from '@/components/Header/BurgerNav.vue'
import { ModuleType } from '@/modules/Auth/auth.store';

defineProps<{
  open: boolean
  modules: Array<ModuleType>
  name: string
  email: string
  avatar?: string
}>()

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'logout'): void
}>()

</script>

<template>
  <TransitionRoot :show="open" as="template">
    <Dialog class="burger-root" @close="emit('close')">
      <!-- Overlay -->
      <TransitionChild
        as="template"
        enter="overlay-enter"
        enter-from="overlay-enter-from"
        enter-to="overlay-enter-to"
        leave="overlay-leave"
        leave-from="overlay-leave-from"
        leave-to="overlay-leave-to"
      >
        <div class="burger-overlay" />
      </TransitionChild>

      <!-- Panel -->
      <TransitionChild
        as="template"
        enter="panel-enter"
        enter-from="panel-enter-from"
        enter-to="panel-enter-to"
        leave="panel-leave"
        leave-from="panel-leave-from"
        leave-to="panel-leave-to"
      >
        <DialogPanel class="burger-panel">
          <!-- USER -->
          <BurgerUser
            :name="name"
            :email="email"
            :avatar="avatar"
          />

          <hr />

          <!-- NAV + MODULES -->
          <BurgerNav
            :modules="modules"
            @close="emit('close')"
            @logout="emit('logout')"
          />
        </DialogPanel>
      </TransitionChild>
    </Dialog>
  </TransitionRoot>
</template>

<style scoped>
/* ===== Root / overlay ===== */
.burger-root {
  position: fixed;
  inset: 0;
  z-index: 1100;
}

.burger-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.4);
}

/* ===== Panel ===== */
.burger-panel {
  position: fixed;
  top: 56px;
  left: 0;

  width: 100vw;
  height: 100dvh;
  padding-top: calc(var(--header-height, 0));
  padding-left: 1rem;

  background: var(--pico-background-color);
  color: var(--pico-color);

  display: flex;
  flex-direction: column;
  gap: 1rem;

  will-change: transform;
}

/* Desktop */
@media (min-width: 640px) {
  .burger-panel {
    width: 280px;
  }
}

/* ===== Panel slide ===== */
.panel-enter {
  transition: transform 300ms ease-out;
}
.panel-enter-from {
  transform: translateX(-100%);
}
.panel-enter-to {
  transform: translateX(0);
}

.panel-leave {
  transition: transform 200ms ease-in;
}
.panel-leave-from {
  transform: translateX(0);
}
.panel-leave-to {
  transform: translateX(-100%);
}

/* ===== Overlay fade ===== */
.overlay-enter {
  transition: opacity 200ms ease-out;
}
.overlay-enter-from {
  opacity: 0;
}
.overlay-enter-to {
  opacity: 1;
}

.overlay-leave {
  transition: opacity 150ms ease-in;
}
.overlay-leave-from {
  opacity: 1;
}
.overlay-leave-to {
  opacity: 0;
}
</style>
