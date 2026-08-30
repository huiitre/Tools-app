<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useTemtemTeamsStore } from '@/modules/Temtem/teams/teams.store'
import { useTemtemdexStore } from '@/modules/Temtem/temtemdex/temtemdex.store'
import { typesOf } from '@/modules/Temtem/shared/temtem.helpers'
import type { TemtemSummary, TemtemTechnique } from '@/modules/Temtem/shared/types/temtem.types'
import SimulatorTemtemPickerModal from '../components/SimulatorTemtemPickerModal.vue'

const teamsStore = useTemtemTeamsStore()
const dexStore = useTemtemdexStore()
const selectedTeamId = ref<number | null>(null)
const enemies = ref<(TemtemSummary | null)[]>([null, null])
const pickingIndex = ref<number | null>(null)
const recentEnemyIds = ref<number[]>([])
const RECENT_ENEMIES_STORAGE_KEY = 'temtem.simulator.recent-enemy-ids'

onMounted(() => {
  teamsStore.ensureLoaded()

  try {
    const stored = JSON.parse(localStorage.getItem(RECENT_ENEMIES_STORAGE_KEY) ?? '[]')
    if (Array.isArray(stored)) recentEnemyIds.value = stored.filter((id): id is number => Number.isInteger(id)).slice(0, 10)
  } catch {
    recentEnemyIds.value = []
  }
})

watch(() => teamsStore.teams, teams => {
  if (selectedTeamId.value === null && teams.length) selectedTeamId.value = teams[0].id
}, { immediate: true })

const selectedTeam = computed(() => teamsStore.teams.find(team => team.id === selectedTeamId.value) ?? null)

function multiplierForType(attackerTypeId: number, defender: TemtemSummary): number | null {
  if (!dexStore.effectiveness.length) return null

  const first = dexStore.effectiveness.find(entry => entry.attackerTypeId === attackerTypeId && entry.defenderTypeId === defender.type1.id)?.multiplier
  const defenderSecondType = defender.type2
  const second = defenderSecondType
    ? dexStore.effectiveness.find(entry => entry.attackerTypeId === attackerTypeId && entry.defenderTypeId === defenderSecondType.id)?.multiplier
    : 1

  return first === undefined || second === undefined ? null : first * second
}

function multiplier(attacker: TemtemSummary, defender: TemtemSummary): number | null {
  const values = typesOf(attacker)
    .map(type => multiplierForType(type.id, defender))
    .filter((value): value is number => value !== null)

  return values.length ? Math.max(...values) : null
}

function techniqueMultiplier(technique: TemtemTechnique, defender: TemtemSummary): number | null {
  return multiplierForType(technique.type.id, defender)
}

// Sans connaître les quatre techniques réellement équipées par l'adversaire, ses propres types
// donnent l'alerte la plus utile : le pire multiplicateur qu'il pourrait infliger à ce Temtem.
function dangerMultiplier(defender: TemtemSummary, enemy: TemtemSummary): number | null {
  const values = typesOf(enemy)
    .map(type => multiplierForType(type.id, defender))
    .filter((value): value is number => value !== null)

  return values.length ? Math.max(...values) : null
}

function state(value: number | null) {
  if (value === null || value === 1) return 'neutral'
  return value > 1 ? 'strong' : 'weak'
}

function dangerState(value: number | null) {
  if (value === null || value === 1) return 'neutral'
  return value > 1 ? 'weak' : 'strong'
}

function chooseEnemy(temtem: TemtemSummary) {
  if (pickingIndex.value !== null) enemies.value[pickingIndex.value] = temtem
  recentEnemyIds.value = [temtem.id, ...recentEnemyIds.value.filter(id => id !== temtem.id)].slice(0, 10)
  localStorage.setItem(RECENT_ENEMIES_STORAGE_KEY, JSON.stringify(recentEnemyIds.value))
  pickingIndex.value = null
}

function removeEnemy(index: number) {
  enemies.value[index] = null
}

function resetEnemies() {
  enemies.value = [null, null]
}
</script>

<template>
  <div class="simulator">
    <aside class="teams-panel">
      <p class="eyebrow">Vos équipes</p>
      <button
        v-for="team in teamsStore.teams"
        :key="team.id"
        type="button"
        class="team-choice"
        :class="{ selected: team.id === selectedTeamId }"
        @click="selectedTeamId = team.id"
      >
        <strong>{{ team.name }}</strong>
        <span class="team-mini-members" :aria-label="`${team.members.length} Temtem`">
          <span v-for="member in team.members" :key="member.id" class="team-mini" :title="member.temtem.name">
            <img v-if="member.temtem.imageUrl" :src="member.temtem.imageUrl" :alt="member.temtem.name">
            <i v-else class="mdi mdi-pokeball" aria-hidden="true" />
          </span>
        </span>
      </button>
      <p v-if="!teamsStore.loading && !teamsStore.teams.length" class="empty">Créez une équipe pour commencer.</p>
    </aside>

    <main class="arena">
      <header>
        <div>
          <p class="eyebrow">Simulateur de combat</p>
          <h2>{{ selectedTeam?.name ?? 'Choisissez une équipe' }}</h2>
        </div>
      </header>

      <div class="battle-layout">
        <div class="analysis-column">
          <section class="enemies-section">
            <header class="enemies-header">
              <div><h3>Adversaires</h3><p>Choisissez les deux Temtem en face.</p></div>
              <button v-if="enemies.some(Boolean)" type="button" class="reset-enemies" @click="resetEnemies">Réinitialiser</button>
            </header>
            <div class="enemy-slots">
              <div
                v-for="(enemy, index) in enemies"
                :key="index"
                class="enemy-card"
                :class="{ empty: !enemy }"
              >
                <button type="button" class="enemy-select" @click="pickingIndex = index">
                  <template v-if="enemy">
                    <img v-if="enemy.imageUrl" :src="enemy.imageUrl" :alt="enemy.name">
                    <span><strong>{{ enemy.name }}</strong><span class="types"><img v-for="type in typesOf(enemy)" :key="type.id" :src="type.imageUrl ?? ''" :alt="type.name"></span></span>
                  </template>
                  <template v-else><i class="mdi mdi-plus" /><span>Choisir un adversaire</span></template>
                </button>
                <button v-if="enemy" type="button" class="enemy-remove" :aria-label="`Retirer ${enemy.name}`" :title="`Retirer ${enemy.name}`" @click="removeEnemy(index)">
                  <i class="mdi mdi-close" aria-hidden="true" />
                </button>
              </div>
            </div>
          </section>

          <section v-if="selectedTeam?.members.length" class="team-analysis">
            <header class="analysis-header">
              <div><h3>Votre équipe</h3><p>Les couleurs indiquent l’efficacité contre chaque adversaire sélectionné.</p></div>
              <div v-if="enemies.some(Boolean)" class="opponent-headings">
                <span v-for="(enemy, index) in enemies" :key="index">{{ enemy?.name ?? 'Libre' }}</span>
              </div>
            </header>

            <div class="team-members">
              <article v-for="member in selectedTeam.members" :key="member.id" class="member-analysis">
                <div class="member-overview">
                  <img v-if="member.temtem.imageUrl" :src="member.temtem.imageUrl" :alt="member.temtem.name">
                  <div><strong>{{ member.temtem.name }}</strong><span class="types"><img v-for="type in typesOf(member.temtem)" :key="type.id" :src="type.imageUrl ?? ''" :alt="type.name"></span></div>
                  <div v-if="enemies.some(Boolean)" class="member-affinities">
                    <span v-for="(enemy, index) in enemies" :key="index" class="affinity">
                      <span class="affinity-line" title="Meilleure attaque selon les types de ce Temtem"><i class="mdi mdi-sword-cross" aria-hidden="true" /><span class="effectiveness" :class="state(enemy ? multiplier(member.temtem, enemy) : null)">{{ enemy ? `${multiplier(member.temtem, enemy)}×` : '—' }}</span></span>
                      <span class="affinity-line" title="Danger potentiel selon les types de l’adversaire"><i class="mdi mdi-shield-outline" aria-hidden="true" /><span class="effectiveness" :class="dangerState(enemy ? dangerMultiplier(member.temtem, enemy) : null)">{{ enemy ? `${dangerMultiplier(member.temtem, enemy)}×` : '—' }}</span></span>
                    </span>
                  </div>
                </div>

                <div v-if="member.techniques.length" class="techniques">
                  <div v-for="technique in member.techniques" :key="technique.id" class="technique">
                    <span class="technique-name"><img v-if="technique.type.imageUrl" :src="technique.type.imageUrl" :alt="technique.type.name">{{ technique.name }}</span>
                    <div v-if="enemies.some(Boolean)" class="effectiveness-values">
                      <span v-for="(enemy, index) in enemies" :key="index" class="effectiveness" :class="state(enemy ? techniqueMultiplier(technique, enemy) : null)">{{ enemy ? `${techniqueMultiplier(technique, enemy)}×` : '—' }}</span>
                    </div>
                  </div>
                </div>
                <p v-else class="no-techniques">Aucune technique ajoutée pour ce Temtem.</p>
              </article>
            </div>
          </section>

          <p v-else-if="selectedTeam" class="empty">Cette équipe n’a pas de membre.</p>
        </div>

        <aside class="future-panel" aria-label="Informations complémentaires à venir" />
      </div>
    </main>

    <SimulatorTemtemPickerModal v-if="pickingIndex !== null" :recent-ids="recentEnemyIds" @close="pickingIndex = null" @pick="chooseEnemy" />
  </div>
</template>

<style scoped lang="scss">
.simulator {
  display: grid;
  grid-template-columns: 220px minmax(0, 1fr);
  min-height: 100%;
}

.teams-panel {
  padding: 1.5rem 1rem;
  border-right: 1px solid var(--pico-card-border-color);
}

.eyebrow {
  margin: 0 0 .35rem;
  color: var(--pico-muted-color);
  font-size: .67rem;
  font-weight: 700;
  letter-spacing: .08em;
  text-transform: uppercase;
}

.team-choice {
  display: block;
  width: 100%;
  margin: 0 0 .5rem;
  padding: .65rem .7rem;
  border: 1px solid transparent;
  background: transparent;
  color: var(--pico-color);
  text-align: left;
}

.team-choice:hover,
.team-choice.selected {
  border-color: var(--pico-primary);
  color: var(--pico-primary);
}

.team-mini-members {
  display: flex;
  gap: .2rem;
  margin-top: .45rem;
}

.team-mini {
  display: grid;
  width: 25px;
  height: 25px;
  overflow: hidden;
  place-items: center;
  border: 1px solid var(--pico-card-border-color);
  border-radius: 50%;
  background: var(--pico-card-sectioning-background-color);
}

.team-mini img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.team-mini i { font-size: .8rem; }

.arena {
  width: 100%;
  max-width: none;
  box-sizing: border-box;
  margin: 0;
  padding: 1.5rem;
}

.arena > header {
  display: flex;
  align-items: end;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.arena h2,
.arena h3 { margin: 0; }

.arena > header > p,
.analysis-header p {
  margin: 0;
  color: var(--pico-muted-color);
  font-size: .8rem;
}

.battle-layout {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 450px;
  align-items: start;
  gap: 1rem;
}

.analysis-column { display: grid; align-content: start; gap: 1rem; }

.enemies-section,
.team-analysis,
.future-panel {
  padding: 1rem;
  border: 1px solid var(--pico-card-border-color);
  border-radius: var(--pico-border-radius);
  background: var(--pico-card-background-color);
}

.enemies-header { display: flex; align-items: start; justify-content: space-between; gap: .75rem; margin-bottom: .75rem; }
.enemies-section h3 { margin: 0; font-size: .9rem; }
.enemies-header p { margin: .15rem 0 0; color: var(--pico-muted-color); font-size: .78rem; }
.reset-enemies { width: auto; height: 1.75rem; margin: 0; padding: .2rem .45rem; border: 1px solid var(--pico-muted-border-color); background: transparent; color: var(--pico-muted-color); font-size: .65rem; white-space: nowrap; }
.reset-enemies:hover { border-color: var(--pico-primary); color: var(--pico-primary); }

.enemy-slots {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: .65rem;
}

.future-panel { min-height: 180px; }

.enemy-card {
  display: flex;
  align-items: center;
  min-height: 76px;
  margin: 0;
  padding: .6rem;
  border: 1px solid var(--pico-card-border-color);
  border-radius: 8px;
  background: var(--pico-card-sectioning-background-color);
}

.enemy-select {
  display: flex;
  flex: 1;
  align-items: center;
  gap: .65rem;
  min-width: 0;
  margin: 0;
  padding: 0;
  border: 0;
  background: transparent;
  color: inherit;
  text-align: left;
  cursor: pointer;
}

.enemy-card:hover,
.enemy-card:hover strong,
.enemy-card:hover > span { color: var(--pico-primary) !important; }

.enemy-card:hover { border-color: var(--pico-primary); }
.enemy-select > img { width: 58px; height: 58px; border-radius: 6px; object-fit: cover; }
.enemy-select > span { display: grid; gap: .25rem; }
.enemy-card.empty { justify-content: center; border-style: dashed; background: transparent; color: var(--pico-muted-color); text-align: center; }
.enemy-card.empty .enemy-select { justify-content: center; }
.enemy-card.empty .enemy-select > i { font-size: 1.25rem; }

.enemy-remove {
  display: grid;
  width: 1.7rem;
  height: 1.7rem;
  margin: 0 0 0 .4rem;
  padding: 0;
  place-items: center;
  border: 0;
  background: transparent;
  color: var(--pico-muted-color);
}

.enemy-remove:hover { color: var(--pico-primary); }

.types { display: flex; gap: .2rem; }
.types img { width: 16px; height: 16px; border-radius: 3px; }

.analysis-header {
  display: flex;
  align-items: end;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: .85rem;
}

.analysis-header h3 { margin-bottom: .2rem; font-size: .95rem; }

.team-members {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: .65rem;
}

.opponent-headings,
.effectiveness-values {
  display: grid;
  grid-template-columns: repeat(2, 50px);
  gap: .35rem;
}

.member-affinities {
  display: grid;
  grid-template-columns: repeat(2, 74px);
  gap: .35rem;
  margin-left: auto;
}

.affinity { display: grid; justify-items: end; gap: .15rem; }
.affinity-line { display: inline-flex; align-items: center; gap: .15rem; }
.affinity-line i { color: var(--pico-muted-color); font-size: .8rem; }

.opponent-headings span {
  overflow: hidden;
  color: var(--pico-muted-color);
  font-size: .68rem;
  text-align: center;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.member-overview,
.technique {
  display: flex;
  align-items: center;
  gap: .65rem;
  padding: .55rem .65rem;
  border: 1px solid var(--pico-card-border-color);
  background: var(--pico-card-sectioning-background-color);
}

.member-overview { border-radius: 8px 8px 0 0; }
.member-overview > img { width: 44px; height: 44px; border-radius: 6px; object-fit: cover; }
.member-overview > div:nth-child(2) { display: grid; gap: .3rem; }
.member-overview .effectiveness-values { margin-left: auto; }

.technique {
  min-height: 35px;
  border-top: 0;
  padding-left: 1.1rem;
  font-size: .8rem;
}

.technique:last-child { border-radius: 0 0 8px 8px; }

.technique-name {
  display: flex;
  align-items: center;
  gap: .4rem;
  min-width: 0;
}

.technique-name img { width: 16px; height: 16px; border-radius: 3px; }
.technique .effectiveness-values { margin-left: auto; }

.effectiveness { padding: .3rem .35rem; border-radius: 5px; font-size: .75rem; font-variant-numeric: tabular-nums; text-align: center; }
.affinity .effectiveness { min-width: 30px; padding: .16rem .22rem; font-size: .65rem; }
.effectiveness.strong { background: color-mix(in srgb, #48a86e 18%, transparent); color: #48a86e; }
.effectiveness.weak { background: color-mix(in srgb, #dc5252 18%, transparent); color: #dc5252; }
.effectiveness.neutral { background: var(--pico-card-background-color); color: var(--pico-muted-color); }

.no-techniques {
  margin: 0;
  padding: .45rem .7rem;
  border: 1px solid var(--pico-card-border-color);
  border-top: 0;
  border-radius: 0 0 8px 8px;
  color: var(--pico-muted-color);
  font-size: .75rem;
}

.empty { color: var(--pico-muted-color); font-size: .8rem; }

@media (max-width: 760px) {
  .simulator { grid-template-columns: 1fr; }
  .teams-panel { display: flex; gap: .4rem; overflow-x: auto; border-right: 0; border-bottom: 1px solid var(--pico-card-border-color); }
  .teams-panel .eyebrow,
  .teams-panel .empty { display: none; }
  .team-choice { min-width: 135px; }
  .arena { padding: 1rem; }
  .arena > header { display: block; }
  .arena > header > p { margin-top: .5rem; }
  .battle-layout { grid-template-columns: 1fr; }
  .future-panel { display: none; }
  .analysis-header { align-items: start; flex-direction: column; }
}

@media (max-width: 500px) {
  .opponent-headings { display: none; }
  .opponent-headings,
  .effectiveness-values { grid-template-columns: repeat(2, 58px); }
  .member-overview,
  .technique { align-items: flex-start; }
  .member-overview > img { width: 36px; height: 36px; }
  .effectiveness { font-size: .7rem; }
}
</style>
