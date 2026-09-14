<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { executeGameServerRawCommand, fetchGameServerRawCommandHistory } from '../fetch/gameServers.fetch'
import { formatRelativeTime } from '@/utils/formatRelativeTime'

const props = defineProps<{ slug: string }>()

// Un échec de connexion n'a jamais atteint le jeu : rien n'est audité côté API, cette entrée ne
// vit que dans cet onglet le temps de la session (voir GetGameServerDashboardUseCase.ExecuteRawCommand).
interface TransientError {
  command: string
  error: string
}

const command = ref('')
const running = ref(false)
const loadingHistory = ref(true)
const transientErrors = ref<TransientError[]>([])
// Déjà triée par l'API, la plus récente en tête.
const history = ref<Awaited<ReturnType<typeof fetchGameServerRawCommandHistory>>>([])

onMounted(loadHistory)

async function loadHistory() {
  loadingHistory.value = true
  try {
    history.value = await fetchGameServerRawCommandHistory(props.slug)
  } finally {
    loadingHistory.value = false
  }
}

async function run() {
  const value = command.value.trim()
  if (!value || running.value) return

  running.value = true
  try {
    await executeGameServerRawCommand(props.slug, value)
    command.value = ''
    await loadHistory()
  } catch (error) {
    const message = (error as { response?: { data?: { message?: string } } })?.response?.data?.message
    transientErrors.value.unshift({ command: value, error: message ?? 'Échec de la requête.' })
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

    <div v-if="transientErrors.length || history.length" class="raw-console-history">
      <div v-for="(entry, index) in transientErrors" :key="`transient-${index}`" class="raw-console-entry">
        <p class="raw-console-command">&gt; {{ entry.command }}</p>
        <pre class="raw-console-answer raw-console-answer--error">{{ entry.error }}</pre>
      </div>

      <div v-for="(entry, index) in history" :key="`history-${index}`" class="raw-console-entry">
        <p class="raw-console-command">
          &gt; {{ entry.command }}
          <span class="raw-console-meta">{{ entry.userName }} · {{ formatRelativeTime(entry.executedAt) }}</span>
        </p>
        <pre v-if="entry.answer" class="raw-console-answer">{{ entry.answer }}</pre>
        <p v-else class="raw-console-answer raw-console-answer--empty">(aucune réponse)</p>
      </div>
    </div>
    <p v-else-if="!loadingHistory" class="raw-console-empty">Aucune commande exécutée pour l'instant.</p>
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

.raw-console-meta {
  margin-left: 0.5rem;
  font-family: var(--pico-font-family, sans-serif);
  font-size: 0.72rem;
  font-weight: 400;
  color: var(--pico-muted-color);
}

.raw-console-empty {
  margin: 0;
  font-size: 0.78rem;
  color: var(--pico-muted-color);
  font-style: italic;
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
