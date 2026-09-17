<script setup lang="ts">
import { nextTick, ref } from 'vue'
import AppLogContextFloating from './AppLogContextFloating.vue'

defineProps<{ title: string; content: string }>()
const visible = ref(false)
const x = ref(0)
const y = ref(0)
const width = ref(0)
const height = ref(0)
const position = (event: MouseEvent) => {
  const offset = 12
  x.value = Math.max(16, Math.min(window.innerWidth - width.value - 16, event.clientX + offset))
  y.value = Math.max(16, Math.min(window.innerHeight - height.value - 16, event.clientY + offset))
}
const enter = async (event: MouseEvent) => {
  visible.value = true
  await nextTick()
  const tooltip = document.querySelector('.app-log-context-floating') as HTMLElement | null
  width.value = tooltip?.offsetWidth ?? 0
  height.value = tooltip?.offsetHeight ?? 0
  position(event)
}
</script>

<template>
  <span class="context-trigger" @mouseenter="enter" @mousemove="position" @mouseleave="visible = false"><slot /></span>
  <Teleport to="body"><AppLogContextFloating :title="title" :content="content" :visible="visible" :x="x" :y="y" /></Teleport>
</template>

<style scoped>.context-trigger { cursor: pointer; }</style>
