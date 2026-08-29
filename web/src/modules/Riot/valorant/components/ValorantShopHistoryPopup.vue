<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'
import { useFloating, offset, flip, shift, autoUpdate } from '@floating-ui/vue'
import { useRiotStore } from '@/modules/Riot/riot.store'

const riotStore = useRiotStore()

const isOpen = ref(false)
const reference = ref<HTMLElement | null>(null)
const floating = ref<HTMLElement | null>(null)

const { floatingStyles } = useFloating(reference, floating, {
  placement: 'bottom-end',
  middleware: [offset(10), flip(), shift()],
  whileElementsMounted: autoUpdate,
})

function togglePopup() {
  isOpen.value = !isOpen.value
}

function closePopup() {
  isOpen.value = false
}

const handleClickOutside = (e: MouseEvent) => {
  const target = e.target as HTMLElement
  if (isOpen.value && 
      floating.value && !floating.value.contains(target) && 
      reference.value && !reference.value.contains(target)) {
    closePopup()
  }
}

const onScroll = (e: Event) => {
  if (isOpen.value && !floating.value?.contains(e.target as Node)) {
    closePopup()
  }
}

onMounted(() => {
  window.addEventListener('mousedown', handleClickOutside)
  window.addEventListener('scroll', onScroll, { capture: true })
})

onBeforeUnmount(() => {
  window.removeEventListener('mousedown', handleClickOutside)
  window.removeEventListener('scroll', onScroll, { capture: true })
})

function formatDate(dateStr: string) {
  const date = new Date(dateStr)
  return new Intl.DateTimeFormat('fr-FR', { day: 'numeric', month: 'long', year: 'numeric' }).format(date)
}
</script>

<template>
  <div class="history-container">
    <button 
      ref="reference"
      class="history-btn" 
      :class="{ 'history-btn--active': isOpen }"
      @click="togglePopup"
    >
      <i class="mdi mdi-history" />
      Historique
    </button>

    <Teleport to="body">
      <Transition name="pop">
        <div 
          v-if="isOpen"
          ref="floating"
          :style="floatingStyles"
          class="history-popup"
        >
          <div class="popup-header">
            <i class="mdi mdi-clock-outline" />
            Historique du shop
          </div>

          <div class="popup-content custom-scrollbar">
            <div v-if="!riotStore.storeHistory.length" class="empty-state">
              Aucun historique disponible
            </div>
            
            <div 
              v-for="entry in riotStore.storeHistory" 
              :key="entry.date" 
              class="history-day"
            >
              <div class="day-label">{{ formatDate(entry.date) }}</div>
              <div class="day-skins">
                <div 
                  v-for="skin in entry.skins" 
                  :key="skin.id" 
                  class="skin-mini-card"
                >
                  <div class="skin-mini-img">
                    <img :src="skin.iconUrl ?? ''" :alt="skin.name" loading="lazy" />
                  </div>
                  <div class="skin-mini-name">{{ skin.name }}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<style lang="scss" scoped>
.history-container {
  display: inline-block;
}

.history-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.8rem;
  padding: 0.35rem 0.75rem;
  background: color-mix(in srgb, var(--pico-muted-border-color) 40%, transparent);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: 6px;
  color: inherit;
  cursor: pointer;
  width: auto;
  transition: all 0.2s ease;
  margin: 0;

  &:hover, &--active {
    border-color: var(--pico-primary);
    background: color-mix(in srgb, var(--pico-primary) 10%, transparent);
    color: var(--pico-primary);
  }
}

.history-popup {
  z-index: 1000;
  width: 320px;
  max-height: 480px;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 10px;
  box-shadow: 0 12px 32px rgba(0, 0, 0, 0.4), 0 0 0 1px rgba(255, 255, 255, 0.05);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  backdrop-filter: blur(12px);
}

.popup-header {
  padding: 0.75rem 1rem;
  background: rgba(255, 255, 255, 0.03);
  border-bottom: 1px solid var(--pico-card-border-color);
  font-size: 0.85rem;
  font-weight: 700;
  color: var(--pico-muted-color);
  display: flex;
  align-items: center;
  gap: 0.5rem;

  i { color: var(--pico-primary); }
}

.popup-content {
  flex: 1;
  overflow-y: auto;
  padding: 1rem;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.history-day {
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
}

.day-label {
  font-size: 0.7rem;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--pico-primary);
  opacity: 0.8;
  padding-bottom: 0.2rem;
  border-bottom: 1px solid color-mix(in srgb, var(--pico-primary) 20%, transparent);
}

.day-skins {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.5rem;
}

.skin-mini-card {
  background: rgba(0, 0, 0, 0.2);
  border-radius: 6px;
  padding: 0.4rem;
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  cursor: default;
}

.skin-mini-img {
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  
  img {
    max-width: 100%;
    max-height: 100%;
    object-fit: contain;
  }
}

.skin-mini-name {
  font-size: 0.65rem;
  color: var(--pico-muted-color);
  text-align: center;
  line-height: 1.2;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.empty-state {
  text-align: center;
  padding: 2rem 0;
  font-size: 0.8rem;
  color: var(--pico-muted-color);
  font-style: italic;
}

/* Animations */
.pop-enter-active, .pop-leave-active {
  transition: transform 0.2s cubic-bezier(0.34, 1.56, 0.64, 1), opacity 0.2s ease;
}
.pop-enter-from, .pop-leave-to {
  opacity: 0;
  transform: scale(0.95) translateY(-5px);
}

.custom-scrollbar {
  &::-webkit-scrollbar { width: 4px; }
  &::-webkit-scrollbar-track { background: transparent; }
  &::-webkit-scrollbar-thumb {
    background: var(--pico-muted-border-color);
    border-radius: 10px;
  }
}
</style>
