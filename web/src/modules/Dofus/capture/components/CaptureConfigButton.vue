<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useFloating, offset, autoUpdate } from '@floating-ui/vue'
import { useEnv } from '@/composables/useEnv'
import { useOS } from '@/composables/useOS'
import { useDofusStore } from '@/modules/Dofus/dofus.store'
import { formatNumber } from '@/utils/formatNumber'
import { useCaptureStore } from '../capture.store'

//* Délai après une connexion de personnage au-delà duquel l'absence de prix moyens signale
//* probablement une mise à jour du client qui a renommé les messages.
const PRICE_LIST_GRACE_MS = 60_000

const capture = useCaptureStore()
const dofus = useDofusStore()
const { isElectron } = useEnv()
const { isLinux } = useOS()

const isOpen = ref(false)
const reference = ref<HTMLElement | null>(null)
const floating = ref<HTMLElement | null>(null)
const now = ref(Date.now())
let clock: number | null = null

const { floatingStyles } = useFloating(reference, floating, {
  placement: 'bottom-end',
  middleware: [offset(5)],
  whileElementsMounted: autoUpdate,
})

const status = computed(() => capture.status)

const formatTime = (ts: number) =>
  new Date(ts).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit', second: '2-digit' })

const priceListState = computed(() => {
  const s = status.value
  if (!capture.isFeatureActive('hdvSniffer') || !s) return null

  if (s.lastPriceListAt && (!s.lastConnectionAt || s.lastPriceListAt >= s.lastConnectionAt)) {
    return { kind: 'ok', text: `${formatNumber(s.lastPriceListCount ?? 0)} prix moyens reçus à ${formatTime(s.lastPriceListAt)}` }
  }
  if (s.lastConnectionAt && now.value - s.lastConnectionAt > PRICE_LIST_GRACE_MS) {
    return { kind: 'warn', text: 'Personnage connecté mais aucun prix reçu : le client Dofus a peut-être été mis à jour.' }
  }
  if (s.lastConnectionAt) {
    return { kind: 'wait', text: 'Connexion détectée, réception des prix…' }
  }
  return { kind: 'wait', text: 'En attente de la connexion d\'un personnage.' }
})

const selectorWarning = computed(() => {
  if (!capture.prefs.hdvSniffer) return null
  const code = dofus.currentGameVersion?.code
  if (code && code !== 'dofus3') return 'Le sélecteur de jeu n\'est pas sur Dofus 3 : les prix capturés ne seront pas enregistrés.'
  if (dofus.currentGameServerId === null) return 'Aucun serveur sélectionné : les prix capturés ne seront pas enregistrés.'
  return null
})

const savedUnder = computed(() => {
  const version = dofus.currentGameVersion?.name
  const server = dofus.currentGameServer?.name
  return version && server ? `${version} · ${server}` : null
})

const toggle = (feature: 'autofocus' | 'hdvSniffer', e: Event) => {
  capture.setFeature(feature, (e.target as HTMLInputElement).checked)
}

const toggleOpen = () => {
  isOpen.value = !isOpen.value
  if (isOpen.value) capture.recheckRequirements()
}

const onClickOutside = (e: MouseEvent) => {
  const target = e.target as HTMLElement
  if (reference.value && floating.value && !reference.value.contains(target) && !floating.value.contains(target)) {
    isOpen.value = false
  }
}

onMounted(() => {
  document.addEventListener('click', onClickOutside)
  clock = window.setInterval(() => (now.value = Date.now()), 5000)
})

onBeforeUnmount(() => {
  document.removeEventListener('click', onClickOutside)
  if (clock !== null) clearInterval(clock)
})
</script>

<template>
  <div v-if="isElectron && isLinux" class="capture-config-container">
    <button
      ref="reference"
      class="capture-config-button"
      type="button"
      aria-label="Capture Dofus 3"
      :title="capture.isRunning ? 'Capture Dofus 3 active' : 'Capture Dofus 3 inactive'"
      @click="toggleOpen"
    >
      <i class="mdi mdi-radar" :class="{ active: capture.isRunning }" />
    </button>

    <div v-if="isOpen" ref="floating" :style="floatingStyles" class="floating-panel">
      <header class="panel-header">
        <strong>Capture Dofus 3</strong>
        <span class="state" :class="{ on: capture.isRunning }">
          {{ capture.isRunning ? 'Active' : 'Inactive' }}
        </span>
      </header>
      <p class="panel-hint">
        Écoute passive du jeu avec tshark, pour Dofus 3 uniquement. Tourne en tâche de fond,
        même fenêtre switcher fermée.
      </p>

      <section class="features">
        <label class="feature" :class="{ on: capture.isFeatureActive('autofocus') }">
          <input
            type="checkbox"
            role="switch"
            :checked="capture.prefs.autofocus"
            @change="toggle('autofocus', $event)"
          />
          <span class="feature-text">
            <span class="feature-name">Autofocus</span>
            <span class="feature-desc">Focus le personnage suivant à la fin d'un tour</span>
          </span>
        </label>
        <p v-if="capture.errors.autofocus" class="line error">{{ capture.errors.autofocus }}</p>

        <label class="feature" :class="{ on: capture.isFeatureActive('hdvSniffer') }">
          <input
            type="checkbox"
            role="switch"
            :checked="capture.prefs.hdvSniffer"
            @change="toggle('hdvSniffer', $event)"
          />
          <span class="feature-text">
            <span class="feature-name">Sniffer HDV</span>
            <span class="feature-desc">Enregistre les prix moyens et ceux des objets cliqués</span>
          </span>
        </label>
        <p v-if="capture.errors.hdvSniffer" class="line error">{{ capture.errors.hdvSniffer }}</p>
      </section>

      <hr />

      <section class="status">
        <template v-if="capture.tsharkMissing">
          <p class="line error">tshark n'est pas installé.</p>
          <code>sudo apt install tshark</code>
        </template>
        <template v-else-if="capture.permissionsMissing">
          <p class="line error">tshark n'a pas les droits de capture.</p>
          <code>sudo usermod -aG wireshark $USER</code>
          <p class="line muted">Puis fermer et rouvrir la session.</p>
        </template>
        <p v-else-if="status?.error" class="line error">{{ status.error }}</p>
        <p v-else-if="capture.isRunning" class="line ok">En écoute sur le port 5555</p>
        <p v-else class="line muted">Aucune fonction activée</p>

        <p v-if="priceListState" class="line" :class="priceListState.kind">{{ priceListState.text }}</p>
        <p v-if="selectorWarning" class="line warn">{{ selectorWarning }}</p>
        <p v-else-if="capture.prefs.hdvSniffer && savedUnder" class="line muted">
          Prix enregistrés sous {{ savedUnder }}
        </p>

        <button class="outline secondary recheck" type="button" @click="capture.recheckRequirements()">
          <i class="mdi mdi-refresh" /> Revérifier tshark
        </button>
      </section>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.capture-config-container { display: inline-flex; }

.capture-config-button {
  display: inline-flex; align-items: center; justify-content: center;
  width: 2.25rem; height: 2.25rem; padding: 0; margin: 0;
  border-radius: var(--pico-border-radius); border: 1px solid transparent; background: transparent;
  color: var(--pico-muted-color); cursor: pointer; transition: all 0.15s ease;
  &:hover { color: var(--pico-primary); background-color: var(--pico-card-background-color); border-color: var(--pico-muted-border-color); }
  i { font-size: 1.2rem; &.active { color: var(--pico-ins-color); } }
}

.floating-panel {
  position: absolute; z-index: 1000; width: 290px; padding: 0.75rem;
  background: var(--pico-background-color); border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius); box-shadow: var(--pico-card-box-shadow); font-size: 0.75rem;
}

.panel-header {
  display: flex; justify-content: space-between; align-items: center;
  strong { text-transform: uppercase; letter-spacing: 0.05em; font-weight: 700; }
  .state {
    font-size: 0.65rem; font-weight: 700; text-transform: uppercase; color: var(--pico-muted-color);
    &.on { color: var(--pico-ins-color); }
  }
}

.panel-hint { margin: 0.4rem 0 0.75rem; font-size: 0.65rem; color: var(--pico-muted-color); line-height: 1.35; }

.features { display: flex; flex-direction: column; gap: 0.4rem; }

.feature {
  display: flex; align-items: center; gap: 0.5rem; margin: 0; padding: 0.4rem 0.5rem; cursor: pointer;
  border: 1px solid var(--pico-muted-border-color); border-left-width: 3px; border-radius: 4px;
  &.on { border-left-color: var(--pico-form-element-valid-border-color); .feature-name { color: var(--pico-ins-color); } }
  input { margin: 0; scale: 0.7; flex-shrink: 0; }
  .feature-text { display: flex; flex-direction: column; }
  .feature-name { font-weight: 700; font-size: 0.72rem; }
  .feature-desc { font-size: 0.62rem; color: var(--pico-muted-color); }
}

hr { margin: 0.75rem 0; opacity: 0.4; }

.status {
  display: flex; flex-direction: column; gap: 0.3rem;
  code { font-size: 0.65rem; padding: 0.2rem 0.4rem; }
  .recheck {
    margin: 0.3rem 0 0; padding: 0.2rem 0.5rem; width: auto; align-self: flex-start;
    font-size: 0.62rem; display: inline-flex; align-items: center; gap: 0.25rem;
  }
}

.line {
  margin: 0; font-size: 0.68rem; line-height: 1.35;
  &.ok { color: var(--pico-ins-color); }
  &.error { color: var(--pico-del-color); }
  &.warn { color: var(--pico-del-color); }
  &.wait, &.muted { color: var(--pico-muted-color); }
}
</style>
