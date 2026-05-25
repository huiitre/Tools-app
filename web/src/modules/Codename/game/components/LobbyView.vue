<script setup lang="ts">
import type { CodenamePlayer, Team, Role } from '@/modules/Codename/codename.types'

const props = defineProps<{
  players: CodenamePlayer[]
  myPlayerId: string
}>()

const emit = defineEmits<{
  selectTeam: [team: Team]
  selectRole: [role: Exclude<Role, 'SPECTATOR'>]
  toggleReady: []
}>()

const me = () => props.players.find(p => p.id === props.myPlayerId)

const redPlayers = () => props.players.filter(p => p.team === 'RED')
const bluePlayers = () => props.players.filter(p => p.team === 'BLUE')
const spectators = () => props.players.filter(p => !p.team)
</script>

<template>
  <div class="lobby">
    <h2 class="lobby-title">Lobby</h2>
    <p class="lobby-hint">Choisissez votre équipe et votre rôle, puis cliquez sur Prêt quand vous êtes prêts.</p>

    <div class="teams-grid">
      <!-- Équipe Rouge -->
      <div class="team-panel red">
        <div class="team-header">
          <span class="team-name">Équipe Rouge</span>
          <span class="player-count">{{ redPlayers().length }}</span>
        </div>

        <div class="team-players">
          <div
            v-for="player in redPlayers()"
            :key="player.id"
            class="player-row"
            :class="{ me: player.id === myPlayerId }"
          >
            <span class="player-nick">{{ player.nickname }}</span>
            <span class="player-role">
              {{ player.role === 'SPYMASTER' ? 'Espion' : player.role === 'OPERATIVE' ? 'Opératif' : '' }}
            </span>
            <span v-if="player.isReady" class="ready-dot" />
          </div>
        </div>

        <div v-if="me()?.team !== 'RED'" class="join-team-btn-wrapper">
          <button class="join-team-btn red" @click="emit('selectTeam', 'RED')">
            Rejoindre
          </button>
        </div>

        <template v-if="me()?.team === 'RED'">
          <div class="role-picker">
            <button
              class="role-btn"
              :class="{ active: me()?.role === 'SPYMASTER' }"
              @click="emit('selectRole', 'SPYMASTER')"
            >
              Espion
            </button>
            <button
              class="role-btn"
              :class="{ active: me()?.role === 'OPERATIVE' }"
              @click="emit('selectRole', 'OPERATIVE')"
            >
              Opératif
            </button>
          </div>
        </template>
      </div>

      <!-- Équipe Bleue -->
      <div class="team-panel blue">
        <div class="team-header">
          <span class="team-name">Équipe Bleue</span>
          <span class="player-count">{{ bluePlayers().length }}</span>
        </div>

        <div class="team-players">
          <div
            v-for="player in bluePlayers()"
            :key="player.id"
            class="player-row"
            :class="{ me: player.id === myPlayerId }"
          >
            <span class="player-nick">{{ player.nickname }}</span>
            <span class="player-role">
              {{ player.role === 'SPYMASTER' ? 'Espion' : player.role === 'OPERATIVE' ? 'Opératif' : '' }}
            </span>
            <span v-if="player.isReady" class="ready-dot" />
          </div>
        </div>

        <div v-if="me()?.team !== 'BLUE'" class="join-team-btn-wrapper">
          <button class="join-team-btn blue" @click="emit('selectTeam', 'BLUE')">
            Rejoindre
          </button>
        </div>

        <template v-if="me()?.team === 'BLUE'">
          <div class="role-picker">
            <button
              class="role-btn"
              :class="{ active: me()?.role === 'SPYMASTER' }"
              @click="emit('selectRole', 'SPYMASTER')"
            >
              Espion
            </button>
            <button
              class="role-btn"
              :class="{ active: me()?.role === 'OPERATIVE' }"
              @click="emit('selectRole', 'OPERATIVE')"
            >
              Opératif
            </button>
          </div>
        </template>
      </div>
    </div>

    <!-- Spectateurs -->
    <div v-if="spectators().length > 0" class="spectators">
      <span class="spectators-label">Spectateurs :</span>
      <span v-for="s in spectators()" :key="s.id" class="spectator-nick">{{ s.nickname }}</span>
    </div>

    <!-- Bouton Prêt -->
    <div class="ready-section">
      <button
        v-if="me()?.team"
        class="ready-btn"
        :class="{ active: me()?.isReady }"
        @click="emit('toggleReady')"
      >
        {{ me()?.isReady ? 'Annuler' : 'Prêt !' }}
      </button>
      <p v-else class="not-in-team">Rejoignez une équipe pour pouvoir vous marquer prêt.</p>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.lobby {
  max-width: 720px;
  margin: 0 auto;
  padding: 1.5rem 1rem;
}

.lobby-title {
  text-align: center;
  margin-bottom: 0.25rem;
  font-size: 1.4rem;
}

.lobby-hint {
  text-align: center;
  font-size: 0.85rem;
  color: var(--pico-muted-color);
  margin-bottom: 2rem;
}

.teams-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1.25rem;
  margin-bottom: 1.5rem;
}

.team-panel {
  border: 2px solid transparent;
  border-radius: var(--pico-border-radius);
  padding: 1.25rem;
  background: var(--pico-card-background-color);

  &.red { border-color: rgba(239, 68, 68, 0.4); }
  &.blue { border-color: rgba(59, 130, 246, 0.4); }
}

.team-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 1rem;
}

.team-name {
  font-weight: 600;
  font-size: 0.9rem;
  text-transform: uppercase;
  letter-spacing: 0.06em;

  .red & { color: #ef4444; }
  .blue & { color: #3b82f6; }
}

.player-count {
  font-size: 0.78rem;
  color: var(--pico-muted-color);
}

.team-players {
  min-height: 60px;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  margin-bottom: 1rem;
}

.player-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.88rem;

  &.me .player-nick {
    font-weight: 600;
  }
}

.player-nick { flex: 1; }
.player-role { font-size: 0.78rem; color: var(--pico-muted-color); }
.ready-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #22c55e;
}

.join-team-btn-wrapper {
  margin-top: 0.5rem;
}

.join-team-btn {
  width: 100%;
  border: none;
  border-radius: var(--pico-border-radius);
  padding: 0.45rem;
  font-size: 0.82rem;
  font-weight: 600;
  cursor: pointer;
  transition: opacity 0.2s;

  &.red {
    background: rgba(239, 68, 68, 0.15);
    color: #ef4444;
  }

  &.blue {
    background: rgba(59, 130, 246, 0.15);
    color: #3b82f6;
  }

  &:hover { opacity: 0.75; }
}

.role-picker {
  display: flex;
  gap: 0.5rem;
  margin-top: 0.5rem;
}

.role-btn {
  flex: 1;
  padding: 0.4rem;
  font-size: 0.8rem;
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  background: transparent;
  color: var(--pico-muted-color);
  cursor: pointer;
  transition: all 0.2s;

  &.active {
    border-color: var(--pico-primary);
    color: var(--pico-primary);
    background: rgba(var(--pico-primary-rgb, 99, 102, 241), 0.1);
  }
}

.spectators {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
  font-size: 0.82rem;
  color: var(--pico-muted-color);
  margin-bottom: 1.5rem;
}

.spectators-label { font-weight: 500; }
.spectator-nick {
  padding: 0.1rem 0.4rem;
  border-radius: 4px;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-muted-border-color);
}

.ready-section {
  text-align: center;
}

.ready-btn {
  padding: 0.7rem 3rem;
  font-size: 0.95rem;
  font-weight: 600;
  border: 2px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  background: transparent;
  color: var(--pico-muted-color);
  cursor: pointer;
  transition: all 0.2s;

  &.active {
    border-color: #22c55e;
    color: #22c55e;
    background: rgba(34, 197, 94, 0.1);
  }

  &:hover:not(.active) {
    border-color: var(--pico-color);
    color: var(--pico-color);
  }
}

.not-in-team {
  font-size: 0.82rem;
  color: var(--pico-muted-color);
}
</style>