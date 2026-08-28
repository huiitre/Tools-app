<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'

export interface MapFrameMarker {
  key: string
  xPercent: number
  yPercent: number
  color: string
  tooltip: string
}

const props = withDefaults(defineProps<{
  imageSrc: string
  imageAlt: string
  players: MapFrameMarker[]
  bases: MapFrameMarker[]
  interactive?: boolean
}>(), { interactive: false })

const canvas = ref<HTMLCanvasElement | null>(null)
const frame = ref<HTMLElement | null>(null)
const hoveredMarker = ref<MapFrameMarker | null>(null)
let image: HTMLImageElement | null = null
let resizeObserver: ResizeObserver | null = null
const scale = ref(1)
const dragging = ref(false)
let position = { x: 0, y: 0 }
let dragStart = { x: 0, y: 0, offsetX: 0, offsetY: 0 }

function applyTransform() {
  if (!canvas.value) return
  canvas.value.style.transform = props.interactive
    ? `translate3d(${Math.round(position.x)}px, ${Math.round(position.y)}px, 0)`
    : ''
}

function resetView() {
  if (!props.interactive || !frame.value || !canvas.value) return
  scale.value = 1
  renderCanvas()
  position = {
    x: (frame.value.clientWidth - canvas.value.offsetWidth) / 2,
    y: (frame.value.clientHeight - canvas.value.offsetHeight) / 2,
  }
  applyTransform()
}

function zoom(nextScale: number, originX?: number, originY?: number) {
  if (!props.interactive || !frame.value) return
  const previousScale = scale.value
  const resolvedScale = Math.min(4, Math.max(0.5, nextScale))
  if (resolvedScale === previousScale) return
  const x = originX ?? frame.value.clientWidth / 2
  const y = originY ?? frame.value.clientHeight / 2
  const ratio = resolvedScale / previousScale
  position = { x: x - (x - position.x) * ratio, y: y - (y - position.y) * ratio }
  scale.value = resolvedScale
  renderCanvas()
  applyTransform()
}

function handleWheel(event: WheelEvent) {
  if (!props.interactive || !frame.value) return
  event.preventDefault()
  const bounds = frame.value.getBoundingClientRect()
  zoom(scale.value * (event.deltaY < 0 ? 1.12 : 0.88), event.clientX - bounds.left, event.clientY - bounds.top)
}

function startDrag(event: PointerEvent) {
  if (!props.interactive || event.button !== 0 || !frame.value) return
  dragging.value = true
  dragStart = { x: event.clientX, y: event.clientY, offsetX: position.x, offsetY: position.y }
  frame.value.setPointerCapture(event.pointerId)
}

function moveDrag(event: PointerEvent) {
  if (!dragging.value) return
  position = {
    x: dragStart.offsetX + event.clientX - dragStart.x,
    y: dragStart.offsetY + event.clientY - dragStart.y,
  }
  applyTransform()
}

function stopDrag(event: PointerEvent) {
  if (!dragging.value || !frame.value) return
  dragging.value = false
  if (frame.value.hasPointerCapture(event.pointerId)) frame.value.releasePointerCapture(event.pointerId)
}

function resizeCanvas() {
  const element = canvas.value
  if (!element || !frame.value || !image?.naturalWidth || !image.naturalHeight) return
  if (props.interactive) {
    scale.value = 1
    renderCanvas()
    position = {
      x: (frame.value.clientWidth - element.offsetWidth) / 2,
      y: (frame.value.clientHeight - element.offsetHeight) / 2,
    }
    applyTransform()
  } else {
    renderCanvas()
  }
}

function renderCanvas() {
  const element = canvas.value
  if (!element || !frame.value || !image?.naturalWidth || !image.naturalHeight) return
  if (props.interactive) {
    const baseWidth = Math.min(frame.value.clientWidth, image.naturalWidth)
    element.style.width = `${baseWidth * scale.value}px`
  }
  const width = element.clientWidth
  const height = width * image.naturalHeight / image.naturalWidth
  const ratio = window.devicePixelRatio || 1
  element.width = Math.round(width * ratio)
  element.height = Math.round(height * ratio)
  element.style.aspectRatio = `${image.naturalWidth} / ${image.naturalHeight}`
  draw()
}

function draw() {
  const element = canvas.value
  if (!element || !image?.complete) return
  const context = element.getContext('2d')
  if (!context) return
  const ratio = window.devicePixelRatio || 1
  const width = element.width / ratio
  const height = element.height / ratio
  context.setTransform(ratio, 0, 0, ratio, 0, 0)
  context.imageSmoothingEnabled = true
  context.imageSmoothingQuality = 'high'
  context.clearRect(0, 0, width, height)
  context.drawImage(image, 0, 0, width, height)

  for (const marker of props.bases) drawBase(context, marker, width, height)
  for (const marker of props.players) drawPlayer(context, marker, width, height)

  if (hoveredMarker.value) drawTooltip(context, hoveredMarker.value, width, height)
}

function drawBase(context: CanvasRenderingContext2D, marker: MapFrameMarker, width: number, height: number) {
  const x = marker.xPercent * width / 100
  const y = marker.yPercent * height / 100
  context.save()
  context.fillStyle = marker.color
  context.strokeStyle = 'white'
  context.lineWidth = 2
  context.beginPath()
  context.moveTo(x, y - 10)
  context.lineTo(x + 9, y - 2)
  context.lineTo(x + 7, y + 9)
  context.lineTo(x - 7, y + 9)
  context.lineTo(x - 9, y - 2)
  context.closePath()
  context.fill()
  context.stroke()
  context.fillStyle = 'white'
  context.fillRect(x - 3, y + 1, 6, 6)
  context.restore()
}

function drawPlayer(context: CanvasRenderingContext2D, marker: MapFrameMarker, width: number, height: number) {
  const x = marker.xPercent * width / 100
  const y = marker.yPercent * height / 100
  context.save()
  context.fillStyle = marker.color
  context.strokeStyle = 'white'
  context.lineWidth = 2
  context.beginPath()
  context.arc(x, y, 6, 0, Math.PI * 2)
  context.fill()
  context.stroke()
  context.restore()
}

function drawTooltip(context: CanvasRenderingContext2D, marker: MapFrameMarker, width: number, height: number) {
  const lines = marker.tooltip.split('\n')
  const x = Math.min(marker.xPercent * width / 100 + 12, width - 190)
  const y = Math.max(marker.yPercent * height / 100 - 12, 24)
  context.save()
  context.font = '12px sans-serif'
  const boxWidth = Math.min(190, Math.max(...lines.map(line => context.measureText(line).width)) + 16)
  const boxHeight = lines.length * 16 + 12
  context.fillStyle = 'rgba(15, 23, 42, 0.92)'
  context.beginPath()
  context.roundRect(x, y - boxHeight, boxWidth, boxHeight, 5)
  context.fill()
  context.fillStyle = 'white'
  lines.forEach((line, index) => context.fillText(line, x + 8, y - boxHeight + 17 + index * 16))
  context.restore()
}

function markerAt(event: MouseEvent): MapFrameMarker | null {
  const element = canvas.value
  if (!element) return null
  const bounds = element.getBoundingClientRect()
  const x = (event.clientX - bounds.left) / bounds.width * 100
  const y = (event.clientY - bounds.top) / bounds.height * 100
  return [...props.players, ...props.bases].find(marker => {
    const dx = (marker.xPercent - x) / 100 * bounds.width
    const dy = (marker.yPercent - y) / 100 * bounds.height
    return Math.sqrt(dx * dx + dy * dy) <= 14
  }) ?? null
}

function handlePointerMove(event: MouseEvent) {
  const next = markerAt(event)
  if (next?.key !== hoveredMarker.value?.key) {
    hoveredMarker.value = next
    draw()
  }
}

function handlePointerLeave() {
  if (hoveredMarker.value) {
    hoveredMarker.value = null
    draw()
  }
}

function loadImage() {
  image = new Image()
  image.onload = resizeCanvas
  image.src = props.imageSrc
}

watch(() => props.imageSrc, loadImage)
watch(() => [props.players, props.bases], draw, { deep: true })

onMounted(async () => {
  await nextTick()
  loadImage()
  resizeObserver = new ResizeObserver(resizeCanvas)
  if (frame.value) resizeObserver.observe(frame.value)
})

onBeforeUnmount(() => resizeObserver?.disconnect())
</script>

<template>
  <div
    ref="frame"
    class="map-frame"
    :class="{ 'map-frame--interactive': interactive, 'map-frame--dragging': dragging }"
    @wheel="handleWheel"
    @pointerdown="startDrag"
    @pointermove="moveDrag"
    @pointerup="stopDrag"
    @pointercancel="stopDrag"
  >
    <div v-if="interactive" class="map-frame-controls" @pointerdown.stop>
      <button type="button" title="Zoom arrière" @click="zoom(scale / 1.2)"><i class="mdi mdi-minus" /></button>
      <button type="button" title="Recentrer la carte" @click="resetView"><i class="mdi mdi-crosshairs-gps" /></button>
      <button type="button" title="Zoom avant" @click="zoom(scale * 1.2)"><i class="mdi mdi-plus" /></button>
    </div>
    <canvas
      ref="canvas"
      class="map-frame-canvas"
      role="img"
      :aria-label="imageAlt"
      @mousemove="handlePointerMove"
      @mouseleave="handlePointerLeave"
    />
  </div>
</template>

<style lang="scss" scoped>
.map-frame {
  position: relative;
  width: 100%;
  border-radius: var(--pico-border-radius);
  overflow: hidden;
  border: 1px solid var(--pico-card-border-color);
}

.map-frame--interactive {
  height: min(78dvh, 900px);
  cursor: grab;
  touch-action: none;
  user-select: none;
  background: var(--pico-background-color);
}

.map-frame--dragging { cursor: grabbing; }

.map-frame-canvas {
  display: block;
  width: 100%;
  height: auto;
  cursor: crosshair;
}

.map-frame--interactive .map-frame-canvas {
  position: absolute;
  top: 0;
  left: 0;
  transform-origin: top left;
  cursor: inherit;
}

.map-frame-controls {
  position: absolute;
  z-index: 2;
  top: 0.75rem;
  right: 0.75rem;
  display: flex;
  overflow: hidden;
  border: 1px solid var(--pico-card-border-color);
  border-radius: var(--pico-border-radius);
  background: var(--pico-card-background-color);
  box-shadow: var(--pico-card-box-shadow);
}

.map-frame-controls button {
  display: grid;
  place-items: center;
  width: 2.2rem;
  height: 2.2rem;
  margin: 0;
  padding: 0;
  border: 0;
  border-right: 1px solid var(--pico-card-border-color);
  border-radius: 0;
  background: transparent;
  color: var(--pico-color);
}

.map-frame-controls button:last-child { border-right: 0; }
.map-frame-controls button:hover { background: var(--pico-primary-background); color: var(--pico-primary-inverse); }
</style>
