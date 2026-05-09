<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { VueDraggable } from 'vue-draggable-plus'
import SwitcherHeader from '../components/SwitcherHeader.vue'
import SwitcherEmptyState from '../components/SwitcherEmptyState.vue'
import SwitcherSettings from '../components/SwitcherSettings.vue'
import SwitcherWindowItem from '../components/SwitcherWindowItem.vue'
import { useAutofocusMapping } from '../composables/useAutofocusMapping'

interface DofusWindow {
  windowId: string
  pid: number
  characterName: string | null
  enabled: boolean
}

interface HotkeyConfig {
  prevKeycode: number | null
  prevLabel: string | null
  nextKeycode: number | null
  nextLabel: string | null
  debounce: number
  syncMode: boolean
  autofocus: boolean
}

const { mapping, loadMapping } = useAutofocusMapping()
const windows = ref<DofusWindow[]>([])
const scanning = ref(false)
const showSettings = ref(false)
const debounce = ref(1)
const currentWindowId = ref<string | null>(null)

const enabledCount = computed(() => windows.value.filter(w => w.enabled).length)

async function refreshCurrent() {
  currentWindowId.value = await window.switcher!.getCurrent()
}

async function scan() {
  scanning.value = true
  try {
    windows.value = await window.switcher!.scan()
    await refreshCurrent()
  } finally {
    scanning.value = false
  }
}

async function onOrderChange() {
  const ids = windows.value.map(w => w.windowId)
  windows.value = await window.switcher!.setOrder(ids)
}

async function toggleEnabled(windowId: string) {
  const w = windows.value.find(w => w.windowId === windowId)
  if (!w) return
  windows.value = await window.switcher!.setEnabled(windowId, !w.enabled)
}

async function focusWindow(windowId: string) {
  await window.switcher!.focusWindow(windowId)
  await refreshCurrent()
}

async function renameWindow(windowId: string, name: string) {
  windows.value = await window.switcher!.renameWindow(windowId, name)
}

async function onHotkeysChanged(config: HotkeyConfig) {
  window.switcher!.log({ level: 'info', service: 'SwitcherView', message: 'onHotkeysChanged triggered', data: { autofocus: config.autofocus } })
  console.log('[SwitcherView] onHotkeysChanged →', JSON.stringify(config))
  try {
    debounce.value = config.debounce
    await window.switcher!.configureHotkeys({
      prevKeycode: config.prevKeycode,
      nextKeycode: config.nextKeycode,
      debounce: config.debounce,
    })

    if (config.autofocus) {
      window.switcher!.log({ level: 'info', service: 'SwitcherView', message: 'Loading mapping...' })
      await loadMapping()
      window.switcher!.log({ level: 'info', service: 'SwitcherView', message: 'Mapping loaded, calling startAutofocus...', data: { mappingSize: Object.keys(mapping.value).length } })
      
      const result = await window.switcher!.startAutofocus({
        interface: 'any',
        mapping: { ...mapping.value }
      })
      window.switcher!.log({ level: 'info', service: 'SwitcherView', message: 'startAutofocus result received', data: result })
      console.log('[Switcher] startAutofocus result:', result)
    } else {
      await window.switcher!.stopAutofocus()
      window.switcher!.log({ level: 'info', service: 'SwitcherView', message: 'Autofocus stopped' })
    }
  } catch (e: any) {
    window.switcher!.log({ level: 'error', service: 'SwitcherView', message: 'Error in onHotkeysChanged', data: e.message })
    console.error('[Switcher] onHotkeysChanged error:', e)
  }
}

onMounted(async () => {
  await scan()
  await window.switcher!.startHotkeys()
  window.switcher!.onCurrentChanged((windowId: string | null) => {
    currentWindowId.value = windowId
  })
})

onUnmounted(async () => {
  await window.switcher!.stopHotkeys()
  window.switcher!.offCurrentChanged()
})
</script>

<template>
  <div class="switcher">
    <SwitcherHeader
      :enabled-count="enabledCount"
      :total="windows.length"
      :scanning="scanning"
      @scan="scan"
      @toggle-settings="showSettings = !showSettings"
    />

    <Transition name="settings">
      <SwitcherSettings
        v-if="showSettings"
        v-model:debounce="debounce"
        @close="showSettings = false"
        @hotkeys-changed="onHotkeysChanged"
      />
    </Transition>

    <div class="switcher__body">
      <SwitcherEmptyState v-if="!windows.length && !scanning" />

      <VueDraggable
        v-else
        v-model="windows"
        class="switcher__list"
        handle=".drag-handle"
        :animation="180"
        @end="onOrderChange"
      >
        <SwitcherWindowItem
          v-for="w in windows"
          :key="w.windowId"
          :window="w"
          :is-current="w.windowId === currentWindowId"
          @focus="focusWindow"
          @toggle-enabled="toggleEnabled"
          @rename="renameWindow"
        />
      </VueDraggable>
    </div>

    <div class="switcher__footer">
      <span class="switcher__hint">
        <span class="mdi mdi-information-outline" />
        Double-clic pour renommer
      </span>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.switcher {
  height: 100dvh;
  display: flex;
  flex-direction: column;
  background: rgba(12, 14, 20, 0.82);
  backdrop-filter: blur(18px);
  -webkit-backdrop-filter: blur(18px);
  color: #e8eaf0;
  font-family: inherit;
  overflow: hidden;
  position: relative;

  &__body {
    flex: 1;
    overflow-y: auto;
    padding: 8px 0;

    &::-webkit-scrollbar { width: 4px; }
    &::-webkit-scrollbar-track { background: transparent; }
    &::-webkit-scrollbar-thumb {
      background: rgba(255,255,255,0.1);
      border-radius: 2px;
    }
  }

  &__list {
    display: flex;
    flex-direction: column;
    gap: 6px;
    padding: 0 8px;
  }

  &__footer {
    padding: 8px 14px;
    border-top: 1px solid rgba(255,255,255,0.07);
    flex-shrink: 0;
  }

  &__hint {
    font-size: 11px;
    color: rgba(255,255,255,0.25);
    display: flex;
    align-items: center;
    gap: 4px;

    .mdi { font-size: 13px; }
  }
}

.settings-enter-active,
.settings-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}
.settings-enter-from,
.settings-leave-to {
  opacity: 0;
  transform: translateY(6px);
}
</style>
