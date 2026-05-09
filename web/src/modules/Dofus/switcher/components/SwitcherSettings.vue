<script setup lang="ts">
import { ref, onMounted } from 'vue'
import LS from '@/services/localStorage'

interface HotkeyConfig {
  prevKeycode: number | null
  prevLabel: string | null
  nextKeycode: number | null
  nextLabel: string | null
  debounce: number
  syncMode: boolean
  autofocus: boolean
}

const KEYCODE_LABELS: Record<number, string> = {
  1: 'Escape',
  2: '1', 3: '2', 4: '3', 5: '4', 6: '5', 7: '6', 8: '7', 9: '8', 10: '9', 11: '0',
  12: '-', 13: '=', 14: 'Backspace', 15: 'Tab',
  16: 'Q', 17: 'W', 18: 'E', 19: 'R', 20: 'T', 21: 'Y', 22: 'U', 23: 'I', 24: 'O', 25: 'P',
  26: '[', 27: ']', 28: 'Enter',
  30: 'A', 31: 'S', 32: 'D', 33: 'F', 34: 'G', 35: 'H', 36: 'J', 37: 'K', 38: 'L',
  39: ';', 40: "'", 41: '`', 42: 'Shift', 43: '\\',
  44: 'Z', 45: 'X', 46: 'C', 47: 'V', 48: 'B', 49: 'N', 50: 'M',
  51: ',', 52: '.', 53: '/', 54: 'Shift R', 55: 'Num *',
  56: 'Alt', 57: 'Space', 58: 'Caps Lock',
  59: 'F1', 60: 'F2', 61: 'F3', 62: 'F4', 63: 'F5', 64: 'F6',
  65: 'F7', 66: 'F8', 67: 'F9', 68: 'F10',
  69: 'Num Lock', 70: 'Scroll Lock',
  71: 'Num 7', 72: 'Num 8', 73: 'Num 9', 74: 'Num -',
  75: 'Num 4', 76: 'Num 5', 77: 'Num 6', 78: 'Num +',
  79: 'Num 1', 80: 'Num 2', 81: 'Num 3', 82: 'Num 0', 83: 'Num .',
  87: 'F11', 88: 'F12',
  3613: 'Enter', 3614: 'Ctrl R', 3615: 'Num /',
  3616: 'Print Screen', 3617: 'Alt R', 3618: 'Home',
  3619: 'Up', 3620: 'Page Up', 3621: 'Left', 3622: 'Right',
  3623: 'End', 3624: 'Down', 3625: 'Page Down',
  3626: 'Insert', 3627: 'Delete',
  3653: 'Meta L', 3654: 'Meta R',
}

function keycodeLabel(keycode: number | null): string | null {
  if (keycode === null) return null
  return KEYCODE_LABELS[keycode] ?? `[${keycode}]`
}

const LS_KEY = 'switcher:hotkeys'

const props = defineProps<{
  debounce: number
}>()

const emit = defineEmits<{
  close: []
  'update:debounce': [value: number]
  'hotkeys-changed': [config: HotkeyConfig]
}>()

const config = ref<HotkeyConfig>({
  prevKeycode: null,
  prevLabel: null,
  nextKeycode: null,
  nextLabel: null,
  debounce: props.debounce,
  syncMode: true,
  autofocus: false,
})

const capturingPrev = ref(false)
const capturingNext = ref(false)

onMounted(() => {
  const saved = LS.get(LS_KEY)
  if (saved) {
    config.value = { ...config.value, ...saved }
    emit('update:debounce', config.value.debounce)
    emit('hotkeys-changed', config.value)
  }
})

function save() {
  LS.set(LS_KEY, config.value)
  console.log('[SwitcherSettings] save() → hotkeys-changed', JSON.stringify(config.value))
  emit('hotkeys-changed', config.value)
}

async function startCapturePrev() {
  capturingPrev.value = true
  capturingNext.value = false
  const keycode = await window.switcher!.captureKey()
  config.value.prevKeycode = keycode
  config.value.prevLabel = KEYCODE_LABELS[keycode] ?? `[${keycode}]`
  capturingPrev.value = false
  save()
}

async function startCaptureNext() {
  capturingNext.value = true
  capturingPrev.value = false
  const keycode = await window.switcher!.captureKey()
  config.value.nextKeycode = keycode
  config.value.nextLabel = KEYCODE_LABELS[keycode] ?? `[${keycode}]`
  capturingNext.value = false
  save()
}

function onCapturePrev(e: KeyboardEvent) {
  e.preventDefault()
  if (e.key === 'Escape') {
    capturingPrev.value = false
    return
  }
  config.value.prevKeycode = e.keyCode
  config.value.prevLabel = e.key
  capturingPrev.value = false
  save()
}

function onCaptureNext(e: KeyboardEvent) {
  e.preventDefault()
  if (e.key === 'Escape') {
    capturingNext.value = false
    return
  }
  config.value.nextKeycode = e.keyCode
  config.value.nextLabel = e.key
  capturingNext.value = false
  save()
}

function clearPrev() {
  config.value.prevKeycode = null
  config.value.prevLabel = null
  save()
}

function clearNext() {
  config.value.nextKeycode = null
  config.value.nextLabel = null
  save()
}

function onDebounceChange(e: Event) {
  const val = Number((e.target as HTMLInputElement).value)
  config.value.debounce = val
  emit('update:debounce', val)
  save()
}

function onSyncModeChange(e: Event) {
  config.value.syncMode = (e.target as HTMLInputElement).checked
  save()
}

function onAutofocusChange(e: Event) {
  config.value.autofocus = (e.target as HTMLInputElement).checked
  save()
}
</script>

<template>
  <div class="settings">
    <div class="settings__header">
      <span>Paramètres</span>
      <button class="btn" @click="$emit('close')">
        <span class="mdi mdi-close" />
      </button>
    </div>

    <div class="settings__body">

      <!-- Autofocus -->
      <div class="settings__group">
        <div class="settings__row">
          <span class="settings__label">Autofocus (Auto-turn)</span>
          <label class="settings__toggle settings__toggle--primary">
            <input
              type="checkbox"
              :checked="config.autofocus"
              @change="onAutofocusChange"
            />
            <span class="settings__toggle-track" />
          </label>
        </div>
        <p class="settings__desc">
          Focalise automatiquement la fenêtre du personnage suivant quand le tour actuel se termine. Nécessite <b>tshark</b>.
        </p>
      </div>

      <!-- Debounce -->
      <div class="settings__group">
        <div class="settings__row">
          <span class="settings__label">Debounce (ms)</span>
          <input
            :value="config.debounce"
            type="number"
            min="1"
            max="500"
            class="settings__input"
            @input="onDebounceChange"
          />
        </div>
        <p class="settings__desc">
          Délai minimum entre deux switchs. Une valeur trop basse peut provoquer des doubles switchs involontaires.
        </p>
      </div>

      <!-- Sync mode -->
      <div class="settings__group">
        <div class="settings__row">
          <span class="settings__label">Mode sync (--sync)</span>
          <label class="settings__toggle">
            <input
              type="checkbox"
              :checked="config.syncMode"
              @change="onSyncModeChange"
            />
            <span class="settings__toggle-track" />
          </label>
        </div>
        <p class="settings__desc">
          Attend la confirmation du gestionnaire de fenêtres avant de continuer. Plus fiable mais légèrement plus lent (~60ms).
        </p>
      </div>

      <!-- Touche précédent -->
      <div class="settings__group">
        <div class="settings__row">
          <span class="settings__label">Touche précédent</span>
          <div class="settings__key-row">
            <button
              class="settings__key-btn"
              :class="{ 'settings__key-btn--capturing': capturingPrev }"
              @click="startCapturePrev"
            >
              <span v-if="capturingPrev">Appuyez...</span>
              <span v-else-if="config.prevLabel">{{ keycodeLabel(config.prevKeycode) }}</span>
              <span v-else class="settings__key-empty">Non défini</span>
            </button>
            <button v-if="config.prevKeycode" class="btn btn--danger" @click="clearPrev">
              <span class="mdi mdi-close" />
            </button>
          </div>
        </div>
        <p class="settings__desc">
          Touche globale pour passer à la fenêtre précédente. Fonctionne même si Dofus est au premier plan.
        </p>
      </div>

      <!-- Touche suivant -->
      <div class="settings__group">
        <div class="settings__row">
          <span class="settings__label">Touche suivant</span>
          <div class="settings__key-row">
            <button
              class="settings__key-btn"
              :class="{ 'settings__key-btn--capturing': capturingNext }"
              @click="startCaptureNext"
            >
              <span v-if="capturingNext">Appuyez...</span>
              <span v-else-if="config.nextLabel">{{ keycodeLabel(config.nextKeycode) }}</span>
              <span v-else class="settings__key-empty">Non défini</span>
            </button>
            <button v-if="config.nextKeycode" class="btn btn--danger" @click="clearNext">
              <span class="mdi mdi-close" />
            </button>
          </div>
        </div>
        <p class="settings__desc">
          Touche globale pour passer à la fenêtre suivante. Fonctionne même si Dofus est au premier plan.
        </p>
      </div>

    </div>
  </div>
</template>

<style lang="scss" scoped>
.settings {
  position: absolute;
  inset: 0;
  background: rgba(12, 14, 20, 0.96);
  backdrop-filter: blur(18px);
  z-index: 10;
  display: flex;
  flex-direction: column;
  overflow-y: auto;

  &::-webkit-scrollbar { width: 4px; }
  &::-webkit-scrollbar-track { background: transparent; }
  &::-webkit-scrollbar-thumb {
    background: rgba(255,255,255,0.1);
    border-radius: 2px;
  }

  &__header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 12px 14px 10px;
    border-bottom: 1px solid rgba(255,255,255,0.07);
    font-size: 14px;
    font-weight: 600;
    flex-shrink: 0;
  }

  &__body {
    padding: 12px 14px;
    display: flex;
    flex-direction: column;
    gap: 8px;
  }

  &__group {
    display: flex;
    flex-direction: column;
    gap: 6px;
    padding-bottom: 12px;
    border-bottom: 1px solid rgba(255,255,255,0.05);

    &:last-child {
      border-bottom: none;
    }
  }

  &__row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px;
  }

  &__label {
    font-size: 13px;
    color: rgba(255,255,255,0.8);
  }

  &__desc {
    font-size: 11px;
    color: rgba(255,255,255,0.3);
    line-height: 1.5;
    margin: 0;
    padding: 6px 8px;
    background: rgba(255,255,255,0.03);
    border: 1px solid rgba(255,255,255,0.06);
    border-radius: 6px;
  }

  &__input {
    background: rgba(255,255,255,0.06);
    border: 1px solid rgba(255,255,255,0.1);
    border-radius: 6px;
    color: #fff;
    font-size: 13px;
    padding: 4px 8px;
    width: 70px;
    outline: none;
    text-align: center;

    &:focus {
      border-color: var(--pico-primary-border);
    }
  }

  &__key-row {
    display: flex;
    align-items: center;
    gap: 4px;
  }

  &__key-btn {
    background: rgba(255,255,255,0.06);
    border: 1px solid rgba(255,255,255,0.1);
    border-radius: 6px;
    color: #fff;
    font-size: 12px;
    padding: 4px 10px;
    cursor: pointer;
    min-width: 80px;
    text-align: center;
    transition: border-color 0.15s, background 0.15s;

    &--capturing {
      border-color: var(--pico-primary-border);
      background: rgba(var(--pico-primary-background), 0.15);
      animation: pulse 1s ease-in-out infinite;
    }
  }

  &__key-empty {
    color: rgba(255,255,255,0.25);
    font-style: italic;
  }

  &__toggle {
    position: relative;
    display: inline-flex;
    align-items: center;
    cursor: pointer;

    input {
      opacity: 0;
      width: 0;
      height: 0;
      position: absolute;

      &:checked + .settings__toggle-track {
        background: var(--pico-primary);
      }

      &:checked + .settings__toggle-track::after {
        transform: translateX(16px);
      }
    }

    &-track {
      width: 36px;
      height: 20px;
      background: rgba(255,255,255,0.15);
      border-radius: 10px;
      transition: background 0.2s;
      position: relative;

      &::after {
        content: '';
        position: absolute;
        top: 2px;
        left: 2px;
        width: 16px;
        height: 16px;
        background: #fff;
        border-radius: 50%;
        transition: transform 0.2s;
      }
    }
  }
}

.btn {
  background: none;
  border: none;
  padding: 6px;
  border-radius: 6px;
  cursor: pointer;
  color: rgba(255,255,255,0.55);
  display: flex;
  align-items: center;
  transition: background 0.15s, color 0.15s;
  margin: 0;

  &:hover {
    background: rgba(255,255,255,0.08);
    color: #fff;
  }

  &--danger {
    color: var(--pico-del-color);

    &:hover {
      background: rgba(255,80,80,0.1);
      color: var(--pico-del-color);
    }
  }

  .mdi {
    font-size: 16px;
  }
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.6; }
}
</style>
