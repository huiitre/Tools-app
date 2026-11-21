<script setup lang="ts">
import { computed, ref } from 'vue'
import { useAdminAndroidAppsStore } from '../store/adminAndroidApps.store'
import { deleteAndroidApp, deleteAndroidAppRelease, updateAndroidApp } from '../fetch/adminAndroidApps.fetch'
import {
  ANDROID_APPS_GRID,
  ANDROID_APP_RELEASES_GRID,
  ANDROID_VERSION_BY_SDK,
  type AndroidApp,
  type AndroidAppRelease,
  type UpdateAndroidAppPayload,
} from '../types/adminAndroidApps.types'
import AndroidAppReleaseRow from './AndroidAppReleaseRow.vue'
import AndroidAppEditModal from './AndroidAppEditModal.vue'
import AndroidAppNotesModal from './AndroidAppNotesModal.vue'
import AndroidAppConfirmModal from './AndroidAppConfirmModal.vue'
import { formatBytes } from '@/utils/formatBytes'
import { ApiException } from '@/services/ApiException'
import toast from '@/services/toast'

// Une app du store, et sous elle ses versions quand on la déplie.
const props = defineProps<{ app: AndroidApp }>()

const store = useAdminAndroidAppsStore()

const expanded = ref(false)
const editOpen = ref(false)
const notesOpen = ref(false)
const deleteAppOpen = ref(false)
const releaseToDelete = ref<AndroidAppRelease | null>(null)
const saving = ref(false)
const deleting = ref(false)

const recommended = computed(() => props.app.releases.find(release => release.isRecommended) ?? null)
const totalSize = computed(() => props.app.releases.reduce((sum, release) => sum + release.fileSize, 0))

const lastUpload = computed(() =>
  props.app.releases.reduce<string | null>((latest, release) =>
    latest === null || release.uploadedAt > latest ? release.uploadedAt : latest, null))

const formatDate = (value: string) =>
  new Date(value).toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' })

const androidLabel = (minSdk: number) => {
  const version = ANDROID_VERSION_BY_SDK[minSdk]
  return version ? `Android ${version}` : `API ${minSdk}`
}

const versionLabel = (release: AndroidAppRelease) => release.versionName ?? String(release.versionCode)

const update = async (payload: UpdateAndroidAppPayload, success: string) => {
  saving.value = true
  try {
    store.upsertLocally(await updateAndroidApp(props.app.id, payload))
    toast.success(success)
  } catch (e) {
    toast.error(e instanceof ApiException ? e.message : "Erreur lors de la mise à jour de l'app")
  } finally {
    saving.value = false
  }
}

const recommend = (release: AndroidAppRelease) =>
  update({ recommendedReleaseId: release.id }, `${props.app.label} : version ${versionLabel(release)} proposée sur le store`)

const onActiveChange = (event: Event) => {
  const isActive = (event.target as HTMLInputElement).checked
  update({ isActive }, isActive ? `${props.app.label} remise sur le store` : `${props.app.label} retirée du store`)
}

const confirmDeleteRelease = async () => {
  const release = releaseToDelete.value
  if (!release) return

  deleting.value = true
  try {
    await deleteAndroidAppRelease(props.app.id, release.id)
    store.removeReleaseLocally(props.app.id, release.id)
    toast.success(`Version ${versionLabel(release)} supprimée`)
    releaseToDelete.value = null
  } catch (e) {
    toast.error(e instanceof ApiException ? e.message : 'Erreur lors de la suppression de la version')
  } finally {
    deleting.value = false
  }
}

const confirmDeleteApp = async () => {
  deleting.value = true
  try {
    await deleteAndroidApp(props.app.id)
    store.removeLocally(props.app.id)
    toast.success(`${props.app.label} supprimée`)
  } catch (e) {
    toast.error(e instanceof ApiException ? e.message : "Erreur lors de la suppression de l'app")
    deleting.value = false
  }
}
</script>

<template>
  <div class="app-row" :class="{ 'app-row--inactive': !app.isActive }" :style="{ gridTemplateColumns: ANDROID_APPS_GRID }">
    <!-- CHEVRON -->
    <div class="cell chevron" :title="expanded ? 'Masquer les versions' : 'Afficher les versions'" @click="expanded = !expanded">
      <i class="mdi" :class="expanded ? 'mdi-chevron-down' : 'mdi-chevron-right'" />
    </div>

    <!-- NOM -->
    <div class="cell cell--name clickable" :title="app.label" @click="expanded = !expanded">{{ app.label }}</div>

    <!-- PAQUET -->
    <div class="cell cell--mono" :title="app.packageName">{{ app.packageName }}</div>

    <!-- SUR LE STORE -->
    <div class="cell">
      <span v-if="!app.isActive" class="chip chip--muted">Désactivée</span>
      <span v-else-if="recommended" class="chip chip--primary" :title="`versionCode ${recommended.versionCode}`">
        {{ versionLabel(recommended) }}
      </span>
      <span v-else class="chip chip--warning" title="Aucune version n'est proposée : l'app n'apparaît pas sur le store">Aucune</span>
    </div>

    <!-- ANDROID MINIMUM -->
    <div class="cell cell--muted">{{ recommended ? androidLabel(recommended.minSdk) : '—' }}</div>

    <!-- TAILLE -->
    <div class="cell cell--muted right">{{ recommended ? formatBytes(recommended.fileSize) : '—' }}</div>

    <!-- VERSIONS -->
    <div class="cell cell--muted right">{{ app.releases.length }}</div>

    <!-- DERNIER IMPORT -->
    <div class="cell cell--muted">{{ lastUpload ? formatDate(lastUpload) : '—' }}</div>

    <!-- ACTIVE -->
    <div class="cell center" :title="app.isActive ? 'Retirer du store sans rien supprimer' : 'Remettre sur le store'">
      <input type="checkbox" role="switch" class="switch" :checked="app.isActive" :disabled="saving" @change="onActiveChange" />
    </div>

    <!-- ACTIONS -->
    <div class="cell cell--actions">
      <!-- Coloré quand l'app a des notes : on voit d'un coup d'œil où il y a de quoi lire. -->
      <button
        class="action-btn"
        :class="{ 'action-btn--filled': app.adminNotes }"
        :title="app.adminNotes ? 'Voir les notes' : 'Ajouter des notes'"
        @click="notesOpen = true"
      >
        <i class="mdi" :class="app.adminNotes ? 'mdi-note-text' : 'mdi-note-plus-outline'" />
      </button>
      <button class="action-btn" title="Modifier" @click="editOpen = true">
        <i class="mdi mdi-pencil-outline" />
      </button>
      <button class="action-btn action-btn--danger" title="Supprimer l'app et tous ses APK" @click="deleteAppOpen = true">
        <i class="mdi mdi-trash-can-outline" />
      </button>
    </div>
  </div>

  <!-- VERSIONS -->
  <div v-if="expanded" class="releases">
    <div class="releases-header" :style="{ gridTemplateColumns: ANDROID_APP_RELEASES_GRID }">
      <span>Version</span>
      <span>Android min.</span>
      <span class="right">Taille</span>
      <span>Certificat</span>
      <span>Import</span>
      <span>Notes</span>
      <span>Statut</span>
      <span />
    </div>

    <AndroidAppReleaseRow
      v-for="release in app.releases"
      :key="release.id"
      :release="release"
      :recommended-cert-sha256="recommended?.certSha256 ?? null"
      :busy="saving"
      @recommend="recommend(release)"
      @delete="releaseToDelete = release"
    />
  </div>

  <AndroidAppNotesModal
    v-if="notesOpen"
    :app="app"
    @updated="updated => { store.upsertLocally(updated); notesOpen = false }"
    @cancel="notesOpen = false"
  />

  <AndroidAppEditModal
    v-if="editOpen"
    :app="app"
    @updated="updated => { store.upsertLocally(updated); editOpen = false }"
    @cancel="editOpen = false"
  />

  <AndroidAppConfirmModal
    v-if="releaseToDelete"
    title="Supprimer cette version"
    icon="mdi-trash-can-outline"
    confirm-label="Supprimer"
    busy-label="Suppression…"
    :busy="deleting"
    @confirm="confirmDeleteRelease"
    @cancel="releaseToDelete = null"
  >
    <p>
      La version <strong>{{ versionLabel(releaseToDelete) }}</strong> ({{ releaseToDelete.versionCode }}) de
      <strong>{{ app.label }}</strong> et son APK ({{ formatBytes(releaseToDelete.fileSize) }}) seront supprimés
      définitivement.
    </p>
  </AndroidAppConfirmModal>

  <AndroidAppConfirmModal
    v-if="deleteAppOpen"
    title="Supprimer l'app"
    icon="mdi-trash-can-outline"
    confirm-label="Supprimer l'app et ses APK"
    busy-label="Suppression…"
    :busy="deleting"
    @confirm="confirmDeleteApp"
    @cancel="deleteAppOpen = false"
  >
    <p>
      <strong>{{ app.label }}</strong> sera retirée du store, et
      <strong>ses {{ app.releases.length }} APK ({{ formatBytes(totalSize) }}) seront supprimés définitivement</strong>.
    </p>
    <p>Pour simplement la retirer du store sans rien perdre, désactivez-la plutôt.</p>
  </AndroidAppConfirmModal>
</template>

<style scoped lang="scss">
$danger: #ef4444;

.app-row {
  display: grid;
  align-items: center;
  column-gap: 0.6rem;
  padding: 0.45rem 0.6rem;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 0.45rem;
  font-size: 0.85rem;

  &:hover { box-shadow: inset 0 0 0 2px var(--pico-primary-border); }

  // Gardée visible pour être réactivée, mais visiblement hors du store.
  &--inactive {
    .cell--name, .cell--mono { opacity: 0.55; }
  }
}

.cell {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  min-width: 0;

  &--name { font-weight: 600; }
  &--muted { color: var(--pico-muted-color); font-size: 0.8rem; }
  &--mono { font-family: monospace; font-size: 0.75rem; color: var(--pico-muted-color); }
  &--actions { display: flex; align-items: center; justify-content: flex-end; gap: 0.15rem; overflow: visible; }
}

.right { text-align: right; }
.center { text-align: center; }
.clickable { cursor: pointer; }

.chevron {
  text-align: center;
  cursor: pointer;
  color: var(--pico-muted-color);
  &:hover { color: var(--pico-color); }
}

.switch { margin: 0; }

.chip {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  max-width: 100%;
  padding: 0.1rem 0.5rem;
  border-radius: 999px;
  font-size: 0.72rem;
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;

  &--primary { background: color-mix(in srgb, var(--pico-primary) 14%, transparent); color: var(--pico-primary); }
  &--warning { background: color-mix(in srgb, #f59e0b 14%, transparent); color: #d97706; }
  &--muted { background: color-mix(in srgb, var(--pico-muted-color) 14%, transparent); color: var(--pico-muted-color); }
}

.releases {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  margin: 0 0 0.35rem 2.2rem;
  padding-left: 0.6rem;
  border-left: 2px solid color-mix(in srgb, var(--pico-primary) 35%, transparent);
}

.releases-header {
  display: grid;
  align-items: center;
  column-gap: 0.6rem;
  padding: 0.15rem 0.6rem 0;
  font-size: 0.72rem;
  color: var(--pico-muted-color);

  span { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
}

.action-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 1.75rem;
  height: 1.75rem;
  padding: 0;
  margin: 0;
  border: 1px solid transparent;
  border-radius: var(--pico-border-radius);
  background: transparent;
  color: var(--pico-muted-color);
  cursor: pointer;

  i { font-size: 1rem; }

  &:hover:not(:disabled) { border-color: var(--pico-muted-border-color); color: var(--pico-color); }
  &:disabled { opacity: 0.4; cursor: not-allowed; }
  &--danger:hover:not(:disabled) { border-color: $danger; color: $danger; }
}

.action-btn--filled { color: var(--pico-primary); }
</style>
