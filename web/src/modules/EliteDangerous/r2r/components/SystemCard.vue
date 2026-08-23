<script setup lang="ts">
import { ref, computed } from 'vue'
import type { R2rSystem } from '../types/r2r.types'
import toast from '@/services/toast'

const props = defineProps<{
  system: R2rSystem
  systemIndex: number
  currentSystemIndex: number
  currentBodiesDone: number[]
  isExobio: boolean
}>()

const emit = defineEmits<{
  (e: 'body-toggle', bodyId: number, checked: boolean): void
  (e: 'system-done'): void
  (e: 'revert-to'): void
}>()

const expanded = ref(true)

const isDone    = computed(() => props.systemIndex < props.currentSystemIndex)
const isCurrent = computed(() => props.systemIndex === props.currentSystemIndex)
const isFuture  = computed(() => props.systemIndex > props.currentSystemIndex)

function isBodyDone(bodyId: number) {
  if (isDone.value) return true
  if (isCurrent.value) return props.currentBodiesDone.includes(bodyId)
  return false
}

function bodyBioValue(body: import('../types/r2r.types').R2rBody): number {
  return body.landmarks?.reduce((a, l) => a + l.value, 0) ?? 0
}

const totalMap = computed(() =>
  props.isExobio
    ? props.system.bodies.reduce((a, b) => a + bodyBioValue(b), 0)
    : props.system.bodies.reduce((a, b) => a + (b.estimated_mapping_value || 0), 0)
)

function subtypeClass(subtype: string) {
  const s = (subtype || '').toLowerCase()
  if (s.includes('water')) return 'type-ww'
  if (s.includes('high metal')) return 'type-hmc'
  return 'type-other'
}

function formatCr(v: number) {
  if (!v) return '—'
  if (v >= 1_000_000) {
    const [int, dec] = (v / 1_000_000).toFixed(2).split('.')
    const intFmt = int.replace(/\B(?=(\d{3})+(?!\d))/g, ' ')
    return `${intFmt}.${dec} MCr`
  }
  if (v >= 1_000) return Math.round(v / 1_000) + ' kCr'
  return v + ' Cr'
}

function formatDist(ls: number) {
  if (ls == null) return '—'
  if (ls >= 1000) return (ls / 1000).toFixed(1) + ' kls'
  return Math.round(ls) + ' ls'
}

async function copy(text: string, e: Event) {
  e.stopPropagation()
  try {
    await navigator.clipboard.writeText(text)
  } catch {
    const ta = document.createElement('textarea')
    ta.value = text
    ta.style.cssText = 'position:fixed;opacity:0'
    document.body.appendChild(ta)
    ta.select()
    document.execCommand('copy')
    document.body.removeChild(ta)
  }
  toast.success(`Copié : ${text}`)
}
</script>

<template>
  <div class="system-card" :class="{ done: isDone, current: isCurrent }">

    <div class="system-header">
      <!-- Checkbox / état -->
      <div class="check-cell" @click.stop>
        <input
          type="checkbox"
          class="sys-check"
          :checked="isDone"
          :title="isDone ? 'Cliquer pour revenir à ce système' : 'Marquer le système comme terminé'"
          @change="isDone ? emit('revert-to') : emit('system-done')"
        />
      </div>

      <span class="sys-index">#{{ systemIndex + 1 }}</span>

      <span
        class="sys-name copyable"
        :title="'Cliquer pour copier : ' + system.name"
        @click.stop="copy(system.name, $event)"
      >{{ system.name }}</span>

      <div class="sys-meta">
        <span class="badge-bodies">{{ system.bodies.length }} corps</span>
        <span class="badge-jumps">{{ system.jumps }} saut{{ system.jumps > 1 ? 's' : '' }}</span>
        <span class="sys-value">{{ formatCr(totalMap) }}</span>
      </div>

      <!-- <span class="chevron" :class="{ open: expanded }">›</span> -->
    </div>

    <div v-if="expanded" class="bodies-wrap">
      <table>
        <thead>
          <tr>
            <th class="col-check" />
            <th>Planète</th>
            <th v-if="!isExobio">Type</th>
            <th>Distance</th>
            <th v-if="!isExobio">Val. Scan</th>
            <th v-if="!isExobio">Val. Map</th>
            <th v-if="isExobio">Val. Bio</th>
            <th v-if="isExobio">Biologiques</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="body in system.bodies"
            :key="body.id64"
            :class="{ 'body-done': isBodyDone(body.id64) }"
          >
            <td class="col-check">
              <input
                type="checkbox"
                class="body-check"
                :checked="isBodyDone(body.id64)"
                :disabled="isFuture || isDone"
                @change="emit('body-toggle', body.id64, ($event.target as HTMLInputElement).checked)"
                @click.stop
              />
            </td>
            <td>
              <span
                class="body-name copyable"
                :title="'Cliquer pour copier : ' + body.name"
                @click="copy(body.name, $event)"
              >{{ body.name }}</span>
            </td>
            <td v-if="!isExobio">
              <span class="subtype-pill" :class="subtypeClass(body.subtype)">{{ body.subtype || '—' }}</span>
              <span v-if="body.is_terraformable" class="terra-badge" title="Terraformable">T</span>
            </td>
            <td class="c-muted">{{ formatDist(body.distance_to_arrival) }}</td>
            <td v-if="!isExobio" class="c-scan">{{ formatCr(body.estimated_scan_value) }}</td>
            <td v-if="!isExobio" class="c-map">{{ formatCr(body.estimated_mapping_value) }}</td>
            <td v-if="isExobio" class="c-bio">{{ formatCr(bodyBioValue(body)) }}</td>
            <td v-if="isExobio" class="c-landmarks">
              <span
                v-for="lm in body.landmarks"
                :key="lm.subtype"
                class="landmark-pill"
              >{{ lm.subtype }} ×{{ lm.count }}</span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

  </div>
</template>

<style lang="scss" scoped>
.system-card {
  // Item d'une colonne flex : sans cela, la carte se laisse comprimer par le conteneur.
  flex-shrink: 0;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: var(--pico-border-radius);
  overflow: hidden;
  transition: border-color 0.15s, opacity 0.15s, box-shadow 0.15s;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);

  &:hover {
    border-color: var(--pico-primary-focus);
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.14);
  }
  &.done  { opacity: 0.45; &:hover { opacity: 1; } }
  &.current {
    border-color: var(--pico-primary);
    box-shadow: 0 0 0 2px color-mix(in srgb, var(--pico-primary) 20%, transparent);
  }
}

/* ── HEADER ─── */
.system-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.55rem 0.9rem;
  cursor: pointer;
  user-select: none;
  background: var(--pico-card-sectioning-background-color);
}

.check-cell {
  width: 18px;
  height: 18px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
}

.sys-check {
  width: 15px;
  height: 15px;
  margin: 0;
  cursor: pointer;
  flex-shrink: 0;
  align-self: center;
}

.done-mark {
  font-size: 0.75rem;
  color: var(--pico-primary);
  font-weight: 700;
  line-height: 1;
}

.future-mark {
  display: inline-block;
  width: 15px;
  height: 15px;
}

.sys-index {
  width: 2.2rem;
  text-align: right;
  font-size: 0.72rem;
  color: var(--pico-muted-color);
  flex-shrink: 0;
}

.sys-name {
  flex: 1;
  font-size: 0.88rem;
  font-weight: 600;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  min-width: 0;
}

.copyable {
  cursor: pointer;
  transition: color 0.15s;
  &:hover { color: var(--pico-primary); }
}

.sys-meta {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  flex-shrink: 0;
}

.badge-bodies {
  font-size: 0.7rem;
  padding: 0.15rem 0.5rem;
  border-radius: 0.75rem;
  background: rgba(62, 166, 224, 0.1);
  color: #3ea6e0;
  border: 1px solid rgba(62, 166, 224, 0.2);
}

.badge-jumps {
  font-size: 0.7rem;
  padding: 0.15rem 0.5rem;
  border-radius: 0.75rem;
  color: var(--pico-muted-color);
  border: 1px solid var(--pico-muted-border-color);
}

.sys-value {
  font-size: 0.82rem;
  font-weight: 700;
  color: var(--pico-primary);
}


.chevron {
  font-size: 0.9rem;
  color: var(--pico-muted-color);
  transition: transform 0.2s;
  flex-shrink: 0;
  &.open { transform: rotate(90deg); }
}

/* ── BODIES ─── */
.bodies-wrap {
  border-top: 1px solid var(--pico-card-border-color);
  overflow-x: auto;

  table {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.82rem;
    margin: 0;
  }

  thead th {
    padding: 0.4rem 0.75rem;
    font-size: 0.68rem;
    font-weight: 600;
    color: var(--pico-muted-color);
    text-transform: uppercase;
    letter-spacing: 0.05em;
    background: var(--pico-background-color);
    border-bottom: 1px solid var(--pico-card-border-color);
    text-align: left;
  }

  tbody tr {
    border-bottom: 1px solid var(--pico-card-border-color);
    transition: background 0.1s;
    &:last-child { border-bottom: none; }
    &:hover { background: var(--pico-card-sectioning-background-color); }

    &.body-done td:not(.col-check) {
      opacity: 0.35;
      text-decoration: line-through;
    }
  }

  td { padding: 0.45rem 0.75rem; vertical-align: middle; }
}

.col-check {
  width: 36px;
  text-align: center;
}

.body-check {
  width: 13px;
  height: 13px;
  margin: 0;
  cursor: pointer;
}

.body-name { font-weight: 500; }

.subtype-pill {
  font-size: 0.68rem;
  padding: 0.1rem 0.45rem;
  border-radius: 0.75rem;
  font-weight: 600;
  white-space: nowrap;

  &.type-ww    { background: rgba(62,166,224,0.1); color: #3ea6e0; border: 1px solid rgba(62,166,224,0.2); }
  &.type-hmc   { background: rgba(200,120,32,0.1); color: #c87820; border: 1px solid rgba(200,120,32,0.2); }
  &.type-other { background: rgba(136,136,204,0.1); color: #9999cc; border: 1px solid rgba(136,136,204,0.2); }
}

.terra-badge {
  margin-left: 0.3rem;
  font-size: 0.62rem;
  padding: 0.1rem 0.3rem;
  border-radius: 0.15rem;
  background: rgba(62,207,142,0.1);
  color: #3ecf8e;
  border: 1px solid rgba(62,207,142,0.2);
  font-weight: 700;
}

.c-muted { color: var(--pico-muted-color); }
.c-scan  { color: var(--pico-muted-color); }
.c-map   { color: var(--pico-primary); font-weight: 600; }
.c-bio   { color: #3ecf8e; font-weight: 600; }

.c-landmarks { min-width: 160px; }

.landmark-pill {
  display: inline-block;
  font-size: 0.68rem;
  padding: 0.1rem 0.45rem;
  border-radius: 0.75rem;
  font-weight: 600;
  white-space: nowrap;
  margin: 0.1rem 0.2rem 0.1rem 0;
  background: rgba(62, 207, 142, 0.1);
  color: #3ecf8e;
  border: 1px solid rgba(62, 207, 142, 0.2);
}
</style>
