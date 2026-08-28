<script setup lang="ts">
import { computed, ref } from 'vue'
import type { GameServerAction, GameServerLivePlayer } from '../types/gameServers.types'

const props = defineProps<{
  action: GameServerAction
  players: GameServerLivePlayer[]
  running: boolean
}>()

const emit = defineEmits<{ submit: [parameters: Record<string, string>] }>()

// Un champ par paramètre déclaré : le formulaire est construit depuis la description de l'action,
// aucun code de jeu n'est écrit ici.
const values = ref<Record<string, string>>(
  Object.fromEntries(props.action.parameters.map(parameter => [parameter.name, '']))
)

const canSubmit = computed(() =>
  props.action.parameters.every(parameter => !parameter.required || values.value[parameter.name]?.trim())
)

function submit() {
  if (!canSubmit.value || props.running) return
  emit('submit', { ...values.value })
  for (const parameter of props.action.parameters) values.value[parameter.name] = ''
}
</script>

<template>
  <form class="action-card" :class="{ 'action-card--danger': action.dangerous }" @submit.prevent="submit">
    <span class="action-label">
      <i class="mdi" :class="action.icon" aria-hidden="true" />
      {{ action.label }}
    </span>

    <template v-for="parameter in action.parameters" :key="parameter.name">
      <select v-if="parameter.type === 'player'" v-model="values[parameter.name]" :disabled="running">
        <option value="">{{ players.length ? 'Choisir un joueur' : 'Aucun joueur connecté' }}</option>
        <option v-for="player in players" :key="player.id ?? player.name" :value="player.id ?? player.name">
          {{ player.name }}
        </option>
      </select>

      <input
        v-else
        v-model="values[parameter.name]"
        :type="parameter.type === 'number' ? 'number' : 'text'"
        :placeholder="parameter.placeholder ?? parameter.label"
        :disabled="running"
      />
    </template>

    <button type="submit" :class="{ danger: action.dangerous }" :disabled="!canSubmit || running">
      {{ action.label }}
    </button>
  </form>
</template>

<style lang="scss" scoped>
.action-card {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  padding: 1rem;
  margin: 0;
  background: var(--pico-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 10px;

  input, select {
    margin: 0;
    padding: 0.4rem 0.6rem;
    font-size: 0.85rem;
    height: auto;
  }

  button {
    margin: 0.25rem 0 0;
    padding: 0.4rem 0.8rem;
    font-size: 0.85rem;
    width: auto;
    align-self: flex-start;
  }
}

.action-card--danger {
  border-color: color-mix(in srgb, #e53e3e 30%, var(--pico-card-border-color));
}

.action-label {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  font-size: 0.85rem;
  font-weight: 600;

  i { font-size: 1rem; color: var(--pico-muted-color); }
}

button.danger {
  background: #e53e3e;
  border-color: #e53e3e;
  color: white;

  &:hover:not(:disabled) {
    background: color-mix(in srgb, #e53e3e 85%, black);
  }
}
</style>
