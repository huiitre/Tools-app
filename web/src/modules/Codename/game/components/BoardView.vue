<script setup lang="ts">
import type { CodenameCard, CodenameClue, Team, Role } from '@/modules/Codename/codename.types'

const props = defineProps<{
  board: CodenameCard[]
  currentTurn: Team | null
  scores: Record<Team, number>
  clue: CodenameClue | null
  myRole: Role
  myTeam: Team | null
}>()

const emit = defineEmits<{
  clickCard: [index: number]
  giveClue: [word: string, count: number]
}>()

const clueWord = defineModel<string>('clueWord', { default: '' })
const clueCount = defineModel<number>('clueCount', { default: 1 })

const isMyTurn = () => props.currentTurn === props.myTeam

const canClickCard = () =>
  isMyTurn() && props.myRole === 'OPERATIVE' && props.clue !== null

const canGiveClue = () =>
  isMyTurn() && props.myRole === 'SPYMASTER' && props.clue === null

const cardClass = (card: CodenameCard) => {
  if (!card.revealed && props.myRole !== 'SPYMASTER') return 'hidden'
  if (!card.revealed && props.myRole === 'SPYMASTER') return `spymaster-${card.color.toLowerCase()}`
  return `revealed-${card.color.toLowerCase()}`
}

function submitClue() {
  if (clueWord.value.trim() && clueCount.value >= 1) {
    emit('giveClue', clueWord.value.trim(), clueCount.value)
    clueWord.value = ''
    clueCount.value = 1
  }
}
</script>

<template>
  <div class="board-wrapper">
    <!-- Score header -->
    <div class="score-bar">
      <div class="score red" :class="{ active: currentTurn === 'RED' }">
        <span class="score-label">Rouge</span>
        <span class="score-value">{{ scores.RED }}</span>
      </div>

      <div class="turn-indicator">
        <span v-if="currentTurn" class="turn-team" :class="currentTurn.toLowerCase()">
          Tour {{ currentTurn === 'RED' ? 'Rouge' : 'Bleu' }}
        </span>
      </div>

      <div class="score blue" :class="{ active: currentTurn === 'BLUE' }">
        <span class="score-value">{{ scores.BLUE }}</span>
        <span class="score-label">Bleu</span>
      </div>
    </div>

    <!-- Indice en cours -->
    <div v-if="clue" class="clue-display">
      Indice : <strong>{{ clue.word }}</strong> — <strong>{{ clue.count }}</strong> carte(s)
    </div>

    <!-- Saisie d'indice (SPYMASTER) -->
    <div v-if="canGiveClue()" class="clue-input-row">
      <input
        v-model="clueWord"
        placeholder="Votre indice…"
        @keydown.enter="submitClue"
      />
      <select v-model="clueCount">
        <option v-for="n in [1,2,3,4,5,6,7,8,9]" :key="n" :value="n">{{ n }}</option>
      </select>
      <button @click="submitClue" :disabled="!clueWord.trim()">Donner l'indice</button>
    </div>

    <!-- Grille 5x5 -->
    <div class="board">
      <div
        v-for="(card, index) in board"
        :key="index"
        class="card"
        :class="[cardClass(card), { clickable: canClickCard() && !card.revealed }]"
        @click="canClickCard() && !card.revealed && emit('clickCard', index)"
      >
        <span class="card-word">{{ card.word }}</span>
      </div>
    </div>

    <p v-if="!isMyTurn()" class="waiting-msg">
      En attente de l'équipe {{ currentTurn === 'RED' ? 'Rouge' : 'Bleue' }}…
    </p>
  </div>
</template>

<style lang="scss" scoped>
.board-wrapper {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 1rem;
  gap: 1rem;
}

.score-bar {
  display: flex;
  align-items: center;
  gap: 2rem;
  width: 100%;
  max-width: 650px;
}

.score {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  font-size: 0.9rem;
  opacity: 0.5;
  transition: opacity 0.3s;

  &.active { opacity: 1; }

  &.red .score-label, &.red .score-value { color: #ef4444; }
  &.blue .score-label, &.blue .score-value { color: #3b82f6; }
}

.score-value {
  font-size: 1.6rem;
  font-weight: 700;
}

.score-label {
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.06em;
}

.turn-indicator {
  flex: 1;
  text-align: center;
}

.turn-team {
  font-size: 0.85rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  padding: 0.3rem 0.9rem;
  border-radius: 999px;

  &.red { background: rgba(239,68,68,0.15); color: #ef4444; }
  &.blue { background: rgba(59,130,246,0.15); color: #3b82f6; }
}

.clue-display {
  font-size: 0.95rem;
  padding: 0.5rem 1.25rem;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  color: var(--pico-color);
}

.clue-input-row {
  display: flex;
  gap: 0.5rem;
  align-items: center;

  input {
    padding: 0.45rem 0.75rem;
    font-size: 0.88rem;
    border: 1px solid var(--pico-muted-border-color);
    border-radius: var(--pico-border-radius);
    background: var(--pico-background-color);
    color: var(--pico-color);
    width: 180px;
  }

  select {
    padding: 0.45rem 0.5rem;
    font-size: 0.88rem;
    border: 1px solid var(--pico-muted-border-color);
    border-radius: var(--pico-border-radius);
    background: var(--pico-background-color);
    color: var(--pico-color);
  }

  button {
    padding: 0.45rem 1rem;
    font-size: 0.85rem;
    background: var(--pico-primary);
    color: var(--pico-primary-inverse);
    border: none;
    border-radius: var(--pico-border-radius);
    cursor: pointer;
    transition: opacity 0.2s;

    &:disabled { opacity: 0.4; cursor: not-allowed; }
    &:not(:disabled):hover { opacity: 0.85; }
  }
}

.board {
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: 0.5rem;
  width: 100%;
  max-width: 650px;
}

.card {
  aspect-ratio: 5/3;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: var(--pico-border-radius);
  border: 1px solid transparent;
  cursor: default;
  user-select: none;
  transition: transform 0.15s, box-shadow 0.15s;

  &.clickable {
    cursor: pointer;

    &:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(0,0,0,0.15);
    }
  }

  &.hidden {
    background: var(--pico-card-background-color);
    border-color: var(--pico-muted-border-color);
    color: var(--pico-color);
  }

  &.spymaster-red {
    background: rgba(239, 68, 68, 0.2);
    border-color: rgba(239, 68, 68, 0.4);
    color: #ef4444;
  }

  &.spymaster-blue {
    background: rgba(59, 130, 246, 0.2);
    border-color: rgba(59, 130, 246, 0.4);
    color: #3b82f6;
  }

  &.spymaster-neutral {
    background: rgba(156, 163, 175, 0.2);
    border-color: rgba(156, 163, 175, 0.3);
    color: var(--pico-muted-color);
  }

  &.spymaster-assassin {
    background: rgba(30, 30, 30, 0.6);
    border-color: rgba(0,0,0,0.5);
    color: #fff;
  }

  &.revealed-red {
    background: #ef4444;
    color: #fff;
    border-color: #dc2626;
  }

  &.revealed-blue {
    background: #3b82f6;
    color: #fff;
    border-color: #2563eb;
  }

  &.revealed-neutral {
    background: #9ca3af;
    color: #fff;
    border-color: #6b7280;
  }

  &.revealed-assassin {
    background: #1a1a1a;
    color: #fff;
    border-color: #000;
  }
}

.card-word {
  font-size: 0.72rem;
  font-weight: 600;
  text-align: center;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  padding: 0.25rem;
}

.waiting-msg {
  font-size: 0.85rem;
  color: var(--pico-muted-color);
  text-align: center;
}
</style>
