<script setup lang="ts">
defineProps<{
  year: number
  month: number
  canGoPrev: boolean
  canGoNext: boolean
  isCurrentMonth: boolean
}>()

const emit = defineEmits<{
  (e: 'prev'): void
  (e: 'next'): void
  (e: 'today'): void
}>()
</script>

<template>
  <header class="calendar-header">
    <button
      class="nav-btn prev"
      aria-label="Mois précédent"
      :disabled="!canGoPrev"
      @click="emit('prev')"
    >
      ◀
    </button>

    <div class="calendar-center">
      <h2 class="calendar-title">
        {{
          new Date(year, month).toLocaleDateString('fr-FR', {
            month: 'long',
            year: 'numeric'
          })
        }}
      </h2>

      <button
        class="today-btn"
        :class="{ active: isCurrentMonth }"
        @click="emit('today')"
      >
        Aujourd’hui
      </button>
    </div>

    <button
      class="nav-btn next"
      aria-label="Mois suivant"
      :disabled="!canGoNext"
      @click="emit('next')"
    >
      ▶
    </button>
  </header>
</template>

<style lang="scss" scoped>
.calendar-header {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 1rem;
  margin-bottom: 1.25rem;
}

/* ===== Centre ===== */

.calendar-center {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.25rem;
}

.calendar-title {
  margin: 0;
  width: 14ch;
  text-align: center;
  font-size: 1.2rem;
  font-weight: bold;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--pico-color);
  white-space: nowrap;
}

/* ===== Bouton Aujourd’hui ===== */

.today-btn {
  all: unset;
  cursor: pointer;
  font-size: 0.7rem;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--pico-muted-color);
  padding: 0.15rem 0.4rem;
  border-radius: 4px;

  &:hover {
    color: var(--pico-primary);
    background: var(--pico-card-background-color);
  }
}

.today-btn.active {
  color: var(--pico-primary);
  font-weight: 600;
}

/* ===== Boutons navigation ===== */

.nav-btn {
  all: unset;
  cursor: pointer;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  color: var(--pico-muted-color);

  &:hover:not(:disabled) {
    color: var(--pico-primary);
    background: var(--pico-card-background-color);
  }

  &:disabled {
    opacity: 0.3;
    cursor: not-allowed;
  }
}

.nav-btn.prev {
  justify-self: start;
}

.nav-btn.next {
  justify-self: end;
}
</style>
