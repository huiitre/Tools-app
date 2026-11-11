<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import axios from 'axios'
import LanguagePicker from '../components/LanguagePicker.vue'
import { useTranslationStore } from '../store/translation.store'
import { translateTexts } from '../fetch/translation.fetch'
import { TRANSLATION_MAX_CHARACTERS, type DetectedLanguage } from '../types/translation.types'
import { languageName } from '../utils/languageName'
import { ApiException } from '@/services/ApiException'
import { useClipboard } from '@/composables/useClipboard'

const store = useTranslationStore()
const { copy } = useClipboard()

const text = ref('')
const result = ref('')
const detected = ref<DetectedLanguage | null>(null)
const durationMs = ref<number | null>(null)
const loading = ref(false)
const error = ref<string | null>(null)
const languagesError = ref(false)
const truncated = ref(false)
const textarea = ref<HTMLTextAreaElement | null>(null)

const length = computed(() => text.value.length)
const fill = computed(() => Math.min(length.value / TRANSLATION_MAX_CHARACTERS, 1))
const nearLimit = computed(() => fill.value >= 0.9)
const atLimit = computed(() => length.value >= TRANSLATION_MAX_CHARACTERS)
const formatCount = (n: number) => n.toLocaleString('fr-FR')

// La source réelle, une fois la détection faite : c'est elle que l'inversion reprend.
const effectiveSource = computed(() =>
  store.source === 'auto' ? detected.value?.language ?? null : store.source,
)
const canSwap = computed(() => !!effectiveSource.value && effectiveSource.value !== store.target)

const durationLabel = computed(() => {
  if (durationMs.value === null) return null
  const seconds = durationMs.value / 1000
  return `Traduit en ${seconds < 0.1 ? '< 0,1' : seconds.toLocaleString('fr-FR', { maximumFractionDigits: 1 })} s`
})

/* ── Traduction ─────────────────────────────────────────── */
let debounce: ReturnType<typeof setTimeout> | null = null
let controller: AbortController | null = null

function reset() {
  controller?.abort()
  result.value = ''
  detected.value = null
  durationMs.value = null
  error.value = null
  loading.value = false
}

async function translateNow() {
  if (debounce) clearTimeout(debounce)
  if (!text.value.trim()) return reset()
  if (length.value > TRANSLATION_MAX_CHARACTERS || !store.loaded) return

  // Une frappe plus récente rend la réponse en attente inutile : on l'annule.
  controller?.abort()
  const current = new AbortController()
  controller = current

  loading.value = true
  error.value = null
  try {
    const response = await translateTexts(
      [text.value],
      store.source === 'auto' ? null : store.source,
      store.target,
      current.signal,
    )
    result.value = response.texts[0] ?? ''
    detected.value = response.detected
    durationMs.value = response.durationMs
  } catch (e) {
    if (axios.isCancel(e)) return
    error.value = e instanceof ApiException ? e.message : 'La traduction a échoué. Réessaie dans un instant.'
  } finally {
    if (controller === current) loading.value = false
  }
}

// Comme les traducteurs en ligne : on traduit quand la frappe marque une pause.
function schedule() {
  if (debounce) clearTimeout(debounce)
  if (!text.value.trim()) return reset()
  debounce = setTimeout(translateNow, 650)
}

watch(text, schedule)
watch(() => [store.source, store.target], () => {
  if (store.source === 'auto') detected.value = null
  if (text.value.trim()) translateNow()
})

/* ── Actions ────────────────────────────────────────────── */
function swap() {
  if (!canSwap.value) return
  const previousSource = effectiveSource.value!
  const previousResult = result.value

  store.source = store.target
  store.target = previousSource
  if (previousResult) {
    text.value = previousResult
    result.value = ''
  }
}

function clear() {
  text.value = ''
  truncated.value = false
  reset()
  nextTick(() => textarea.value?.focus())
}

function onPaste(event: ClipboardEvent) {
  const pasted = event.clipboardData?.getData('text') ?? ''
  const field = event.target as HTMLTextAreaElement
  const kept = text.value.length - (field.selectionEnd - field.selectionStart)
  truncated.value = kept + pasted.length > TRANSLATION_MAX_CHARACTERS
}

function onKeydown(event: KeyboardEvent) {
  if ((event.ctrlKey || event.metaKey) && event.key === 'Enter') {
    event.preventDefault()
    translateNow()
  }
}

watch(text, () => {
  if (!atLimit.value) truncated.value = false
})

/* ── Lecture à voix haute ───────────────────────────────── */
const canSpeak = typeof window !== 'undefined' && 'speechSynthesis' in window
const speaking = ref<'source' | 'target' | null>(null)

function speak(side: 'source' | 'target') {
  if (!canSpeak) return
  if (speaking.value === side) {
    window.speechSynthesis.cancel()
    speaking.value = null
    return
  }
  const content = side === 'source' ? text.value : result.value
  const lang = side === 'source' ? effectiveSource.value : store.target
  if (!content.trim()) return

  window.speechSynthesis.cancel()
  const utterance = new SpeechSynthesisUtterance(content)
  if (lang) utterance.lang = lang
  utterance.onend = utterance.onerror = () => { speaking.value = null }
  speaking.value = side
  window.speechSynthesis.speak(utterance)
}

onMounted(async () => {
  try {
    await store.ensureLanguages()
  } catch {
    languagesError.value = true
  }
  textarea.value?.focus()
})

onBeforeUnmount(() => {
  if (debounce) clearTimeout(debounce)
  controller?.abort()
  if (canSpeak) window.speechSynthesis.cancel()
})
</script>

<template>
  <main class="translator-page">
    <header class="translator-title">
      <h1>Traduction</h1>
      <p>Tape ou colle un texte, la traduction suit.</p>
    </header>

    <div v-if="languagesError" class="translator-banner" role="alert">
      <i class="mdi mdi-cloud-off-outline" aria-hidden="true" />
      Le service de traduction ne répond pas pour le moment.
    </div>

    <section class="translator" :class="{ busy: loading }">
      <div class="translator-bar">
        <div class="bar-side">
          <LanguagePicker
            v-model="store.source"
            :codes="store.languages.map(l => l.code)"
            :detected="detected?.language"
            allow-auto
            label="Langue du texte"
          />
        </div>

        <button
          type="button"
          class="swap"
          :disabled="!canSwap"
          title="Inverser les langues"
          aria-label="Inverser les langues"
          @click="swap"
        >
          <i class="mdi mdi-swap-horizontal" aria-hidden="true" />
        </button>

        <div class="bar-side bar-side--target">
          <LanguagePicker v-model="store.target" :codes="store.targetCodes" label="Langue de traduction" />
        </div>
      </div>

      <div class="translator-panes">
        <!-- SOURCE -->
        <div class="pane pane--source">
          <textarea
            ref="textarea"
            v-model="text"
            class="source-input"
            :maxlength="TRANSLATION_MAX_CHARACTERS"
            placeholder="Saisis le texte à traduire"
            spellcheck="true"
            @paste="onPaste"
            @keydown="onKeydown"
          />

          <button v-if="text" type="button" class="icon-btn clear-btn" title="Effacer" aria-label="Effacer le texte" @click="clear">
            <i class="mdi mdi-close" aria-hidden="true" />
          </button>

          <footer class="pane-footer">
            <div class="pane-actions">
              <button
                v-if="canSpeak"
                type="button"
                class="icon-btn"
                :class="{ active: speaking === 'source' }"
                :disabled="!text.trim()"
                :title="speaking === 'source' ? 'Arrêter la lecture' : 'Écouter'"
                @click="speak('source')"
              >
                <i class="mdi" :class="speaking === 'source' ? 'mdi-stop' : 'mdi-volume-high'" aria-hidden="true" />
              </button>
              <span class="hint">Ctrl + Entrée pour traduire tout de suite</span>
            </div>

            <div class="counter" :class="{ near: nearLimit, full: atLimit }">
              <span>{{ formatCount(length) }} / {{ formatCount(TRANSLATION_MAX_CHARACTERS) }}</span>
              <span class="counter-track"><span class="counter-fill" :style="{ width: `${fill * 100}%` }" /></span>
            </div>
          </footer>

          <p v-if="truncated" class="pane-notice">
            <i class="mdi mdi-content-cut" aria-hidden="true" />
            Texte coupé à {{ formatCount(TRANSLATION_MAX_CHARACTERS) }} caractères, la limite d'une traduction.
          </p>
        </div>

        <!-- TRADUCTION -->
        <div class="pane pane--target" aria-live="polite">
          <span class="progress" aria-hidden="true" />

          <div class="target-body">
            <p v-if="error" class="target-error">
              <i class="mdi mdi-alert-circle-outline" aria-hidden="true" />
              {{ error }}
            </p>

            <p v-else-if="result" class="target-text">{{ result }}</p>

            <div v-else-if="loading" class="target-skeleton" aria-label="Traduction en cours">
              <span /><span /><span />
            </div>

            <p v-else class="target-placeholder">La traduction apparaîtra ici</p>
          </div>

          <footer class="pane-footer">
            <div class="pane-actions">
              <button
                v-if="canSpeak"
                type="button"
                class="icon-btn"
                :class="{ active: speaking === 'target' }"
                :disabled="!result"
                :title="speaking === 'target' ? 'Arrêter la lecture' : 'Écouter'"
                @click="speak('target')"
              >
                <i class="mdi" :class="speaking === 'target' ? 'mdi-stop' : 'mdi-volume-high'" aria-hidden="true" />
              </button>
              <button
                type="button"
                class="icon-btn"
                :disabled="!result"
                title="Copier la traduction"
                @click="copy(result, 'Traduction')"
              >
                <i class="mdi mdi-content-copy" aria-hidden="true" />
              </button>
            </div>

            <span v-if="durationLabel && result && !error" class="meta">
              <template v-if="detected">Détectée : {{ languageName(detected.language) }} · </template>{{ durationLabel }}
            </span>
          </footer>
        </div>
      </div>
    </section>
  </main>
</template>

<style scoped lang="scss">
.translator-page {
  // Remplit la zone de contenu de Page.vue (qui défile) ; en dessous de 520 px, la page défile.
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 520px;
  max-width: 1180px;
  margin: 0 auto;
  padding: 1.5rem 1rem;
  box-sizing: border-box;
}

.translator-title {
  margin-bottom: 1.1rem;

  h1 { margin: 0; font-size: 1.6rem; font-weight: 800; letter-spacing: -0.01em; }
  p { margin: 0.25rem 0 0; color: var(--pico-muted-color); font-size: 0.9rem; }
}

.translator-banner {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 1rem;
  padding: 0.7rem 0.9rem;
  border: 1px solid var(--pico-form-element-invalid-border-color);
  border-radius: 12px;
  background: color-mix(in srgb, var(--pico-del-color) 8%, transparent);
  color: var(--pico-del-color);
  font-size: 0.9rem;
}

/* ── Carte ─────────────────────────────────────────────── */
.translator {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  position: relative;
  border: 1px solid var(--pico-card-border-color);
  border-radius: 20px;
  background: var(--pico-card-background-color);
  box-shadow: var(--pico-card-box-shadow);
  overflow: hidden;
}

.translator-bar {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto minmax(0, 1fr);
  align-items: center;
  gap: 0.5rem;
  padding: 0.55rem 0.9rem;
  border-bottom: 1px solid var(--pico-muted-border-color);
}

.bar-side { display: flex; min-width: 0; }
.bar-side--target { padding-left: 0.5rem; }

.swap {
  display: grid;
  place-items: center;
  width: 2.5rem;
  height: 2.5rem;
  margin: 0;
  padding: 0;
  border: 1px solid var(--pico-muted-border-color);
  border-radius: 50%;
  // Pas --pico-background-color : Pico la remplace par l'accent à l'intérieur d'un bouton.
  background: var(--pico-card-background-color);
  color: inherit;
  cursor: pointer;
  transition: transform 0.25s ease, border-color 0.15s ease, color 0.15s ease;

  i { font-size: 1.25rem; }

  &:hover:not(:disabled) {
    border-color: var(--pico-primary);
    color: var(--pico-primary);
    transform: rotate(180deg);
  }

  &:disabled { opacity: 0.4; cursor: not-allowed; }
}

.translator-panes {
  flex: 1;
  min-height: 0;
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  grid-template-rows: minmax(0, 1fr);
}

/* ── Volets ─────────────────────────────────────────────── */
.pane {
  position: relative;
  display: flex;
  flex-direction: column;
  min-height: 0;
  padding: 1.1rem 0.5rem 0.75rem 1.25rem;
}

.pane--target {
  border-left: 1px solid var(--pico-muted-border-color);
  background: var(--pico-card-sectioning-background-color);
}

.source-input {
  flex: 1;
  width: 100%;
  min-height: 0;
  margin: 0;
  padding: 0 2.5rem 0 0;
  border: 0;
  background: transparent;
  box-shadow: none;
  color: inherit;
  font-size: 1.25rem;
  line-height: 1.55;
  resize: none;
  overflow-y: auto;
  // Pico anime la couleur des champs : inutile ici, et visible au moindre changement de focus.
  transition: none;

  &:focus { box-shadow: none; }
  &::placeholder { color: var(--pico-form-element-placeholder-color); }
}

.clear-btn {
  position: absolute;
  top: 0.9rem;
  right: 1.4rem;
}

// Seule cette zone défile : le pied (actions, durée) reste visible.
.target-body {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  overflow-y: auto;
  padding-right: 0.75rem;
}

.target-text {
  flex: 1;
  margin: 0;
  font-size: 1.25rem;
  line-height: 1.55;
  white-space: pre-wrap;
  word-break: break-word;
  transition: opacity 0.15s ease;
}

.busy .target-text { opacity: 0.55; }

.target-placeholder {
  flex: 1;
  margin: 0;
  color: var(--pico-muted-color);
  font-size: 1.25rem;
  line-height: 1.55;
}

.target-error {
  flex: 1;
  display: flex;
  align-items: flex-start;
  gap: 0.45rem;
  margin: 0;
  color: var(--pico-del-color);
  font-size: 0.95rem;

  i { font-size: 1.15rem; }
}

.target-skeleton {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.65rem;

  span {
    height: 1rem;
    border-radius: 6px;
    background: linear-gradient(
      90deg,
      var(--pico-muted-border-color) 0%,
      color-mix(in srgb, var(--pico-muted-border-color) 40%, transparent) 50%,
      var(--pico-muted-border-color) 100%
    );
    background-size: 200% 100%;
    animation: shimmer 1.4s ease-in-out infinite;

    &:nth-child(1) { width: 85%; }
    &:nth-child(2) { width: 65%; }
    &:nth-child(3) { width: 40%; }
  }
}

/* Barre de progression discrète en haut du volet pendant une traduction. */
.progress {
  position: absolute;
  inset: 0 0 auto 0;
  height: 2px;
  overflow: hidden;
  opacity: 0;
  transition: opacity 0.2s ease;

  &::after {
    content: '';
    position: absolute;
    inset: 0;
    width: 35%;
    background: var(--pico-primary);
    animation: slide 1.1s ease-in-out infinite;
  }
}

.busy .progress { opacity: 1; }

/* ── Pied des volets ───────────────────────────────────── */
.pane-footer {
  flex-shrink: 0;
  padding-right: 0.75rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  min-height: 2.5rem;
  margin-top: 0.75rem;
}

.pane-actions {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  min-width: 0;
}

.hint {
  margin-left: 0.35rem;
  color: var(--pico-muted-color);
  font-size: 0.72rem;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.meta {
  color: var(--pico-muted-color);
  font-size: 0.75rem;
  text-align: right;
}

.icon-btn {
  display: grid;
  place-items: center;
  width: 2.25rem;
  height: 2.25rem;
  margin: 0;
  padding: 0;
  border: 1px solid transparent;
  border-radius: 50%;
  background: transparent;
  color: var(--pico-muted-color);
  cursor: pointer;
  transition: background 0.15s ease, color 0.15s ease;

  i { font-size: 1.15rem; }

  &:hover:not(:disabled), &.active {
    background: color-mix(in srgb, var(--pico-primary) 12%, transparent);
    color: var(--pico-primary);
  }

  &:disabled { opacity: 0.35; cursor: not-allowed; }
}

.counter {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.25rem;
  color: var(--pico-muted-color);
  font-size: 0.75rem;
  font-variant-numeric: tabular-nums;

  &.near { color: var(--pico-primary); }
  &.full { color: var(--pico-del-color); font-weight: 700; }
}

.counter-track {
  width: 90px;
  height: 3px;
  border-radius: 999px;
  background: var(--pico-muted-border-color);
  overflow: hidden;
}

.counter-fill {
  display: block;
  height: 100%;
  border-radius: inherit;
  background: currentColor;
  transition: width 0.2s ease;
}

.pane-notice {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  margin: 0.25rem 0 0;
  color: var(--pico-del-color);
  font-size: 0.78rem;
}

// Barres de défilement fines, aux couleurs du thème.
.source-input, .target-body {
  scrollbar-width: thin;
  scrollbar-color: var(--pico-muted-border-color) transparent;

  &::-webkit-scrollbar { width: 6px; }
  &::-webkit-scrollbar-track { background: transparent; }
  &::-webkit-scrollbar-thumb { background: var(--pico-muted-border-color); border-radius: 999px; }
}

@keyframes shimmer {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

@keyframes slide {
  0% { transform: translateX(-100%); }
  100% { transform: translateX(300%); }
}

/* ── Mobile : volets empilés ───────────────────────────── */
@media (max-width: 768px) {
  // Volets empilés à hauteur fixe : c'est la page qui défile, chaque volet garde son défilement.
  .translator-page { height: auto; min-height: 0; padding: 1rem 0.75rem 2rem; }

  .translator-panes { grid-template-columns: 1fr; grid-template-rows: auto auto; }

  .pane { height: 46dvh; min-height: 220px; }
  .source-input { font-size: 1.1rem; }
  .target-text, .target-placeholder { font-size: 1.1rem; }

  .pane--target {
    border-left: 0;
    border-top: 1px solid var(--pico-muted-border-color);
  }

  .hint { display: none; }
}
</style>
