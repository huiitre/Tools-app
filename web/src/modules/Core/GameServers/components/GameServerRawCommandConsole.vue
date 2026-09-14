<script setup lang="ts">
import { ref } from 'vue'
import { executeGameServerRawCommand } from '../fetch/gameServers.fetch'

const props = defineProps<{ slug: string }>()

interface RawCommandEntry {
  command: string
  answer: string | null
  error: string | null
}

const command = ref('')
const running = ref(false)
// Plus récent en tête : c'est la dernière réponse qui intéresse, comme le journal serveur.
const history = ref<RawCommandEntry[]>([])

async function run() {
  const value = command.value.trim()
  if (!value || running.value) return

  running.value = true
  try {
    const answer = await executeGameServerRawCommand(props.slug, value)
    history.value.unshift({ command: value, answer, error: null })
    command.value = ''
  } catch (error) {
    const message = (error as { response?: { data?: { message?: string } } })?.response?.data?.message
    history.value.unshift({ command: value, answer: null, error: message ?? 'Échec de la requête.' })
  } finally {
    running.value = false
  }
}
</script>

<template>
  <div class="raw-console">
    <p class="raw-console-warning">
      <i class="mdi mdi-alert-outline" aria-hidden="true" />
      Commande envoyée telle quelle au serveur, sans validation ni confirmation.
    </p>

    <form class="raw-console-input" @submit.prevent="run">
      <input
        v-model="command"
        type="text"
        placeholder="Commande RCON (ex. op joueur)"
        :disabled="running"
        spellcheck="false"
      />
      <button type="submit" class="danger" :disabled="running || !command.trim()">Exécuter</button>
    </form>

    <div v-if="history.length" class="raw-console-history">
      <div v-for="(entry, index) in history" :key="index" class="raw-console-entry">
        <p class="raw-console-command">&gt; {{ entry.command }}</p>
        <pre v-if="entry.error" class="raw-console-answer raw-console-answer--error">{{ entry.error }}</pre>
        <pre v-else-if="entry.answer" class="raw-console-answer">{{ entry.answer }}</pre>
        <p v-else class="raw-console-answer raw-console-answer--empty">(aucune réponse)</p>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.raw-console {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding: 1rem 1.25rem;
}

.raw-console-warning {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  margin: 0;
  font-size: 0.78rem;
  color: var(--pico-muted-color);

  i { color: #e53e3e; font-size: 0.95rem; }
}

.raw-console-input {
  display: flex;
  gap: 0.5rem;

  input {
    flex: 1;
    margin: 0;
    padding: 0.4rem 0.6rem;
    font-size: 0.85rem;
    font-family: var(--pico-font-family-monospace, monospace);
    height: auto;
  }

  button {
    margin: 0;
    padding: 0.4rem 0.8rem;
    font-size: 0.85rem;
    width: auto;
    white-space: nowrap;
  }

  button.danger {
    background: #e53e3e;
    border-color: #e53e3e;
    color: white;

    &:hover:not(:disabled) {
      background: color-mix(in srgb, #e53e3e 85%, black);
    }
  }
}

.raw-console-history {
  max-height: 18rem;
  overflow: auto;
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
}

.raw-console-entry {
  border-left: 2px solid var(--pico-card-border-color);
  padding-left: 0.6rem;
}

.raw-console-command {
  margin: 0 0 0.2rem;
  font-family: var(--pico-font-family-monospace, monospace);
  font-size: 0.78rem;
  font-weight: 600;
}

.raw-console-answer {
  margin: 0;
  font-family: var(--pico-font-family-monospace, monospace);
  font-size: 0.78rem;
  color: var(--pico-muted-color);
  white-space: pre-wrap;
}

.raw-console-answer--error {
  color: #e53e3e;
}

.raw-console-answer--empty {
  font-style: italic;
}
</style>
