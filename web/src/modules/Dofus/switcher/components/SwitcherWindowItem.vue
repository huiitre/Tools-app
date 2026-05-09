<script setup lang="ts">
import { ref } from 'vue'

interface DofusWindow {
  windowId: string
  pid: number
  characterName: string | null
  enabled: boolean
}

const props = defineProps<{
  window: DofusWindow
  isCurrent: boolean
}>()

const emit = defineEmits<{
  focus: [windowId: string]
  toggleEnabled: [windowId: string]
  rename: [windowId: string, name: string]
}>()

const editing = ref(false)
const editingName = ref('')

function startEdit() {
  editing.value = true
  editingName.value = props.window.characterName ?? ''
}

function confirmEdit() {
  if (editingName.value.trim()) {
    emit('rename', props.window.windowId, editingName.value.trim())
  }
  editing.value = false
}

function cancelEdit() {
  editing.value = false
}
</script>

<template>
  <div
    class="item"
    :class="{
      'item--disabled': !window.enabled,
      'item--current': isCurrent
    }"
  >
    <span class="drag-handle mdi mdi-drag-vertical" />

    <div class="item__name">
      <template v-if="editing">
        <input
          v-model="editingName"
          class="item__name-input"
          autofocus
          @keyup.enter="confirmEdit"
          @keyup.escape="cancelEdit"
          @blur="confirmEdit"
        />
      </template>
      <template v-else>
        <span class="item__name-text" @dblclick="startEdit">
          {{ window.characterName ?? window.windowId }}
        </span>
        <button class="btn btn--ghost" @click="startEdit">
          <span class="mdi mdi-pencil-outline" />
        </button>
      </template>
    </div>

    <div class="item__actions">
      <button class="btn" title="Focus" @click="emit('focus', window.windowId)">
        <span class="mdi mdi-cursor-default-click-outline" />
      </button>
      <button
        class="btn"
        :title="window.enabled ? 'Désactiver' : 'Activer'"
        @click="emit('toggleEnabled', window.windowId)"
      >
        <span
          class="mdi"
          :class="window.enabled ? 'mdi-eye-outline' : 'mdi-eye-off-outline'"
          :style="{ color: window.enabled ? 'var(--pico-ins-color)' : 'var(--pico-del-color)' }"
        />
      </button>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.item {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 8px 10px;
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.04);
  border: 2px solid rgba(255, 255, 255, 0.05);
  transition: background 0.15s, opacity 0.15s, border-color 0.15s;

  &:hover {
    background: rgba(255, 255, 255, 0.07);

    .btn--ghost {
      opacity: 1;
    }
  }

  &--disabled {
    opacity: 0.4;
  }

  &--current {
    border-color: var(--pico-primary-border);
  }

  &__name {
    flex: 1;
    display: flex;
    align-items: center;
    gap: 4px;
    min-width: 0;
  }

  &__name-text {
    font-size: 13px;
    font-weight: 500;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    cursor: default;
    flex: 1;
  }

  &__name-input {
    flex: 1;
    background: rgba(255, 255, 255, 0.08);
    border: 1px solid var(--pico-primary-border);
    border-radius: 5px;
    color: #fff;
    font-size: 13px;
    padding: 2px 6px;
    outline: none;
    min-width: 0;
  }

  &__actions {
    display: flex;
    gap: 2px;
    flex-shrink: 0;
  }
}

.btn {
  background: none;
  border: none;
  padding: 6px;
  border-radius: 6px;
  cursor: pointer;
  color: rgba(255, 255, 255, 0.55);
  display: flex;
  align-items: center;
  transition: background 0.15s, color 0.15s;
  margin: 0;

  &:hover {
    background: rgba(255, 255, 255, 0.08);
    color: #fff;
  }

  &--ghost {
    opacity: 0;
    padding: 3px;
    font-size: 13px;
  }

  .mdi {
    font-size: 16px;
  }
}
</style>

<style>
.drag-handle {
  cursor: grab;
  color: rgba(255, 255, 255, 0.2);
  font-size: 18px;
  flex-shrink: 0;
}
.drag-handle:active {
  cursor: grabbing;
}
</style>