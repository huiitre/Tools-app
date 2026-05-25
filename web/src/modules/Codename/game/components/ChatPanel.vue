<script setup lang="ts">
import { ref, nextTick, watch } from 'vue'

interface ChatEvent {
  id: string
  type: 'CHAT_MSG' | 'CARD_CLICK' | 'CLUE_GIVEN' | 'PLAYER_JOIN' | 'GAME_START' | 'GAME_END' | 'TEAM_CHANGE' | 'ROLE_CHANGE' | 'PLAYER_READY'
  nickname?: string
  content: string
  timestamp: string
}

const props = defineProps<{
  events: ChatEvent[]
  canChat: boolean
}>()

const emit = defineEmits<{
  sendChat: [message: string]
}>()

const message = ref('')
const scrollEl = ref<HTMLElement>()

watch(() => props.events.length, async () => {
  await nextTick()
  if (scrollEl.value) {
    scrollEl.value.scrollTop = scrollEl.value.scrollHeight
  }
})

function send() {
  if (message.value.trim()) {
    emit('sendChat', message.value.trim())
    message.value = ''
  }
}

function eventClass(type: ChatEvent['type']) {
  if (type === 'CHAT_MSG') return 'chat'
  if (type === 'CARD_CLICK') return 'event card-click'
  if (type === 'CLUE_GIVEN') return 'event clue'
  if (type === 'GAME_START' || type === 'GAME_END') return 'event system'
  return 'event info'
}

function formatTime(iso: string) {
  return new Date(iso).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })
}
</script>

<template>
  <div class="chat-panel">
    <div class="chat-header">Chat & Événements</div>

    <div class="chat-messages" ref="scrollEl">
      <div
        v-for="evt in events"
        :key="evt.id"
        :class="['message', eventClass(evt.type)]"
      >
        <span v-if="evt.type === 'CHAT_MSG'" class="msg-nick">{{ evt.nickname }}</span>
        <span class="msg-content">{{ evt.content }}</span>
        <span class="msg-time">{{ formatTime(evt.timestamp) }}</span>
      </div>

      <div v-if="events.length === 0" class="empty-chat">
        Aucun message pour l'instant.
      </div>
    </div>

    <div v-if="canChat" class="chat-input">
      <input
        v-model="message"
        placeholder="Message…"
        maxlength="200"
        @keydown.enter="send"
      />
      <button @click="send" :disabled="!message.trim()">→</button>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.chat-panel {
  display: flex;
  flex-direction: column;
  height: 100%;
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  background: var(--pico-card-background-color);
  overflow: hidden;
}

.chat-header {
  padding: 0.6rem 0.75rem;
  font-size: 0.78rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--pico-muted-color);
  border-bottom: 1px solid var(--pico-muted-border-color);
}

.chat-messages {
  flex: 1;
  overflow-y: auto;
  padding: 0.5rem;
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
}

.message {
  display: flex;
  align-items: baseline;
  gap: 0.35rem;
  font-size: 0.82rem;
  line-height: 1.4;

  &.chat {
    color: var(--pico-color);
  }

  &.event {
    color: var(--pico-muted-color);
    font-style: italic;
    font-size: 0.78rem;
  }

  &.event.system {
    color: var(--pico-primary);
    font-weight: 500;
    font-style: normal;
  }

  &.event.clue {
    color: #f59e0b;
  }

  &.event.card-click {
    color: #22c55e;
  }
}

.msg-nick {
  font-weight: 600;
  flex-shrink: 0;
  color: var(--pico-primary);

  &::after { content: ' :'; }
}

.msg-content {
  flex: 1;
  word-break: break-word;
}

.msg-time {
  font-size: 0.7rem;
  color: var(--pico-muted-color);
  flex-shrink: 0;
  margin-left: auto;
}

.empty-chat {
  text-align: center;
  padding: 2rem;
  font-size: 0.8rem;
  color: var(--pico-muted-color);
}

.chat-input {
  display: flex;
  border-top: 1px solid var(--pico-muted-border-color);

  input {
    flex: 1;
    padding: 0.5rem 0.6rem;
    font-size: 0.85rem;
    border: none;
    background: transparent;
    color: var(--pico-color);
    outline: none;
  }

  button {
    padding: 0.5rem 0.75rem;
    background: none;
    border: none;
    border-left: 1px solid var(--pico-muted-border-color);
    cursor: pointer;
    color: var(--pico-primary);
    font-size: 1rem;
    transition: opacity 0.2s;

    &:disabled { opacity: 0.3; cursor: not-allowed; }
    &:not(:disabled):hover { opacity: 0.75; }
  }
}
</style>