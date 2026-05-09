<script setup lang="ts">
defineProps<{
  enabledCount: number
  total: number
  scanning: boolean
}>()

defineEmits<{
  scan: []
  toggleSettings: []
}>()
</script>

<template>
  <div class="header">
    <div class="header__title">
      <span class="mdi mdi-controller-classic" />
      Dofus Switcher
      <span class="header__count">{{ enabledCount }}/{{ total }}</span>
    </div>
    <div class="header__actions">
      <button class="btn" :disabled="scanning" @click="$emit('scan')">
        <span class="mdi" :class="scanning ? 'mdi-loading spin' : 'mdi-refresh'" />
      </button>
      <button class="btn" @click="$emit('toggleSettings')">
        <span class="mdi mdi-tune-variant" />
      </button>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 14px 10px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.07);
  flex-shrink: 0;

  &__title {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 14px;
    font-weight: 600;
    letter-spacing: 0.02em;

    .mdi {
      color: var(--pico-primary);
      font-size: 18px;
    }
  }

  &__count {
    font-size: 11px;
    font-weight: 400;
    color: rgba(255, 255, 255, 0.35);
    margin-left: 2px;
  }

  &__actions {
    display: flex;
    gap: 4px;
  }
}

.btn {
  background: none;
  border: none;
  padding: 6px;
  border-radius: 6px;
  cursor: pointer;
  color: rgba(255, 255, 255, 0.55);
  display: flex;
  align-items: center;
  transition: background 0.15s, color 0.15s;
  margin: 0;

  &:hover:not(:disabled) {
    background: rgba(255, 255, 255, 0.08);
    color: #fff;
  }

  &:disabled {
    opacity: 0.4;
    cursor: not-allowed;
  }

  .mdi {
    font-size: 16px;
  }
}

.spin {
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}
</style>