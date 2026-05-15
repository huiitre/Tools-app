<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { useValorantSkinDetail } from '../composables/useValorantSkinDetail'
import type { ValorantSkinLevel } from '../valorant.types'

const { isOpen, skin, close } = useValorantSkinDetail()

const selectedLevelIndex = ref(0)
const selectedChromaIndex = ref(0)
const videoActive = ref(false)
const chromaVideoActive = ref(false)
const chromaFullRenderFailed = ref(false)

watch(skin, () => {
  selectedLevelIndex.value = 0
  selectedChromaIndex.value = 0
  videoActive.value = false
  chromaVideoActive.value = false
  chromaFullRenderFailed.value = false
})

watch(isOpen, (val: boolean) => {
  document.body.style.overflow = val ? 'hidden' : ''
})

const selectedLevel = computed(() => skin.value?.levels?.[selectedLevelIndex.value] ?? null)
const selectedChroma = computed(() => skin.value?.chromas?.[selectedChromaIndex.value] ?? null)

const mainMedia = computed<{ type: 'image' | 'video'; src: string } | null>(() => {
  if (chromaVideoActive.value && selectedChroma.value?.streamedVideoUrl) {
    return { type: 'video', src: selectedChroma.value.streamedVideoUrl }
  }
  if (videoActive.value && selectedLevel.value?.streamedVideoUrl) {
    return { type: 'video', src: selectedLevel.value.streamedVideoUrl }
  }
  const chroma = selectedChroma.value
  if (chroma) {
    const src = (!chromaFullRenderFailed.value && chroma.fullRenderUrl) ? chroma.fullRenderUrl : chroma.displayIconUrl
    if (src) return { type: 'image', src }
  }
  const src = selectedLevel.value?.displayIconUrl ?? skin.value?.iconUrl
  if (src) return { type: 'image', src }
  return null
})

const hasMultipleChromas = computed(() => (skin.value?.chromas?.length ?? 0) > 1)

function selectLevel(i: number) {
  selectedLevelIndex.value = i
  selectedChromaIndex.value = 0
  videoActive.value = false
  chromaVideoActive.value = false
}

function selectChroma(i: number) {
  selectedChromaIndex.value = i
  videoActive.value = false
  chromaVideoActive.value = false
  chromaFullRenderFailed.value = false
}

function toggleVideo() {
  videoActive.value = !videoActive.value
}

function toggleChromaVideo() {
  chromaVideoActive.value = !chromaVideoActive.value
}

function onMainImageError() {
  if (!chromaFullRenderFailed.value && selectedChroma.value?.fullRenderUrl) {
    chromaFullRenderFailed.value = true
  }
}

const LEVEL_BADGES: Record<string, { label: string; color: string }> = {
  SoundEffects:             { label: 'Son',       color: '#3b82f6' },
  Animation:                { label: 'Animation', color: '#eab308' },
  VFX:                      { label: 'VFX',       color: '#8b5cf6' },
  Finisher:                 { label: 'Finisher',  color: '#ef4444' },
  HeartbeatAndRadarRange:   { label: 'Radar',     color: '#06b6d4' },
  KillCounter:              { label: 'Compteur',  color: '#f97316' },
  InspectAndKill:           { label: 'Inspect',   color: '#10b981' },
  TopFrag:                  { label: 'Top Frag',  color: '#f59e0b' },
  RandomizedSpawnAnimation: { label: 'Spawn',     color: '#6366f1' },
}

function getBadge(level: ValorantSkinLevel) {
  if (!level.levelItem) return null
  const key = level.levelItem.replace('EEquippableSkinLevelItem::', '')
  return LEVEL_BADGES[key] ?? { label: key, color: '#94a3b8' }
}

function getBadgeStyle(level: ValorantSkinLevel): Record<string, string> {
  const badge = getBadge(level)
  if (!badge) return {}
  return {
    color: badge.color,
    background: `${badge.color}1a`,
    borderColor: `${badge.color}44`,
  }
}

function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape') close()
}

onMounted(() => document.addEventListener('keydown', onKeydown))
onUnmounted(() => {
  document.removeEventListener('keydown', onKeydown)
  document.body.style.overflow = ''
})
</script>

<template>
  <Teleport to="body">
    <Transition name="modal">
      <div v-if="isOpen && skin" class="overlay" @click.self="close">
        <div class="modal">

          <!-- Header -->
          <div class="modal-header">
            <h2 class="skin-title">{{ skin.name }}</h2>
            <button class="close-btn" @click="close" title="Fermer">
              <i class="mdi mdi-close" />
            </button>
          </div>

          <!-- Body -->
          <div class="modal-body">

            <!-- ── LEFT: media zone ──────────────────────────────── -->
            <div class="media-col">
              <div class="media-zone">
                <Transition name="media" mode="out-in">
                  <video
                    v-if="mainMedia?.type === 'video'"
                    :key="'video-' + mainMedia.src"
                    :src="mainMedia.src"
                    class="media-video"
                    autoplay
                    loop
                    controls
                  />
                  <img
                    v-else-if="mainMedia?.type === 'image'"
                    :key="'img-' + mainMedia.src"
                    :src="mainMedia.src"
                    :alt="skin.name"
                    class="media-image"
                    @error="onMainImageError"
                  />
                  <div v-else key="empty" class="media-empty">
                    <i class="mdi mdi-image-off-outline" />
                  </div>
                </Transition>
              </div>

              <!-- Chroma swatches -->
              <div v-if="hasMultipleChromas" class="chroma-strip">
                <button
                  v-for="(chroma, i) in skin.chromas"
                  :key="chroma.assetId"
                  class="chroma-swatch"
                  :class="{ active: selectedChromaIndex === i }"
                  :title="chroma.name ?? `Variante ${i}`"
                  @click="selectChroma(i)"
                >
                  <img
                    v-if="chroma.displayIconUrl"
                    :src="chroma.displayIconUrl"
                    :alt="chroma.name ?? ''"
                    draggable="false"
                  />
                  <span v-else class="swatch-fallback" />
                </button>
              </div>

              <!-- Active chroma label + video -->
              <div
                v-if="(selectedChromaIndex > 0 && selectedChroma?.name) || selectedChroma?.streamedVideoUrl"
                class="chroma-name"
              >
                <span v-if="selectedChromaIndex > 0 && selectedChroma?.name">
                  <i class="mdi mdi-palette-outline" />
                  {{ selectedChroma.name }}
                </span>
                <button
                  v-if="selectedChroma?.streamedVideoUrl"
                  class="play-toggle"
                  :class="{ active: chromaVideoActive }"
                  :title="chromaVideoActive ? 'Masquer la vidéo' : 'Voir la vidéo'"
                  @click.stop="toggleChromaVideo"
                >
                  <i class="mdi" :class="chromaVideoActive ? 'mdi-stop' : 'mdi-play'" />
                </button>
              </div>
            </div>

            <!-- ── RIGHT: levels sidebar ─────────────────────────── -->
            <div class="sidebar">
              <div v-if="skin.levels.length" class="section">
                <p class="section-label">
                  <i class="mdi mdi-lock-open-variant-outline" />
                  Niveaux de déblocage
                </p>

                <div class="levels-list">
                  <button
                    v-for="(level, i) in skin.levels"
                    :key="level.assetId"
                    class="level-card"
                    :class="{ active: selectedLevelIndex === i && selectedChromaIndex === 0 }"
                    @click="selectLevel(i)"
                  >
                    <!-- Thumbnail -->
                    <div class="level-thumb">
                      <img
                        v-if="level.displayIconUrl"
                        :src="level.displayIconUrl"
                        :alt="`Niveau ${Number(i) + 1}`"
                        draggable="false"
                      />
                      <i v-else class="mdi mdi-image-off-outline placeholder-icon" />
                      <span v-if="level.streamedVideoUrl" class="has-video">
                        <i class="mdi mdi-play-circle" />
                      </span>
                    </div>

                    <!-- Info -->
                    <div class="level-info">
                      <span class="level-num">Niveau {{ Number(i) + 1 }}</span>
                      <span
                        v-if="getBadge(level)"
                        class="level-badge"
                        :style="getBadgeStyle(level)"
                      >{{ getBadge(level)!.label }}</span>
                    </div>

                    <!-- Play toggle (only on selected level) -->
                    <button
                      v-if="level.streamedVideoUrl && selectedLevelIndex === i && selectedChromaIndex === 0"
                      class="play-toggle"
                      :class="{ active: videoActive }"
                      :title="videoActive ? 'Masquer la vidéo' : 'Voir la vidéo'"
                      @click.stop="toggleVideo"
                    >
                      <i class="mdi" :class="videoActive ? 'mdi-stop' : 'mdi-play'" />
                    </button>
                  </button>
                </div>
              </div>

              <div v-if="!skin.levels.length" class="no-content">
                <i class="mdi mdi-information-outline" />
                Aucun niveau disponible
              </div>
            </div>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style lang="scss" scoped>
/* ── Overlay ─────────────────────────────────────────────────────────── */
.overlay {
  position: fixed;
  inset: 0;
  z-index: 9500;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(0, 0, 0, 0.88);
  padding: 1rem;
}

/* ── Modal shell ─────────────────────────────────────────────────────── */
.modal {
  width: 100%;
  max-width: 1300px;
  max-height: 92vh;
  background: #0d0d1e;
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 14px;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  box-shadow: 0 32px 80px rgba(0, 0, 0, 0.7);
}

/* ── Header ──────────────────────────────────────────────────────────── */
.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem 1.25rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.07);
  flex-shrink: 0;
}

.skin-title {
  font-size: 1.1rem;
  font-weight: 700;
  color: #fff;
  margin: 0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.close-btn {
  width: 2rem;
  height: 2rem;
  border-radius: 50%;
  border: none;
  background: rgba(255, 255, 255, 0.06);
  color: rgba(255, 255, 255, 0.5);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  transition: background 0.2s, color 0.2s;
  margin: 0;
  padding: 0;

  i { font-size: 1.1rem; }
  &:hover { background: rgba(255, 255, 255, 0.14); color: #fff; }
}

/* ── Body ────────────────────────────────────────────────────────────── */
.modal-body {
  display: grid;
  grid-template-columns: 1fr 340px;
  flex: 1;
  overflow: hidden;
  min-height: 0;
}

/* ── Media column ────────────────────────────────────────────────────── */
.media-col {
  display: flex;
  flex-direction: column;
  background: #06060f;
  border-right: 1px solid rgba(255, 255, 255, 0.06);
  overflow: hidden;
}

.media-zone {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2rem;
  min-height: 0;
  position: relative;
}

.media-image {
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
  border-radius: 6px;
  transition: transform 0.25s ease;
}

.media-video {
  max-width: 100%;
  max-height: 100%;
  border-radius: 6px;
  background: #000;
}

.media-empty {
  font-size: 3rem;
  color: rgba(255, 255, 255, 0.15);
}

/* ── Chroma strip ────────────────────────────────────────────────────── */
.chroma-strip {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.6rem;
  padding: 0.75rem 1rem;
  border-top: 1px solid rgba(255, 255, 255, 0.06);
  flex-wrap: wrap;
  flex-shrink: 0;
}

.chroma-swatch {
  width: 3.2rem;
  height: 3.2rem;
  border-radius: 6px;
  border: 2px solid rgba(255, 255, 255, 0.15);
  padding: 0;
  margin: 0;
  background: rgba(0, 0, 0, 0.4);
  cursor: pointer;
  overflow: hidden;
  transition: border-color 0.2s, transform 0.2s, box-shadow 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;

  img {
    width: 100%;
    height: 100%;
    object-fit: contain;
    display: block;
  }

  .swatch-fallback {
    width: 1.2rem;
    height: 1.2rem;
    border-radius: 4px;
    background: rgba(255, 255, 255, 0.3);
    display: block;
  }

  &.active {
    border-color: var(--pico-primary);
    box-shadow: 0 0 0 3px color-mix(in srgb, var(--pico-primary) 35%, transparent);
    transform: scale(1.08);
  }

  &:not(.active):hover {
    border-color: rgba(255, 255, 255, 0.45);
    transform: scale(1.05);
  }
}

.chroma-name {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  font-size: 0.72rem;
  color: rgba(255, 255, 255, 0.45);
  padding: 0.35rem 1rem 0.5rem;
  flex-shrink: 0;

  span {
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    i { margin-right: 0.3rem; font-size: 0.8rem; }
  }
}

/* ── Sidebar ─────────────────────────────────────────────────────────── */
.sidebar {
  overflow-y: auto;
  padding: 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  scrollbar-width: thin;
  scrollbar-color: rgba(255,255,255,0.1) transparent;
}

.section-label {
  font-size: 0.65rem;
  font-weight: 700;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: rgba(255, 255, 255, 0.35);
  margin: 0 0 0.6rem;

  i { margin-right: 0.35rem; }
}

/* ── Level cards ─────────────────────────────────────────────────────── */
.levels-list {
  display: flex;
  flex-direction: column;
  gap: 0.45rem;
}

.level-card {
  display: flex;
  align-items: center;
  gap: 0.65rem;
  padding: 0.55rem 0.65rem;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(255, 255, 255, 0.07);
  border-radius: 8px;
  cursor: pointer;
  text-align: left;
  transition: background 0.2s, border-color 0.2s;
  margin: 0;
  width: 100%;
  color: #fff;

  &:hover:not(.active) {
    background: rgba(255, 255, 255, 0.08);
    border-color: rgba(255, 255, 255, 0.15);
  }

  &.active {
    background: color-mix(in srgb, var(--pico-primary) 12%, transparent);
    border-color: color-mix(in srgb, var(--pico-primary) 50%, transparent);
  }
}

.level-thumb {
  width: 2.75rem;
  height: 2.75rem;
  flex-shrink: 0;
  border-radius: 6px;
  background: rgba(0, 0, 0, 0.35);
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  position: relative;

  img {
    width: 100%;
    height: 100%;
    object-fit: contain;
    display: block;
  }

  .placeholder-icon {
    font-size: 1.1rem;
    color: rgba(255, 255, 255, 0.2);
  }

  .has-video {
    position: absolute;
    bottom: 1px;
    right: 1px;
    font-size: 0.7rem;
    color: var(--pico-primary);
    line-height: 1;
  }
}

.level-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  min-width: 0;
}

.level-num {
  font-size: 0.82rem;
  font-weight: 600;
  color: rgba(255, 255, 255, 0.85);
  white-space: nowrap;
}

.level-badge {
  display: inline-block;
  font-size: 0.62rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  padding: 0.1rem 0.4rem;
  border-radius: 4px;
  border: 1px solid;
  width: fit-content;
}

.play-toggle {
  width: 1.8rem;
  height: 1.8rem;
  border-radius: 50%;
  border: 1px solid var(--pico-primary);
  background: color-mix(in srgb, var(--pico-primary) 15%, transparent);
  color: var(--pico-primary);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  padding: 0;
  margin: 0;
  transition: background 0.2s, color 0.2s;

  i { font-size: 0.9rem; }

  &.active {
    background: var(--pico-primary);
    color: var(--pico-primary-inverse);
  }

  &:hover:not(.active) {
    background: color-mix(in srgb, var(--pico-primary) 28%, transparent);
  }
}

.no-content {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.8rem;
  color: rgba(255, 255, 255, 0.3);
  padding: 1rem 0;

  i { font-size: 1rem; }
}

/* ── Transitions ─────────────────────────────────────────────────────── */
.modal-enter-active { transition: opacity 0.25s ease, transform 0.25s cubic-bezier(0.22, 1, 0.36, 1); }
.modal-leave-active { transition: opacity 0.18s ease; }
.modal-enter-from  { opacity: 0; transform: scale(0.96); }
.modal-leave-to    { opacity: 0; }

.media-enter-active { transition: opacity 0.2s ease; }
.media-leave-active { transition: opacity 0.15s ease; }
.media-enter-from, .media-leave-to { opacity: 0; }

/* ── Responsive ──────────────────────────────────────────────────────── */
@media (max-width: 700px) {
  .modal-body {
    grid-template-columns: 1fr;
    grid-template-rows: auto 1fr;
    overflow-y: auto;
  }

  .media-col {
    border-right: none;
    border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  }

  .media-zone { padding: 1.25rem; min-height: 220px; }
  .sidebar { max-height: 320px; }
}
</style>
