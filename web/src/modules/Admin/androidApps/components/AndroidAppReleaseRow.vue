<script setup lang="ts">
import { computed } from 'vue'
import {
  ANDROID_APP_RELEASES_GRID,
  ANDROID_VERSION_BY_SDK,
  type AndroidAppRelease,
} from '../types/adminAndroidApps.types'
import { formatBytes } from '@/utils/formatBytes'

// Une version importée d'une app. Le certificat est comparé à celui de la version proposée sur le
// store : une autre clé obligerait les appareils à désinstaller l'app.
const props = defineProps<{
  release: AndroidAppRelease
  recommendedCertSha256: string | null
  busy: boolean
}>()

const emit = defineEmits<{
  recommend: []
  delete: []
}>()

const androidLabel = computed(() => {
  const version = ANDROID_VERSION_BY_SDK[props.release.minSdk]
  return version ? `Android ${version}` : `API ${props.release.minSdk}`
})

const certDiffers = computed(() =>
  props.recommendedCertSha256 !== null && props.release.certSha256 !== props.recommendedCertSha256)

const uploadedLabel = computed(() => {
  const date = new Date(props.release.uploadedAt).toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' })
  return props.release.uploadedByName ? `${date} · ${props.release.uploadedByName}` : date
})

const uploadedHint = computed(() =>
  `Importée le ${new Date(props.release.uploadedAt).toLocaleString('fr-FR', { dateStyle: 'medium', timeStyle: 'short' })}`
  + (props.release.uploadedByName ? ` par ${props.release.uploadedByName}` : ''))
</script>

<template>
  <div
    class="release-row"
    :class="{ 'release-row--recommended': release.isRecommended }"
    :style="{ gridTemplateColumns: ANDROID_APP_RELEASES_GRID }"
  >
    <!-- VERSION -->
    <div class="cell cell--version" :title="`versionCode ${release.versionCode}`">
      <span class="version-name">{{ release.versionName ?? '—' }}</span>
      <span class="version-code">{{ release.versionCode }}</span>
    </div>

    <!-- ANDROID MINIMUM -->
    <div class="cell cell--muted" :title="`API ${release.minSdk}`">{{ androidLabel }}</div>

    <!-- TAILLE -->
    <div class="cell cell--muted right">{{ formatBytes(release.fileSize) }}</div>

    <!-- CERTIFICAT -->
    <div class="cell" :title="release.certSha256">
      <span v-if="certDiffers" class="chip chip--danger"><i class="mdi mdi-shield-alert-outline" />Autre clé</span>
      <span v-else class="cell--mono">{{ release.certSha256.slice(0, 12) }}</span>
    </div>

    <!-- IMPORT -->
    <div class="cell cell--muted" :title="uploadedHint">{{ uploadedLabel }}</div>

    <!-- NOTES -->
    <div class="cell cell--muted" :title="release.releaseNotes ?? ''">{{ release.releaseNotes ?? '' }}</div>

    <!-- STATUT -->
    <div class="cell">
      <span v-if="release.isRecommended" class="chip chip--primary"><i class="mdi mdi-store-check-outline" />Sur le store</span>
      <button v-else class="propose-btn" :disabled="busy" title="Proposer cette version aux appareils" @click="emit('recommend')">
        Proposer
      </button>
    </div>

    <!-- ACTIONS -->
    <div class="cell cell--actions">
      <a class="action-btn" :href="release.url" title="Télécharger l'APK" download>
        <i class="mdi mdi-download" />
      </a>
      <button
        class="action-btn action-btn--danger"
        :disabled="release.isRecommended || busy"
        :title="release.isRecommended ? 'Proposez une autre version sur le store avant de supprimer celle-ci' : 'Supprimer cette version'"
        @click="emit('delete')"
      >
        <i class="mdi mdi-trash-can-outline" />
      </button>
    </div>
  </div>
</template>

<style scoped lang="scss">
$danger: #ef4444;

.release-row {
  display: grid;
  align-items: center;
  column-gap: 0.6rem;
  padding: 0.4rem 0.6rem;
  background: var(--pico-card-sectioning-background-color, var(--pico-card-background-color));
  border: 1px solid var(--pico-card-border-color);
  border-radius: 0.45rem;
  font-size: 0.8rem;

  &:hover { box-shadow: inset 0 0 0 2px var(--pico-primary-border); }

  &--recommended { border-color: color-mix(in srgb, var(--pico-primary) 55%, transparent); }
}

.cell {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  min-width: 0;

  &--muted { color: var(--pico-muted-color); }
  &--mono { font-family: monospace; font-size: 0.75rem; color: var(--pico-muted-color); }
  &--actions { display: flex; align-items: center; justify-content: flex-end; gap: 0.15rem; overflow: visible; }
}

.right { text-align: right; }

.cell--version {
  display: flex;
  align-items: baseline;
  gap: 0.45rem;
}

.version-name { font-weight: 600; }

.version-code {
  font-family: monospace;
  font-size: 0.72rem;
  color: var(--pico-muted-color);
}

.chip {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.1rem 0.5rem;
  border-radius: 999px;
  font-size: 0.72rem;
  font-weight: 600;

  &--primary { background: color-mix(in srgb, var(--pico-primary) 14%, transparent); color: var(--pico-primary); }
  &--danger { background: color-mix(in srgb, #{$danger} 12%, transparent); color: #dc2626; }
}

.propose-btn {
  width: auto;
  margin: 0;
  padding: 0.12rem 0.6rem;
  border: 1px solid var(--pico-muted-border-color);
  border-radius: 999px;
  background: transparent;
  color: var(--pico-muted-color);
  font-size: 0.72rem;
  cursor: pointer;

  &:hover:not(:disabled) { border-color: var(--pico-primary); color: var(--pico-primary); }
  &:disabled { opacity: 0.4; cursor: not-allowed; }
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
  text-decoration: none;

  i { font-size: 1rem; }

  &:hover:not(:disabled) { border-color: var(--pico-muted-border-color); color: var(--pico-color); }
  &:disabled { opacity: 0.4; cursor: not-allowed; }
  &--danger:hover:not(:disabled) { border-color: $danger; color: $danger; }
}
</style>
