<script setup lang="ts">
import { computed, ref } from 'vue'
import { confirmAndroidAppImport } from '../fetch/adminAndroidApps.fetch'
import {
  ANDROID_APP_LABEL_MAX_LENGTH,
  ANDROID_APP_RELEASE_NOTES_MAX_LENGTH,
  ANDROID_VERSION_BY_SDK,
  type AndroidApp,
  type AndroidAppImportAnalysis,
  type AndroidAppImportWarningCode,
} from '../types/adminAndroidApps.types'
import { formatBytes } from '@/utils/formatBytes'
import { ApiException } from '@/services/ApiException'

// Popup ouverte à chaque import : rien n'est créé avant qu'on ait vu ce qui a été lu dans l'APK et
// ce qui change par rapport à la version proposée sur le store. Un clic à côté annule.
const props = defineProps<{ analysis: AndroidAppImportAnalysis }>()

const emit = defineEmits<{
  confirmed: [app: AndroidApp]
  cancel: []
}>()

type Level = 'changed' | 'warning' | 'danger' | null

const apk = computed(() => props.analysis.apk)
const existing = computed(() => props.analysis.existingApp)
const isNewApp = computed(() => existing.value === null)
const hasRecommended = computed(() => existing.value?.recommendedVersionCode != null)

const hasWarning = (code: AndroidAppImportWarningCode) =>
  props.analysis.warnings.some(warning => warning.code === code)

const certificateChanged = computed(() => hasWarning('CERTIFICATE_CHANGED'))

const severity = (code: AndroidAppImportWarningCode) =>
  code === 'CERTIFICATE_CHANGED' ? 'danger' : code === 'NEW_APP' ? 'info' : 'warning'

const severityIcon: Record<string, string> = {
  danger: 'mdi-alert',
  warning: 'mdi-alert-outline',
  info: 'mdi-information-outline',
}

const androidLabel = (minSdk: number | null | undefined) => {
  if (minSdk == null) return '—'
  const version = ANDROID_VERSION_BY_SDK[minSdk]
  return version ? `Android ${version}` : `API ${minSdk}`
}

const versionLabel = (name: string | null | undefined, code: number | null | undefined) =>
  code == null ? '—' : name ?? String(code)

// Une tuile par information, toutes bâties pareil pour rester alignées : libellé, valeur du fichier,
// précision, puis la valeur proposée sur le store quand l'app y est déjà. La couleur dit si le
// changement est normal, à surveiller ou bloquant.
const tiles = computed(() => {
  const current = hasRecommended.value ? existing.value : null
  const integer = new Intl.NumberFormat('fr-FR')

  return [
    {
      label: 'Version',
      icon: 'mdi-tag-outline',
      value: versionLabel(apk.value.versionName, apk.value.versionCode),
      detail: `versionCode ${apk.value.versionCode}`,
      previous: current ? versionLabel(current.recommendedVersionName, current.recommendedVersionCode) : null,
      level: (hasWarning('OLDER_THAN_RECOMMENDED') ? 'warning' : current ? 'changed' : null) as Level,
    },
    {
      label: 'Android minimum',
      icon: 'mdi-android',
      value: androidLabel(apk.value.minSdk),
      detail: `API ${apk.value.minSdk}`,
      previous: current ? androidLabel(current.recommendedMinSdk) : null,
      level: (hasWarning('MIN_SDK_RAISED')
        ? 'warning'
        : current && apk.value.minSdk !== current.recommendedMinSdk ? 'changed' : null) as Level,
    },
    {
      label: 'Taille',
      icon: 'mdi-harddisk',
      value: formatBytes(apk.value.fileSize),
      detail: `${integer.format(apk.value.fileSize)} octets`,
      previous: current?.recommendedFileSize != null ? formatBytes(current.recommendedFileSize) : null,
      level: null as Level,
    },
    {
      label: 'Certificat',
      icon: certificateChanged.value ? 'mdi-shield-alert-outline' : 'mdi-shield-check-outline',
      value: certificateChanged.value ? 'Différent' : current ? 'Identique' : apk.value.certSha256.slice(0, 12),
      detail: `Schéma ${apk.value.signatureSchemes.join(', ') || '—'}`,
      previous: current?.recommendedCertSha256 ? current.recommendedCertSha256.slice(0, 12) : null,
      level: (certificateChanged.value ? 'danger' : null) as Level,
    },
  ]
})

const label = ref(props.analysis.suggestedLabel)
const recommend = ref(true)
const releaseNotes = ref('')
const saving = ref(false)
const error = ref<string | null>(null)

const cancel = () => {
  if (!saving.value) emit('cancel')
}

const confirm = async () => {
  error.value = null
  const trimmedLabel = label.value.trim()

  if (isNewApp.value && (!trimmedLabel || trimmedLabel.length > ANDROID_APP_LABEL_MAX_LENGTH)) {
    error.value = `Le nom doit faire entre 1 et ${ANDROID_APP_LABEL_MAX_LENGTH} caractères`
    return
  }

  saving.value = true
  try {
    const app = await confirmAndroidAppImport(props.analysis.importId, {
      label: isNewApp.value ? trimmedLabel : null,
      recommend: recommend.value,
      releaseNotes: releaseNotes.value.trim() || null,
    })
    emit('confirmed', app)
  } catch (e) {
    error.value = e instanceof ApiException ? e.message : "Erreur lors de l'import"
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Teleport to="body">
    <div class="overlay" @click.self="cancel">
      <div class="popup">
        <header class="popup-header">
          <div class="identity">
            <span class="app-icon"><i class="mdi mdi-android" /></span>
            <div class="identity-text">
              <h3>{{ isNewApp ? label || apk.packageName : existing?.label }}</h3>
              <span class="package">{{ apk.packageName }}</span>
            </div>
            <span class="pill" :class="isNewApp ? 'pill--info' : 'pill--muted'">
              {{ isNewApp ? 'Nouvelle app' : 'Nouvelle version' }}
            </span>
          </div>
          <button type="button" class="icon-btn" title="Fermer" :disabled="saving" @click="cancel">
            <i class="mdi mdi-close" />
          </button>
        </header>

        <div class="popup-body">
          <!-- AVERTISSEMENTS -->
          <ul v-if="analysis.warnings.length" class="callouts">
            <li
              v-for="warning in analysis.warnings"
              :key="warning.code"
              class="callout"
              :class="`callout--${severity(warning.code)}`"
            >
              <i class="mdi" :class="severityIcon[severity(warning.code)]" />
              <span>{{ warning.message }}</span>
            </li>
          </ul>
          <p v-else-if="hasRecommended" class="callout callout--ok">
            <i class="mdi mdi-check-circle-outline" />
            <span>Mise à jour ordinaire : version plus récente, même certificat.</span>
          </p>

          <!-- APK -->
          <p class="section-label">
            Fichier
          </p>
          <div class="tiles">
            <div v-for="tile in tiles" :key="tile.label" class="tile" :class="tile.level ? `tile--${tile.level}` : null">
              <span class="tile-icon"><i class="mdi" :class="tile.icon" /></span>
              <div class="tile-body">
                <span class="tile-label">{{ tile.label }}</span>
                <strong class="tile-value">{{ tile.value }}</strong>
                <span class="tile-detail">{{ tile.detail }}</span>
                <span v-if="hasRecommended" class="tile-previous">
                  Sur le store : <b>{{ tile.previous ?? '—' }}</b>
                </span>
              </div>
            </div>
          </div>

          <!-- SIGNATURE -->
          <p class="section-label">Signature</p>
          <div class="signature">
            <span class="signer">{{ apk.certSubject || 'Signataire inconnu' }}</span>
            <code class="hash" :class="{ 'hash--danger': certificateChanged }" :title="'Certificat ' + apk.certSha256">
              {{ apk.certSha256 }}
            </code>
            <code v-if="certificateChanged && existing?.recommendedCertSha256" class="hash hash--muted" title="Certificat de la version proposée sur le store">
              {{ existing.recommendedCertSha256 }}
            </code>
          </div>

          <!-- NOM -->
          <template v-if="isNewApp">
            <label class="section-label" for="android-app-label">Nom sur le store</label>
            <input
              id="android-app-label"
              v-model="label"
              class="field"
              :maxlength="ANDROID_APP_LABEL_MAX_LENGTH"
              :disabled="saving"
              placeholder="Nom affiché sur les appareils"
              @keyup.enter="confirm"
            />
          </template>

          <!-- NOTES -->
          <label class="section-label" for="android-app-notes">
            Notes de version
            <span class="section-hint">facultatif</span>
          </label>
          <textarea
            id="android-app-notes"
            v-model="releaseNotes"
            class="field field--area"
            rows="2"
            :maxlength="ANDROID_APP_RELEASE_NOTES_MAX_LENGTH"
            :disabled="saving"
            placeholder="Ce qui change dans cette version…"
          />

          <!-- STORE -->
          <label class="store-toggle" :class="{ off: !recommend }">
            <input v-model="recommend" type="checkbox" role="switch" :disabled="saving" />
            <span class="store-toggle-text">
              <strong>Proposer cette version sur le store</strong>
              <span>{{ recommend
                ? 'Les appareils verront cette version et proposeront la mise à jour.'
                : 'Importée sans être distribuée : pour une version de test (nightly).' }}</span>
            </span>
          </label>
        </div>

        <footer class="popup-footer">
          <p v-if="error" class="error">{{ error }}</p>
          <button type="button" class="btn-ghost" :disabled="saving" @click="cancel">Annuler</button>
          <button
            type="button"
            class="btn-primary"
            :class="{ 'btn-primary--danger': certificateChanged }"
            :disabled="saving"
            :aria-busy="saving"
            @click="confirm"
          >
            <i v-if="!saving" class="mdi mdi-download-outline" />
            {{ certificateChanged ? 'Importer malgré tout' : 'Importer' }}
          </button>
        </footer>
      </div>
    </div>
  </Teleport>
</template>

<style scoped lang="scss">
$danger: #e53e3e;
$warning: #f59e0b;
$success: #22c55e;

.overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, .5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9000;
}

.popup {
  display: flex;
  flex-direction: column;
  width: 540px;
  max-width: calc(100vw - 2rem);
  max-height: calc(100vh - 3rem);
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 12px;
  box-shadow: 0 12px 40px rgba(0, 0, 0, .35);
}

/* EN-TÊTE */
.popup-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 1rem 1.1rem 0.75rem;
}

.identity {
  display: flex;
  align-items: center;
  gap: 0.7rem;
  min-width: 0;
}

.app-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  width: 2.6rem;
  height: 2.6rem;
  border-radius: 10px;
  background: color-mix(in srgb, var(--pico-primary) 16%, transparent);
  color: var(--pico-primary);
  font-size: 1.45rem;
}

.identity-text {
  display: flex;
  flex-direction: column;
  min-width: 0;

  h3 {
    margin: 0;
    font-size: 1rem;
    color: var(--pico-contrast);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
}

.package {
  font-family: var(--pico-font-family-monospace);
  font-size: 0.7rem;
  color: var(--pico-muted-color);
}

.pill {
  flex-shrink: 0;
  padding: 0.1rem 0.5rem;
  border-radius: 999px;
  font-size: 0.65rem;
  font-weight: 700;

  &--info { background: color-mix(in srgb, var(--pico-primary) 18%, transparent); color: var(--pico-primary); }
  &--muted { background: color-mix(in srgb, var(--pico-contrast) 8%, transparent); color: var(--pico-muted-color); }
}

/* CORPS */
.popup-body {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  padding: 0 1.1rem 0.75rem;
}

.section-label {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  margin: 1rem 0 0.45rem;
  font-size: 0.7rem;
  font-weight: 600;
  color: var(--pico-muted-color);
  text-transform: uppercase;
  letter-spacing: 0.06em;
}

.section-hint {
  font-weight: 400;
  font-size: 0.68rem;
  letter-spacing: 0;
  text-transform: none;
}

/* AVERTISSEMENTS */
.callouts {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.callout {
  display: flex;
  gap: 0.55rem;
  align-items: flex-start;
  margin: 0;
  padding: 0.55rem 0.7rem;
  border-radius: 8px;
  font-size: 0.78rem;
  line-height: 1.4;
  list-style: none;
  background: color-mix(in srgb, var(--pico-primary) 10%, transparent);

  i { font-size: 1rem; line-height: 1.2; color: var(--pico-primary); }

  &--warning { background: color-mix(in srgb, #{$warning} 12%, transparent); i { color: $warning; } }
  &--danger { background: color-mix(in srgb, #{$danger} 12%, transparent); i { color: $danger; } }
  &--ok { background: color-mix(in srgb, #{$success} 10%, transparent); i { color: $success; } }
}

/* TUILES — 2 × 2, même structure partout pour que tout reste aligné. */
.tiles {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.5rem;
}

.tile {
  display: flex;
  align-items: flex-start;
  gap: 0.65rem;
  min-width: 0;
  padding: 0.7rem 0.75rem;
  border-radius: 10px;
  background: color-mix(in srgb, var(--pico-contrast) 5%, transparent);
  border: 1px solid color-mix(in srgb, var(--pico-contrast) 7%, transparent);

  &--changed {
    .tile-value { color: var(--pico-primary); }
    .tile-icon { color: var(--pico-primary); background: color-mix(in srgb, var(--pico-primary) 15%, transparent); }
  }

  &--warning {
    border-color: color-mix(in srgb, #{$warning} 45%, transparent);
    .tile-value { color: $warning; }
    .tile-icon { color: $warning; background: color-mix(in srgb, #{$warning} 15%, transparent); }
  }

  &--danger {
    border-color: color-mix(in srgb, #{$danger} 50%, transparent);
    .tile-value { color: $danger; }
    .tile-icon { color: $danger; background: color-mix(in srgb, #{$danger} 15%, transparent); }
  }
}

.tile-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  width: 2rem;
  height: 2rem;
  border-radius: 8px;
  background: color-mix(in srgb, var(--pico-contrast) 7%, transparent);
  color: var(--pico-muted-color);
  font-size: 1.05rem;
}

.tile-body {
  display: flex;
  flex-direction: column;
  min-width: 0;
  line-height: 1.3;
}

.tile-label {
  font-size: 0.62rem;
  font-weight: 600;
  color: var(--pico-muted-color);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  white-space: nowrap;
}

.tile-value {
  font-size: 0.98rem;
  color: var(--pico-contrast);
  overflow-wrap: anywhere;
}

.tile-detail {
  font-size: 0.68rem;
  color: var(--pico-muted-color);
  overflow-wrap: anywhere;
}

.tile-previous {
  margin-top: 0.35rem;
  font-size: 0.68rem;
  color: var(--pico-muted-color);
  overflow-wrap: anywhere;

  b { font-weight: 600; color: var(--pico-color); }
}

/* SIGNATURE */
.signature {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  padding: 0.55rem 0.6rem;
  border-radius: 8px;
  background: color-mix(in srgb, var(--pico-contrast) 5%, transparent);
}

.signer {
  font-size: 0.78rem;
  color: var(--pico-contrast);
}

.hash {
  padding: 0;
  background: none;
  font-size: 0.66rem;
  color: var(--pico-muted-color);
  overflow-wrap: anywhere;

  &--danger { color: $danger; }
  &--muted { text-decoration: line-through; opacity: 0.7; }
}

/* CHAMPS — compacts : Pico les rend beaucoup trop hauts par défaut. */
.field {
  width: 100%;
  height: 2.2rem;
  margin: 0;
  padding: 0 0.7rem;
  font-size: 0.85rem;
  border-radius: 8px;

  &--area {
    height: auto;
    padding: 0.5rem 0.7rem;
    resize: vertical;
  }
}

/* STORE */
.store-toggle {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin: 1rem 0 0;
  padding: 0.65rem 0.75rem;
  border-radius: 8px;
  border: 1px solid color-mix(in srgb, var(--pico-primary) 45%, transparent);
  background: color-mix(in srgb, var(--pico-primary) 8%, transparent);
  cursor: pointer;
  transition: background 0.15s, border-color 0.15s;

  input { margin: 0; flex-shrink: 0; }

  &.off {
    border-color: var(--pico-muted-border-color);
    background: transparent;
  }
}

.store-toggle-text {
  display: flex;
  flex-direction: column;
  gap: 0.1rem;

  strong { font-size: 0.82rem; color: var(--pico-contrast); }
  span { font-size: 0.72rem; color: var(--pico-muted-color); }
}

/* PIED */
.popup-footer {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 0.75rem;
  padding: 0.75rem 1.1rem 1rem;
  border-top: 1px solid var(--pico-muted-border-color);
}

.error {
  flex: 1;
  margin: 0;
  font-size: 0.78rem;
  color: $danger;
}

.icon-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  width: 1.9rem;
  height: 1.9rem;
  margin: 0;
  padding: 0;
  border: none;
  border-radius: 8px;
  background: transparent;
  color: var(--pico-muted-color);
  font-size: 1.05rem;
  cursor: pointer;

  &:hover:not(:disabled) {
    background: color-mix(in srgb, var(--pico-contrast) 10%, transparent);
    color: var(--pico-contrast);
  }
}

.btn-primary,
.btn-ghost {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  width: auto;
  margin: 0;
  padding: 0.4rem 1rem;
  font-size: 0.82rem;
  border-radius: 8px;
}

.btn-primary--danger {
  background: $danger;
  border-color: $danger;
  color: #fff;
}

.btn-ghost {
  background: transparent;
  border: 1px solid var(--pico-muted-border-color);
  color: inherit;

  &:hover:not(:disabled) {
    border-color: var(--pico-contrast);
  }
}

@media (max-width: 520px) {
  .tiles { grid-template-columns: minmax(0, 1fr); }
}
</style>
