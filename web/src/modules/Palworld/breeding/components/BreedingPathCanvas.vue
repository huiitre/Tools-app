<script setup lang="ts">
import { nextTick, onMounted, onUnmounted, ref, watch } from 'vue'

const props = withDefaults(defineProps<{
  resetKey?: string | number | null
}>(), {
  resetKey: null,
})

const viewport = ref<HTMLElement | null>(null)
const canvas = ref<HTMLElement | null>(null)
const scale = ref(1)
const dragging = ref(false)

const minScale = 0.4
const maxScale = 2.2
let dragStart = { x: 0, y: 0, offsetX: 0, offsetY: 0 }
let resizeObserver: ResizeObserver | null = null
let position = { x: 0, y: 0 }
let pendingPosition: { x: number, y: number } | null = null
let animationFrame: number | null = null

function clampScale(value: number) {
  return Math.min(maxScale, Math.max(minScale, value))
}

function applyTransform() {
  if (!canvas.value) return
  canvas.value.style.transform = `translate3d(${Math.round(position.x)}px, ${Math.round(position.y)}px, 0) scale(${scale.value})`
}

function queuePosition(nextPosition: { x: number, y: number }) {
  pendingPosition = nextPosition
  if (animationFrame !== null) return
  animationFrame = requestAnimationFrame(() => {
    if (pendingPosition) position = pendingPosition
    pendingPosition = null
    animationFrame = null
    applyTransform()
  })
}

function resetView() {
  nextTick(() => {
    if (!viewport.value || !canvas.value) return
    scale.value = 1.05
    position = {
      x: (viewport.value.clientWidth - canvas.value.offsetWidth * scale.value) / 2,
      y: Math.max(32, viewport.value.clientHeight * 0.16),
    }
    applyTransform()
  })
}

function zoom(nextScale: number, originX?: number, originY?: number) {
  if (!viewport.value) return
  const previousScale = scale.value
  const resolvedScale = clampScale(nextScale)
  if (resolvedScale === previousScale) return

  const x = originX ?? viewport.value.clientWidth / 2
  const y = originY ?? viewport.value.clientHeight / 2
  const ratio = resolvedScale / previousScale
  position = {
    x: x - (x - position.x) * ratio,
    y: y - (y - position.y) * ratio,
  }
  scale.value = resolvedScale
  applyTransform()
}

function handleWheel(event: WheelEvent) {
  const bounds = viewport.value?.getBoundingClientRect()
  if (!bounds) return
  zoom(scale.value * (event.deltaY < 0 ? 1.12 : 0.88), event.clientX - bounds.left, event.clientY - bounds.top)
}

function startDrag(event: PointerEvent) {
  if (event.button !== 0 || !viewport.value) return
  dragging.value = true
  dragStart = { x: event.clientX, y: event.clientY, offsetX: position.x, offsetY: position.y }
  viewport.value.setPointerCapture(event.pointerId)
}

function moveDrag(event: PointerEvent) {
  if (!dragging.value) return
  queuePosition({
    x: dragStart.offsetX + event.clientX - dragStart.x,
    y: dragStart.offsetY + event.clientY - dragStart.y,
  })
}

function stopDrag(event: PointerEvent) {
  if (!dragging.value || !viewport.value) return
  dragging.value = false
  if (viewport.value.hasPointerCapture(event.pointerId)) viewport.value.releasePointerCapture(event.pointerId)
}

watch(() => props.resetKey, resetView)

onMounted(() => {
  resizeObserver = new ResizeObserver(resetView)
  if (viewport.value) resizeObserver.observe(viewport.value)
  if (canvas.value) resizeObserver.observe(canvas.value)
  resetView()
})

onUnmounted(() => {
  resizeObserver?.disconnect()
  if (animationFrame !== null) cancelAnimationFrame(animationFrame)
})
</script>

<template>
  <div
    ref="viewport"
    class="path-canvas"
    :class="{ dragging }"
    @wheel.prevent="handleWheel"
    @pointerdown="startDrag"
    @pointermove="moveDrag"
    @pointerup="stopDrag"
    @pointercancel="stopDrag"
  >
    <div class="path-canvas-controls" @pointerdown.stop>
      <button type="button" title="Zoom arrière" @click="zoom(scale / 1.2)"><i class="mdi mdi-minus" /></button>
      <button type="button" title="Recentrer l'arbre" @click="resetView"><i class="mdi mdi-crosshairs-gps" /></button>
      <button type="button" title="Zoom avant" @click="zoom(scale * 1.2)"><i class="mdi mdi-plus" /></button>
    </div>

    <div
      ref="canvas"
      class="path-canvas-content"
    >
      <slot />
    </div>
  </div>
</template>

<style lang="scss" scoped>
.path-canvas { position: relative; width: 100%; height: min(72dvh, 760px); overflow: hidden; border: 1px solid var(--pico-card-border-color); border-radius: var(--pico-border-radius); cursor: grab; touch-action: none; user-select: none; }
.path-canvas.dragging { cursor: grabbing; }
.path-canvas-controls { position: absolute; z-index: 2; top: .75rem; right: .75rem; display: flex; overflow: hidden; border: 1px solid var(--pico-card-border-color); border-radius: var(--pico-border-radius); background: var(--pico-card-background-color); box-shadow: var(--pico-card-box-shadow); }
.path-canvas-controls button { display: grid; place-items: center; width: 2rem; height: 2rem; margin: 0; padding: 0; border: 0; border-right: 1px solid var(--pico-card-border-color); border-radius: 0; background: transparent; color: var(--pico-color); }
.path-canvas-controls button:last-child { border-right: 0; }
.path-canvas-controls button:hover { background: var(--pico-primary-background); color: var(--pico-primary-inverse); }
.path-canvas-content { position: absolute; top: 0; left: 0; width: max-content; transform-origin: top left; will-change: transform; }
</style>
