<script setup lang="ts">
import { computed, nextTick, ref } from 'vue'
import { useTemtemTeamsStore } from '@/modules/Temtem/teams/teams.store'
import { typesOf } from '@/modules/Temtem/shared/temtem.helpers'
import TemtemContextTrigger from '@/modules/Temtem/shared/components/TemtemContextTrigger.vue'
import TechniquePickerModal from './TechniquePickerModal.vue'
import AddMemberModal from './AddMemberModal.vue'
import type { TemtemTeam, TemtemTeamMember } from '@/modules/Temtem/teams/types/teams.types'
import type { TemtemSummary } from '@/modules/Temtem/shared/types/temtem.types'
import toast from '@/services/toast'

const props = defineProps<{
  team: TemtemTeam
}>()

const MAX_MEMBERS = 6
const MAX_TECHNIQUES = 4
const SLOTS = Array.from({ length: MAX_MEMBERS }, (_, index) => index + 1)

const teamsStore = useTemtemTeamsStore()

const renaming = ref(false)
const draftName = ref('')
const nameInput = ref<HTMLInputElement | null>(null)
const confirmingDelete = ref(false)
const busy = ref(false)
const memberBeingEdited = ref<TemtemTeamMember | null>(null)
const addingMember = ref(false)

// Une modale ouverte se superpose à l'infobulle : on la coupe pendant ce temps.
const modalOpen = computed(() => memberBeingEdited.value !== null || addingMember.value)

const isFull = computed(() => props.team.members.length >= MAX_MEMBERS)

// Par place et non par rang : un membre retiré au milieu laisse un trou, et c'est celui-là que
// le prochain ajout rebouchera.
function memberAt(slot: number): TemtemTeamMember | undefined {
  return props.team.members.find(member => member.slot === slot)
}

// Le message de l'API porte la raison exacte : on le montre plutôt qu'un texte générique.
function reason(error: any, fallback: string) {
  return error?.response?.data?.message || fallback
}

async function startRename() {
  draftName.value = props.team.name
  renaming.value = true
  await nextTick()
  nameInput.value?.select()
}

async function confirmRename() {
  const name = draftName.value.trim()
  if (!name || name === props.team.name) {
    renaming.value = false
    return
  }

  busy.value = true
  try {
    await teamsStore.rename(props.team.id, name)
    renaming.value = false
  } catch (error) {
    toast.error(reason(error, "Impossible de renommer l'équipe."))
  } finally {
    busy.value = false
  }
}

async function remove() {
  busy.value = true
  try {
    await teamsStore.remove(props.team.id)
    toast.success('Équipe supprimée.')
  } catch (error) {
    toast.error(reason(error, "Impossible de supprimer l'équipe."))
  } finally {
    busy.value = false
    confirmingDelete.value = false
  }
}

async function removeMember(member: TemtemTeamMember) {
  busy.value = true
  try {
    await teamsStore.removeMember(props.team.id, member.id)
  } catch (error) {
    toast.error(reason(error, 'Impossible de retirer ce membre.'))
  } finally {
    busy.value = false
  }
}

async function addMember(temtem: TemtemSummary) {
  try {
    await teamsStore.addMember(props.team.id, temtem.id)
    addingMember.value = false
  } catch (error) {
    toast.error(reason(error, "Impossible d'ajouter ce Temtem."))
    addingMember.value = false
  }
}

async function saveTechniques(techniqueIds: number[]) {
  const member = memberBeingEdited.value
  if (!member) return

  try {
    await teamsStore.setTechniques(props.team.id, member.id, techniqueIds)
    memberBeingEdited.value = null
  } catch (error) {
    toast.error(reason(error, "Impossible d'enregistrer les techniques."))
  }
}
</script>

<template>
  <article class="team-card">
    <header class="team-header">
      <template v-if="renaming">
        <input
          ref="nameInput"
          v-model="draftName"
          type="text"
          maxlength="100"
          class="name-input"
          :disabled="busy"
          @keyup.enter="confirmRename"
          @keyup.escape="renaming = false"
          @blur="confirmRename"
        >
      </template>
      <template v-else>
        <!-- Nom cliquable plutôt qu'un bouton « renommer » à côté. -->
        <h3 class="team-name" title="Renommer" @click="startRename">{{ team.name }}</h3>
      </template>

      <span class="team-count" :class="{ full: isFull }">
        {{ team.members.length }} / {{ MAX_MEMBERS }}
      </span>

      <button
        v-if="!confirmingDelete"
        type="button"
        class="icon-btn danger"
        title="Supprimer l'équipe"
        :disabled="busy"
        @click="confirmingDelete = true"
      >
        <i class="mdi mdi-trash-can-outline" />
      </button>

      <span v-else class="confirm-delete">
        Supprimer ?
        <button type="button" class="icon-btn danger" title="Confirmer" :disabled="busy" @click="remove">
          <i class="mdi mdi-check" />
        </button>
        <button type="button" class="icon-btn" title="Annuler" :disabled="busy" @click="confirmingDelete = false">
          <i class="mdi mdi-close" />
        </button>
      </span>
    </header>

    <div class="slots">
      <template v-for="slot in SLOTS" :key="slot">
        <div v-if="memberAt(slot)" class="slot">
          <TemtemContextTrigger :temtem="memberAt(slot)!.temtem" :disabled="modalOpen">
            <div class="member-head">
              <img
                v-if="memberAt(slot)!.temtem.imageUrl"
                :src="memberAt(slot)!.temtem.imageUrl!"
                :alt="memberAt(slot)!.temtem.name"
                loading="lazy"
              >
              <span class="member-name">{{ memberAt(slot)!.temtem.name }}</span>
              <span class="member-types">
                <img
                  v-for="type in typesOf(memberAt(slot)!.temtem)"
                  :key="type.id"
                  :src="type.imageUrl ?? ''"
                  :alt="type.name"
                  :title="type.name"
                  class="type-icon"
                  loading="lazy"
                >
              </span>
            </div>
          </TemtemContextTrigger>

          <button
            type="button"
            class="techniques"
            title="Choisir les techniques"
            :disabled="busy"
            @click="memberBeingEdited = memberAt(slot)!"
          >
            <span
              v-for="technique in memberAt(slot)!.techniques"
              :key="technique.id"
              class="technique-chip"
              :title="technique.effect ?? technique.name"
            >
              <img
                v-if="technique.type.imageUrl"
                :src="technique.type.imageUrl"
                :alt="technique.type.name"
                class="type-icon"
              >
              {{ technique.name }}
            </span>

            <span
              v-for="empty in MAX_TECHNIQUES - memberAt(slot)!.techniques.length"
              :key="`empty-${empty}`"
              class="technique-chip empty"
            >
              Technique libre
            </span>
          </button>

          <button
            type="button"
            class="icon-btn remove-member"
            title="Retirer de l'équipe"
            :disabled="busy"
            @click="removeMember(memberAt(slot)!)"
          >
            <i class="mdi mdi-close" />
          </button>
        </div>

        <button
          v-else
          type="button"
          class="slot empty"
          title="Ajouter un Temtem"
          :disabled="busy"
          @click="addingMember = true"
        >
          <i class="mdi mdi-plus" />
          <span>Place {{ slot }}</span>
        </button>
      </template>
    </div>

    <TechniquePickerModal
      v-if="memberBeingEdited"
      :member="memberBeingEdited"
      @close="memberBeingEdited = null"
      @save="saveTechniques"
    />

    <AddMemberModal
      v-if="addingMember"
      :team-name="team.name"
      @close="addingMember = false"
      @pick="addMember"
    />
  </article>
</template>

<style scoped lang="scss">
.team-card {
  margin: 0;
  padding: 1rem;
  border-radius: 10px;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
}

/* ── Header ──────────────────────────────────────────────────────── */
.team-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.team-name {
  margin: 0;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  flex: 1;
  min-width: 0;

  &:hover { color: var(--pico-primary); }
}

.name-input {
  margin: 0;
  flex: 1;
  height: 2rem;
  font-size: 0.9rem;
}

.team-count {
  font-size: 0.75rem;
  color: var(--pico-muted-color);
  font-variant-numeric: tabular-nums;

  &.full { color: var(--pico-primary); }
}

.confirm-delete {
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  font-size: 0.78rem;
  color: var(--pico-muted-color);
}

.icon-btn {
  display: grid;
  place-items: center;
  width: 1.7rem;
  height: 1.7rem;
  margin: 0;
  padding: 0;
  flex-shrink: 0;

  border: 1px solid var(--pico-card-border-color);
  border-radius: var(--pico-border-radius);
  background: transparent;
  color: var(--pico-muted-color);
  cursor: pointer;
  transition: color 0.15s, border-color 0.15s;

  &:hover:not(:disabled) {
    color: var(--pico-primary);
    border-color: var(--pico-primary);
  }

  &.danger:hover:not(:disabled) {
    color: #e53e3e;
    border-color: #e53e3e;
  }

  i { font-size: 0.95rem; }
}

/* ── Slots ───────────────────────────────────────────────────────── */
.slots {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(190px, 1fr));
  gap: 0.6rem;
}

.slot {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  padding: 0.55rem;
  border-radius: 8px;
  border: 1px solid var(--pico-card-border-color);
  background: var(--pico-card-sectioning-background-color);
}

.slot.empty {
  align-items: center;
  justify-content: center;
  min-height: 108px;
  gap: 0.25rem;

  border-style: dashed;
  background: transparent;
  color: var(--pico-muted-color);
  font-size: 0.78rem;
  cursor: pointer;
  transition: color 0.15s, border-color 0.15s;

  &:hover:not(:disabled) {
    color: var(--pico-primary);
    border-color: var(--pico-primary);
  }

  i { font-size: 1.2rem; }
}

.member-head {
  display: flex;
  align-items: center;
  gap: 0.45rem;

  img {
    width: 38px;
    height: 38px;
    border-radius: 5px;
    flex-shrink: 0;
  }
}

.member-name {
  flex: 1;
  min-width: 0;
  font-size: 0.84rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.member-types {
  display: flex;
  gap: 0.2rem;
  flex-shrink: 0;
}

.type-icon {
  width: 16px;
  height: 16px;
  border-radius: 3px;
}

/* ── Techniques ──────────────────────────────────────────────────── */
.techniques {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  width: 100%;
  margin: 0;
  padding: 0;
  border: none;
  background: transparent;
  cursor: pointer;
}

.technique-chip {
  display: flex;
  align-items: center;
  gap: 0.3rem;
  padding: 0.15rem 0.35rem;
  border-radius: 4px;
  background: color-mix(in srgb, var(--pico-color) 7%, transparent);
  color: var(--pico-color);
  font-size: 0.74rem;
  text-align: left;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.technique-chip.empty {
  background: transparent;
  border: 1px dashed var(--pico-muted-border-color);
  color: var(--pico-muted-color);
  font-style: italic;
}

.techniques:hover .technique-chip:not(.empty) {
  background: color-mix(in srgb, var(--pico-primary) 18%, transparent);
}

.techniques:hover .technique-chip.empty {
  border-color: var(--pico-primary);
  color: var(--pico-primary);
}

.remove-member {
  position: absolute;
  top: 0.3rem;
  right: 0.3rem;
  width: 1.25rem;
  height: 1.25rem;
  opacity: 0;
  transition: opacity 0.15s;

  i { font-size: 0.8rem; }
}

.slot:hover .remove-member {
  opacity: 1;
}
</style>
