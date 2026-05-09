<script setup lang="ts">
import { ref, computed } from 'vue'

const props = defineProps<{
  modelValue: string
  placeholder?: string
  autocomplete?: string
  name?: string
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
}>()

const visible = ref(false)

const type = computed(() => (visible.value ? 'text' : 'password'))

const toggle = () => {
  visible.value = !visible.value
}
</script>

<template>
  <div class="password-input">
    <input
      :type="type"
      :value="modelValue"
      :placeholder="placeholder"
      :autocomplete="autocomplete"
      :name="name"
      @input="emit('update:modelValue', ($event.target as HTMLInputElement).value)"
      required
    />

    <button
      type="button"
      class="toggle"
      @click="toggle"
      aria-label="Afficher / masquer le mot de passe"
    >
      <i
        class="fa-solid"
        :class="visible ? 'fa-eye-slash' : 'fa-eye'"
        aria-hidden="true"
      ></i>
    </button>
  </div>
</template>

<style scoped>
.password-input {
  position: relative;
}

.password-input input {
  padding-right: 2.5rem;
}

.toggle {
  position: absolute;
  bottom: 12%;
  right: 0.75rem;
  transform: translateY(-50%);
  background: none;
  border: none;
  padding: 0;
  cursor: pointer;
  line-height: 1;
  opacity: 0.6;
}

.toggle i {
  font-size: 1rem;
  color: var(--pico-form-element-color);
}

.toggle:hover {
  opacity: 1;
}
</style>
