<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useTemtemTeamsStore } from '../teams.store'
import TeamCard from '../components/TeamCard.vue'
import toast from '@/services/toast'

const teamsStore = useTemtemTeamsStore()

const newTeamName = ref('')
const creating = ref(false)

const teams = computed(() => teamsStore.teams)

onMounted(() => teamsStore.ensureLoaded())

async function createTeam() {
  const name = newTeamName.value.trim()
  if (!name) {
    toast.warning("Donnez un nom à l'équipe.")
    return
  }

  creating.value = true
  try {
    // Sans Temtem : la page « Mes équipes » crée une équipe vide, contrairement à la popup du
    // Temtemdex qui crée et place en un geste.
    await teamsStore.create(name)
    newTeamName.value = ''
  } catch (error: any) {
    toast.error(error?.response?.data?.message || "Impossible de créer l'équipe.")
  } finally {
    creating.value = false
  }
}
</script>

<template>
  <div class="teams">
    <div class="teams-header">
      <div class="create-team">
        <input
          v-model="newTeamName"
          type="text"
          maxlength="100"
          placeholder="Nom de la nouvelle équipe"
          class="name-input"
          :disabled="creating"
          @keyup.enter="createTeam"
        >
        <button type="button" class="create-btn" :disabled="creating" @click="createTeam">
          <i class="mdi mdi-plus" />
          Créer une équipe
        </button>
      </div>

      <span class="teams-count">
        <span class="count-sep" />
        <strong>{{ teams.length }}</strong> équipe{{ teams.length > 1 ? 's' : '' }}
      </span>
    </div>

    <div v-if="teamsStore.error" class="error-banner">
      <i class="mdi mdi-alert-circle-outline" />
      {{ teamsStore.error }}
    </div>

    <div v-else-if="teamsStore.loading" class="status">
      <span class="spinner" />
      Chargement de vos équipes…
    </div>

    <template v-else>
      <div class="team-list">
        <TeamCard v-for="team in teams" :key="team.id" :team="team" />
      </div>

      <p v-if="!teams.length" class="empty">
        Vous n'avez pas encore d'équipe. Créez-en une ci-dessus, ou depuis une carte du Temtemdex.
      </p>
    </template>
  </div>
</template>

<style lang="scss" scoped>
.teams {
  padding: 2rem;
  max-width: 1300px;
  margin: 0 auto;
  width: 100%;
  box-sizing: border-box;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

@media (max-width: 640px) {
  .teams { padding: 1rem; }
}

/* ── Header ──────────────────────────────────────────────────────── */
.teams-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.create-team {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.name-input {
  margin: 0;
  height: 2rem;
  font-size: 0.75rem;
  width: 240px;
}

.create-btn {
  margin: 0;
  width: auto;
  height: 2rem;
  padding: 0 0.8rem;
  font-size: 0.75rem;
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  white-space: nowrap;
  background: transparent;
  border-color: var(--pico-card-border-color);
  color: var(--pico-primary);

  &:hover:not(:disabled) {
    background: transparent;
    border-color: var(--pico-primary);
    color: var(--pico-primary);
  }
}

.teams-count {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.75rem;
  color: var(--pico-muted-color);
  white-space: nowrap;
  margin-left: auto;

  strong { color: var(--pico-primary); font-weight: 700; }
}

.count-sep {
  display: inline-block;
  width: 1px;
  height: 1rem;
  background: var(--pico-muted-border-color);
  margin-right: 0.15rem;
}

/* ── Liste ───────────────────────────────────────────────────────── */
.team-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

/* ── Status / error ──────────────────────────────────────────────── */
.status {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  padding: 3rem 0;
  color: var(--pico-muted-color);
  font-size: 0.9rem;
}

.spinner {
  width: 20px;
  height: 20px;
  border: 2px solid var(--pico-card-border-color);
  border-top-color: var(--pico-primary);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.error-banner {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  border-radius: 8px;
  background: color-mix(in srgb, #e53e3e 10%, transparent);
  border: 1px solid color-mix(in srgb, #e53e3e 25%, transparent);
  color: #e53e3e;
  font-size: 0.875rem;
}

.empty {
  text-align: center;
  color: var(--pico-muted-color);
  padding: 2rem 0;
}
</style>
