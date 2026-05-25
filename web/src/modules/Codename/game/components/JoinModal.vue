<script setup lang="ts">
import { ref } from 'vue'

const emit = defineEmits<{
  join: [nickname: string]
}>()

const nickname = ref('')
const error = ref('')

function submit() {
  if (!nickname.value.trim()) {
    error.value = 'Un pseudo est requis.'
    return
  }
  emit('join', nickname.value.trim())
}
</script>

<template>
  <div class="modal-overlay">
    <div class="modal">
      <h2>Rejoindre la partie</h2>
      <p class="hint">Entrez un pseudo pour rejoindre cette session Codename.</p>

      <div class="field">
        <input
          v-model="nickname"
          placeholder="Votre pseudo…"
          maxlength="50"
          :class="{ invalid: error }"
          @keydown.enter="submit"
          autofocus
        />
        <span v-if="error" class="error-msg">{{ error }}</span>
      </div>

      <button class="join-btn" @click="submit">Rejoindre</button>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.55);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 100;
}

.modal {
  background: var(--pico-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  padding: 2rem;
  width: 100%;
  max-width: 380px;

  h2 { margin-top: 0; font-size: 1.1rem; margin-bottom: 0.25rem; }
}

.hint {
  font-size: 0.85rem;
  color: var(--pico-muted-color);
  margin-bottom: 1.25rem;
}

.field {
  margin-bottom: 1rem;

  input {
    width: 100%;
    padding: 0.55rem 0.75rem;
    font-size: 0.9rem;
    border: 1px solid var(--pico-muted-border-color);
    border-radius: var(--pico-border-radius);
    background: var(--pico-card-background-color);
    color: var(--pico-color);
    box-sizing: border-box;

    &.invalid { border-color: #ef4444; }
  }
}

.error-msg {
  display: block;
  font-size: 0.78rem;
  color: #ef4444;
  margin-top: 0.3rem;
}

.join-btn {
  width: 100%;
  background: var(--pico-primary);
  color: var(--pico-primary-inverse);
  border: none;
  border-radius: var(--pico-border-radius);
  padding: 0.65rem;
  font-size: 0.9rem;
  cursor: pointer;
  transition: opacity 0.2s;

  &:hover { opacity: 0.85; }
}
</style>