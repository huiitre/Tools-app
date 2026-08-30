<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useTemtemTeamsStore } from '@/modules/Temtem/teams/teams.store'
import type { TemtemTeam, TemtemTeamMember } from '@/modules/Temtem/teams/types/teams.types'
import type { TemtemSummary } from '@/modules/Temtem/shared/types/temtem.types'
import toast from '@/services/toast'

const props = defineProps<{
  temtem: TemtemSummary
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const teamsStore = useTemtemTeamsStore()

const newTeamName = ref('')
const creating = ref(false)
const busyTeamId = ref<number | null>(null)

const MAX_MEMBERS = 6

const teams = computed(() => teamsStore.teams)

onMounted(() => teamsStore.ensureLoaded())

const SLOTS = Array.from({ length: MAX_MEMBERS }, (_, index) => index + 1)

// L'API refuserait de toute façon (409 TEAM_FULL) : on le dit avant le clic plutôt qu'après.
function isFull(memberCount: number) {
  return memberCount >= MAX_MEMBERS
}

// Par place et non par rang : un membre retiré au milieu laisse un trou, et c'est ce trou-là que
// le prochain ajout rebouchera. Aligner les images sur les premières places le masquerait.
function memberAt(team: TemtemTeam, slot: number): TemtemTeamMember | undefined {
  return team.members.find(member => member.slot === slot)
}

/**
 * Les six places d'une équipe, occupées ou non — indexées par `slot` et non par rang dans la
 * liste : un membre retiré au milieu laisse un trou que l'API conserve, et le montrer au bon
 * endroit dit à quelle place ira le prochain ajout.
 */
function slots(team: TemtemTeam): (TemtemTeamMember | null)[] {
  return Array.from(
    { length: MAX_MEMBERS },
    (_, index) => team.members.find(member => member.slot === index + 1) ?? null,
  )
}

// Le message de l'API porte la raison exacte (nom déjà pris, équipe pleine…) : on le montre
// plutôt qu'un texte générique qui la masquerait.
function reason(error: any, fallback: string) {
  return error?.response?.data?.message || fallback
}

async function addTo(teamId: number) {
  busyTeamId.value = teamId
  try {
    await teamsStore.addMember(teamId, props.temtem.id)
    toast.success(`${props.temtem.name} ajouté à l'équipe.`)
    emit('close')
  } catch (error) {
    toast.error(reason(error, "Impossible d'ajouter ce Temtem à l'équipe."))
  } finally {
    busyTeamId.value = null
  }
}

// Création et placement d'un seul geste : c'est le `temtemId` du POST côté API, et donc une
// seule transaction — aucune équipe vide ne reste derrière un ajout raté.
async function createAndAdd() {
  const name = newTeamName.value.trim()
  if (!name) {
    toast.warning("Donnez un nom à l'équipe.")
    return
  }

  creating.value = true
  try {
    await teamsStore.create(name, props.temtem.id)
    toast.success(`Équipe « ${name} » créée avec ${props.temtem.name}.`)
    emit('close')
  } catch (error) {
    toast.error(reason(error, "Impossible de créer l'équipe."))
  } finally {
    creating.value = false
  }
}
</script>

<template>
  <div class="overlay" @click.self="emit('close')">
    <div class="modal">
      <h3 class="modal-title">
        <img v-if="temtem.imageUrl" :src="temtem.imageUrl" :alt="temtem.name" class="title-icon">
        Ajouter {{ temtem.name }} à une équipe
      </h3>

      <div class="modal-content">
        <div v-if="teamsStore.loading" class="empty">Chargement de vos équipes…</div>

        <template v-else>
          <button
            v-for="team in teams"
            :key="team.id"
            type="button"
            class="team-item"
            :disabled="isFull(team.members.length) || busyTeamId !== null"
            @click="addTo(team.id)"
          >
            <span class="team-line">
              <span class="team-name">{{ team.name }}</span>
              <span class="team-count" :class="{ full: isFull(team.members.length) }">
                {{ team.members.length }} / {{ MAX_MEMBERS }}
              </span>
            </span>

            <span class="team-slots">
              <span
                v-for="(member, index) in slots(team)"
                :key="index"
                class="slot"
                :class="{ filled: member !== null }"
                :title="member ? member.temtem.name : 'Place libre'"
              >
                <img
                  v-if="member?.temtem.imageUrl"
                  :src="member.temtem.imageUrl"
                  :alt="member.temtem.name"
                  loading="lazy"
                >
              </span>
            </span>
          </button>

          <div v-if="!teams.length" class="empty">
            Vous n'avez pas encore d'équipe.
          </div>
        </template>
      </div>

      <div class="modal-create">
        <input
          v-model="newTeamName"
          type="text"
          maxlength="100"
          placeholder="Nom de la nouvelle équipe"
          :disabled="creating"
          @keyup.enter="createAndAdd"
        >
        <button type="button" :disabled="creating" @click="createAndAdd">
          <i class="mdi mdi-plus" />
          Créer
        </button>
      </div>

      <div class="modal-actions">
        <button type="button" class="secondary" @click="emit('close')">Fermer</button>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.45);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  width: 400px;
  max-width: 90vw;
  max-height: 80vh;
  display: flex;
  flex-direction: column;
  font-size: 0.85rem;
}

.modal-title {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 1rem;
  font-weight: 600;
  margin: 0;
  padding: 0.75rem 1rem;
  border-bottom: 1px solid var(--pico-muted-border-color);
}

.title-icon {
  width: 28px;
  height: 28px;
  border-radius: 4px;
}

.modal-content {
  padding: 0.5rem;
  overflow-y: auto;
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.team-item {
  display: flex;
  flex-direction: column;
  align-items: stretch;
  gap: 0.4rem;
  width: 100%;
  margin: 0;
  padding: 0.5rem;
  border: none;
  border-radius: var(--pico-border-radius);
  background: transparent;
  color: var(--pico-color);
  font-size: 0.85rem;
  text-align: left;
  cursor: pointer;
  transition: background 0.15s;

  &:hover:not(:disabled) {
    background: var(--pico-card-sectioning-background-color);
  }

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}

.team-line {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.team-line {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.team-name {
  flex: 1;
}

.team-slots {
  display: flex;
  gap: 0.3rem;
}

.slot-circle {
  display: grid;
  place-items: center;
  width: 26px;
  height: 26px;
  flex-shrink: 0;

  border-radius: 50%;
  border: 1px solid var(--pico-card-border-color);
  background: var(--pico-card-sectioning-background-color);
  overflow: hidden;

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }
}

/* Une place libre reste un cercle : on voit d'un coup d'œil ce qu'il reste à remplir. */
.slot-circle.empty {
  border-style: dashed;
  background: transparent;
}

/* ── Les six places ──────────────────────────────────────────────── */
.team-slots {
  display: flex;
  gap: 0.3rem;
}

.slot {
  display: grid;
  place-items: center;
  width: 26px;
  height: 26px;
  flex-shrink: 0;

  border-radius: 50%;
  overflow: hidden;

  /* Une place libre reste un cercle, en pointillés : on voit d'un coup d'œil ce qui manque. */
  border: 1px dashed var(--pico-muted-border-color);
  background: transparent;

  &.filled {
    border-style: solid;
    border-color: var(--pico-card-border-color);
    background: var(--pico-card-sectioning-background-color);
  }

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }
}

.team-count {
  font-size: 0.75rem;
  color: var(--pico-muted-color);
  font-variant-numeric: tabular-nums;

  &.full {
    color: var(--pico-del-color, #e53e3e);
  }
}

.empty {
  padding: 1rem;
  text-align: center;
  color: var(--pico-muted-color);
}

.modal-create {
  display: flex;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  border-top: 1px solid var(--pico-muted-border-color);

  input {
    margin: 0;
    height: 2.2rem;
    font-size: 0.85rem;
  }

  button {
    margin: 0;
    width: auto;
    flex-shrink: 0;
    height: 2.2rem;
    padding: 0 0.8rem;
    font-size: 0.85rem;
    display: inline-flex;
    align-items: center;
    gap: 0.3rem;
  }
}

.modal-actions {
  display: flex;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  border-top: 1px solid var(--pico-muted-border-color);

  button {
    flex: 1;
    margin: 0;
    font-size: 0.85rem;
  }
}
</style>
