<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import type { CodenameHistoryEntry, Team, Role } from '@/modules/Codename/codename.types'

const router = useRouter()

const filterTeam = ref<Team | ''>('')
const filterRole = ref<Exclude<Role, 'SPECTATOR'> | ''>('')
const filterResult = ref<'WIN' | 'LOSS' | ''>('')

const mockHistory: CodenameHistoryEntry[] = [
  { id: '1', sessionId: 'abc-111', team: 'RED', role: 'SPYMASTER', result: 'WIN', createdAt: '2026-05-18T20:30:00Z' },
  { id: '2', sessionId: 'abc-222', team: 'BLUE', role: 'OPERATIVE', result: 'LOSS', createdAt: '2026-05-17T18:00:00Z' },
  { id: '3', sessionId: 'abc-333', team: 'RED', role: 'OPERATIVE', result: 'WIN', createdAt: '2026-05-16T15:45:00Z' },
  { id: '4', sessionId: 'abc-444', team: 'BLUE', role: 'SPYMASTER', result: 'WIN', createdAt: '2026-05-15T21:10:00Z' },
  { id: '5', sessionId: 'abc-555', team: 'RED', role: 'OPERATIVE', result: 'LOSS', createdAt: '2026-05-14T17:20:00Z' },
]

const filtered = computed(() =>
  mockHistory.filter(e =>
    (!filterTeam.value || e.team === filterTeam.value) &&
    (!filterRole.value || e.role === filterRole.value) &&
    (!filterResult.value || e.result === filterResult.value)
  )
)

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('fr-FR', {
    day: 'numeric', month: 'long', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  })
}

function goToReplay(sessionId: string) {
  router.push({ name: 'codename-game', params: { sessionId } })
}
</script>

<template>
  <div class="codename-history">
    <div class="history-header">
      <h2>Mes parties <span class="count">({{ filtered.length }})</span></h2>

      <div class="filters">
        <select v-model="filterTeam">
          <option value="">Toutes les équipes</option>
          <option value="RED">Rouge</option>
          <option value="BLUE">Bleu</option>
        </select>
        <select v-model="filterRole">
          <option value="">Tous les rôles</option>
          <option value="SPYMASTER">Espion</option>
          <option value="OPERATIVE">Opératif</option>
        </select>
        <select v-model="filterResult">
          <option value="">Tous les résultats</option>
          <option value="WIN">Victoire</option>
          <option value="LOSS">Défaite</option>
        </select>
      </div>
    </div>

    <div v-if="filtered.length === 0" class="empty-state">
      Aucune partie trouvée avec ces filtres.
    </div>

    <table v-else class="history-table">
      <thead>
        <tr>
          <th>Date</th>
          <th>Équipe</th>
          <th>Rôle</th>
          <th>Résultat</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="entry in filtered" :key="entry.id">
          <td class="date-cell">{{ formatDate(entry.createdAt) }}</td>
          <td>
            <span class="team-badge" :class="entry.team.toLowerCase()">
              {{ entry.team === 'RED' ? 'Rouge' : 'Bleu' }}
            </span>
          </td>
          <td class="role-cell">
            {{ entry.role === 'SPYMASTER' ? 'Espion' : 'Opératif' }}
          </td>
          <td>
            <span class="result-badge" :class="entry.result.toLowerCase()">
              {{ entry.result === 'WIN' ? 'Victoire' : 'Défaite' }}
            </span>
          </td>
          <td>
            <button class="replay-btn" @click="goToReplay(entry.sessionId)">
              Voir le replay
            </button>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style lang="scss" scoped>
.codename-history {
  max-width: 800px;
  margin: 0 auto;
  padding: 1.5rem 1rem;
}

.history-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 1.25rem;
  flex-wrap: wrap;
  gap: 0.75rem;

  h2 {
    margin: 0;
    font-size: 1.1rem;
  }
}

.count {
  font-size: 0.85rem;
  color: var(--pico-muted-color);
  font-weight: normal;
}

.filters {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;

  select {
    padding: 0.35rem 0.6rem;
    font-size: 0.82rem;
    border: 1px solid var(--pico-muted-border-color);
    border-radius: var(--pico-border-radius);
    background: var(--pico-background-color);
    color: var(--pico-color);
    cursor: pointer;
  }
}

.empty-state {
  text-align: center;
  padding: 3rem;
  color: var(--pico-muted-color);
  font-size: 0.9rem;
}

.history-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.88rem;

  th {
    text-align: left;
    padding: 0.5rem 0.75rem;
    border-bottom: 1px solid var(--pico-muted-border-color);
    color: var(--pico-muted-color);
    font-size: 0.78rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    font-weight: 600;
  }

  td {
    padding: 0.75rem;
    border-bottom: 1px solid var(--pico-muted-border-color);
    vertical-align: middle;
  }

  tr:last-child td {
    border-bottom: none;
  }

  tr:hover td {
    background: var(--pico-card-background-color);
  }
}

.date-cell {
  color: var(--pico-muted-color);
  font-size: 0.82rem;
}

.role-cell {
  color: var(--pico-muted-color);
}

.team-badge {
  display: inline-block;
  font-size: 0.75rem;
  font-weight: 600;
  padding: 0.2rem 0.6rem;
  border-radius: 999px;
  text-transform: uppercase;
  letter-spacing: 0.05em;

  &.red {
    background: rgba(239, 68, 68, 0.15);
    color: #ef4444;
  }

  &.blue {
    background: rgba(59, 130, 246, 0.15);
    color: #3b82f6;
  }
}

.result-badge {
  display: inline-block;
  font-size: 0.75rem;
  font-weight: 600;
  padding: 0.2rem 0.6rem;
  border-radius: 999px;

  &.win {
    background: rgba(34, 197, 94, 0.15);
    color: #22c55e;
  }

  &.loss {
    background: rgba(156, 163, 175, 0.15);
    color: var(--pico-muted-color);
  }
}

.replay-btn {
  font-size: 0.75rem;
  padding: 0.25rem 0.6rem;
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  background: transparent;
  color: var(--pico-muted-color);
  cursor: pointer;
  transition: color 0.2s, border-color 0.2s;

  &:hover {
    color: var(--pico-primary);
    border-color: var(--pico-primary);
  }
}
</style>