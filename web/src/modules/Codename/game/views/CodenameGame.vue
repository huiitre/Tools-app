<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRoute } from 'vue-router'
import type { CodenameSession, CodenamePlayer, CodenameCard, Team, Role } from '@/modules/Codename/codename.types'
import JoinModal from '@/modules/Codename/game/components/JoinModal.vue'
import LobbyView from '@/modules/Codename/game/components/LobbyView.vue'
import BoardView from '@/modules/Codename/game/components/BoardView.vue'
import ChatPanel from '@/modules/Codename/game/components/ChatPanel.vue'

const route = useRoute()
const sessionId = route.params.sessionId as string

// ── Player session (localStorage) ────────────────────────────────────────────

const myPlayerId = (() => {
  let id = localStorage.getItem('codename_player_id')
  if (!id) {
    id = crypto.randomUUID()
    localStorage.setItem('codename_player_id', id)
  }
  return id
})()

const joined = ref(false)

// ── Mock session state ────────────────────────────────────────────────────────

function makeBoard(): CodenameCard[] {
  const words = [
    'Chat', 'Souris', 'Ordinateur', 'Soleil', 'Lune',
    'Dragon', 'Épée', 'Château', 'Forêt', 'Rivière',
    'Étoile', 'Fusée', 'Robot', 'Fantôme', 'Trésor',
    'Carte', 'Clé', 'Tour', 'Pont', 'Flamme',
    'Glace', 'Sable', 'Nuage', 'Vent', 'Pierre',
  ]
  const colors: Array<'RED' | 'BLUE' | 'NEUTRAL' | 'ASSASSIN'> = [
    'RED','RED','RED','RED','RED','RED','RED','RED','RED',
    'BLUE','BLUE','BLUE','BLUE','BLUE','BLUE','BLUE','BLUE',
    'NEUTRAL','NEUTRAL','NEUTRAL','NEUTRAL','NEUTRAL','NEUTRAL','NEUTRAL',
    'ASSASSIN',
  ]
  return words.map((word, i) => ({ word, color: colors[i], revealed: false }))
}

const session = ref<CodenameSession>({
  id: sessionId,
  status: 'LOBBY',
  board: makeBoard(),
  currentTurn: null,
  scores: { RED: 9, BLUE: 8 },
  startingTeam: null,
  clue: null,
  players: [],
})

// ── Current player ────────────────────────────────────────────────────────────

const me = computed<CodenamePlayer | undefined>(() =>
  session.value.players.find(p => p.id === myPlayerId)
)

// ── Join ──────────────────────────────────────────────────────────────────────

function handleJoin(nickname: string) {
  const player: CodenamePlayer = {
    id: myPlayerId,
    nickname,
    team: null,
    role: 'SPECTATOR',
    isReady: false,
  }
  session.value.players.push(player)
  joined.value = true
}

// ── Lobby actions ─────────────────────────────────────────────────────────────

function selectTeam(team: Team) {
  const player = session.value.players.find(p => p.id === myPlayerId)
  if (player) {
    player.team = team
    player.role = 'OPERATIVE'
    player.isReady = false
    addEvent('TEAM_CHANGE', `${player.nickname} a rejoint l'équipe ${team === 'RED' ? 'Rouge' : 'Bleue'}.`)
  }
}

function selectRole(role: Exclude<Role, 'SPECTATOR'>) {
  const player = session.value.players.find(p => p.id === myPlayerId)
  if (player) {
    player.role = role
    player.isReady = false
    addEvent('ROLE_CHANGE', `${player.nickname} est maintenant ${role === 'SPYMASTER' ? 'Espion' : 'Opératif'}.`)
  }
}

function toggleReady() {
  const player = session.value.players.find(p => p.id === myPlayerId)
  if (player) {
    player.isReady = !player.isReady
    addEvent('PLAYER_READY', `${player.nickname} est ${player.isReady ? 'prêt' : 'pas encore prêt'}.`)
    checkStart()
  }
}

function checkStart() {
  const teamPlayers = session.value.players.filter(p => p.team)
  if (teamPlayers.length === 0) return

  const allReady = teamPlayers.every(p => p.isReady)
  if (!allReady) return

  const teams = [...new Set(teamPlayers.map(p => p.team))]
  const allTeamsHaveSpymaster = teams.every(team =>
    teamPlayers.some(p => p.team === team && p.role === 'SPYMASTER')
  )

  if (allTeamsHaveSpymaster) startGame()
}

function startGame() {
  session.value.status = 'IN_PROGRESS'
  session.value.currentTurn = 'RED'
  session.value.startingTeam = 'RED'
  addEvent('GAME_START', 'La partie commence ! L\'équipe Rouge joue en premier.')
}

// ── Game actions ──────────────────────────────────────────────────────────────

function clickCard(index: number) {
  const card = session.value.board[index]
  if (card.revealed) return

  card.revealed = true
  addEvent('CARD_CLICK', `Carte « ${card.word} » révélée.`)

  if (card.color === 'ASSASSIN') {
    const winner = session.value.currentTurn === 'RED' ? 'BLUE' : 'RED'
    endGame(winner)
    return
  }

  if (card.color !== session.value.currentTurn) {
    switchTurn()
  }

  session.value.clue = null
  checkWin()
}

function switchTurn() {
  session.value.currentTurn = session.value.currentTurn === 'RED' ? 'BLUE' : 'RED'
  session.value.clue = null
}

function checkWin() {
  const board = session.value.board
  const redLeft = board.filter(c => c.color === 'RED' && !c.revealed).length
  const blueLeft = board.filter(c => c.color === 'BLUE' && !c.revealed).length

  if (redLeft === 0) endGame('RED')
  else if (blueLeft === 0) endGame('BLUE')
}

function endGame(winner: Team) {
  session.value.status = 'FINISHED'
  session.value.winner = winner
  addEvent('GAME_END', `Partie terminée ! L'équipe ${winner === 'RED' ? 'Rouge' : 'Bleue'} a gagné !`)
}

function giveClue(word: string, count: number) {
  session.value.clue = { word, count }
  addEvent('CLUE_GIVEN', `Indice : « ${word} » — ${count} carte(s).`)
}

// ── Chat & events ─────────────────────────────────────────────────────────────

interface ChatEvent {
  id: string
  type: 'CHAT_MSG' | 'CARD_CLICK' | 'CLUE_GIVEN' | 'PLAYER_JOIN' | 'GAME_START' | 'GAME_END' | 'TEAM_CHANGE' | 'ROLE_CHANGE' | 'PLAYER_READY'
  nickname?: string
  content: string
  timestamp: string
}

const events = ref<ChatEvent[]>([])

function addEvent(type: ChatEvent['type'], content: string, nickname?: string) {
  events.value.push({
    id: crypto.randomUUID(),
    type,
    content,
    nickname,
    timestamp: new Date().toISOString(),
  })
}

function sendChat(msg: string) {
  addEvent('CHAT_MSG', msg, me.value?.nickname)
}

// ── Clue model ────────────────────────────────────────────────────────────────

const clueWord = ref('')
const clueCount = ref(1)
</script>

<template>
  <div class="codename-game">
    <!-- Modal de connexion -->
    <JoinModal v-if="!joined" @join="handleJoin" />

    <!-- Partie terminée -->
    <div v-else-if="session.status === 'FINISHED'" class="finished-screen">
      <div class="winner-box" :class="session.winner?.toLowerCase()">
        <h2>Victoire de l'équipe {{ session.winner === 'RED' ? 'Rouge' : 'Bleue' }} !</h2>
        <p>Félicitations à toute l'équipe.</p>
        <button class="restart-btn" @click="session.status = 'LOBBY'">Rejouer</button>
      </div>
    </div>

    <!-- Lobby -->
    <div v-else-if="session.status === 'LOBBY'" class="lobby-layout">
      <LobbyView
        :players="session.players"
        :my-player-id="myPlayerId"
        @select-team="selectTeam"
        @select-role="selectRole"
        @toggle-ready="toggleReady"
      />
    </div>

    <!-- En jeu -->
    <div v-else class="game-layout">
      <div class="board-area">
        <BoardView
          :board="session.board"
          :current-turn="session.currentTurn"
          :scores="session.scores"
          :clue="session.clue"
          :my-role="me?.role ?? 'SPECTATOR'"
          :my-team="me?.team ?? null"
          v-model:clue-word="clueWord"
          v-model:clue-count="clueCount"
          @click-card="clickCard"
          @give-clue="giveClue"
        />
      </div>

      <div class="chat-area">
        <ChatPanel
          :events="events"
          :can-chat="joined"
          @send-chat="sendChat"
        />
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.codename-game {
  height: calc(100vh - var(--header-height, 56px));
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.lobby-layout {
  flex: 1;
  overflow-y: auto;
}

.game-layout {
  flex: 1;
  display: grid;
  grid-template-columns: 1fr 280px;
  gap: 0;
  overflow: hidden;
}

.board-area {
  overflow-y: auto;
  padding: 1rem;
}

.chat-area {
  border-left: 1px solid var(--pico-muted-border-color);
  height: 100%;
  overflow: hidden;
}

.finished-screen {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
}

.winner-box {
  text-align: center;
  padding: 3rem 4rem;
  border-radius: var(--pico-border-radius);
  border: 2px solid transparent;

  h2 { margin-bottom: 0.5rem; }
  p { color: var(--pico-muted-color); margin-bottom: 2rem; }

  &.red {
    background: rgba(239, 68, 68, 0.08);
    border-color: rgba(239, 68, 68, 0.4);
    h2 { color: #ef4444; }
  }

  &.blue {
    background: rgba(59, 130, 246, 0.08);
    border-color: rgba(59, 130, 246, 0.4);
    h2 { color: #3b82f6; }
  }
}

.restart-btn {
  padding: 0.65rem 2.5rem;
  font-size: 0.9rem;
  font-weight: 600;
  border: none;
  border-radius: var(--pico-border-radius);
  background: var(--pico-primary);
  color: var(--pico-primary-inverse);
  cursor: pointer;
  transition: opacity 0.2s;

  &:hover { opacity: 0.85; }
}
</style>