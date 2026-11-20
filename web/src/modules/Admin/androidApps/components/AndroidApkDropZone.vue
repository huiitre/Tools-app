<script setup lang="ts">
import { ref } from 'vue'

// Zone d'import : on y glisse un APK, ou on clique pour ouvrir le sélecteur de fichiers.
const props = defineProps<{
  busy: boolean
  // 0 à 1 pendant l'envoi ; 1 quand le fichier est arrivé et que le serveur l'analyse.
  progress: number
}>()

const emit = defineEmits<{ file: [file: File] }>()

const input = ref<HTMLInputElement | null>(null)
const dragging = ref(false)
const rejected = ref(false)

// dragenter/dragleave se déclenchent aussi en survolant les enfants : on compte les entrées pour
// ne pas faire clignoter la zone.
let depth = 0

const isApk = (file: File) => file.name.toLowerCase().endsWith('.apk')

const pick = () => {
  if (!props.busy) input.value?.click()
}

const submit = (file: File | undefined) => {
  if (!file || props.busy) return

  if (!isApk(file)) {
    rejected.value = true
    setTimeout(() => { rejected.value = false }, 1600)
    return
  }

  emit('file', file)
}

const onChange = (event: Event) => {
  const target = event.target as HTMLInputElement
  submit(target.files?.[0])
  target.value = ''
}

const onDragEnter = () => {
  depth++
  dragging.value = true
}

const onDragLeave = () => {
  depth = Math.max(0, depth - 1)
  if (depth === 0) dragging.value = false
}

const onDrop = (event: DragEvent) => {
  depth = 0
  dragging.value = false
  submit(event.dataTransfer?.files?.[0])
}
</script>

<template>
  <div
    class="drop-zone"
    :class="{ dragging, busy, rejected }"
    role="button"
    tabindex="0"
    @click="pick"
    @keydown.enter.prevent="pick"
    @keydown.space.prevent="pick"
    @dragenter.prevent="onDragEnter"
    @dragover.prevent
    @dragleave.prevent="onDragLeave"
    @drop.prevent="onDrop"
  >
    <span class="drop-icon">
      <i class="mdi" :class="busy ? 'mdi-loading mdi-spin' : rejected ? 'mdi-file-alert-outline' : dragging ? 'mdi-tray-arrow-down' : 'mdi-cloud-upload-outline'" />
    </span>

    <template v-if="busy">
      <strong class="drop-title">{{ progress < 1 ? `Envoi de l'APK… ${Math.round(progress * 100)} %` : 'Lecture de l\'APK…' }}</strong>
      <span class="drop-sub">{{ progress < 1 ? 'Ne fermez pas la page' : 'Version, signature et certificat' }}</span>
      <div class="progress">
        <div class="progress-bar" :class="{ indeterminate: progress >= 1 }" :style="{ width: `${Math.round(progress * 100)}%` }" />
      </div>
    </template>

    <template v-else-if="rejected">
      <strong class="drop-title">Ce n'est pas un APK</strong>
      <span class="drop-sub">Seuls les fichiers .apk sont acceptés</span>
    </template>

    <template v-else-if="dragging">
      <strong class="drop-title">Déposez l'APK</strong>
      <span class="drop-sub">Il sera analysé avant tout import</span>
    </template>

    <template v-else>
      <strong class="drop-title">Glissez un APK ici</strong>
      <span class="drop-sub">Il sera analysé avant d'être ajouté au store</span>
      <span class="drop-button"><i class="mdi mdi-folder-open-outline" />Choisir un fichier</span>
      <span class="drop-hint">.apk · 200 Mo au plus</span>
    </template>

    <input ref="input" type="file" accept=".apk,application/vnd.android.package-archive" hidden @change="onChange" />
  </div>
</template>

<style scoped lang="scss">
$danger: #e53e3e;

.drop-zone {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.35rem;
  width: 100%;
  max-width: 560px;
  min-height: 210px;
  margin: 0.5rem auto 1rem;
  padding: 1.75rem 1.5rem;
  border: 1px solid var(--pico-card-border-color);
  border-radius: 16px;
  background:
    radial-gradient(120% 90% at 50% 0%, color-mix(in srgb, var(--pico-primary) 9%, transparent) 0%, transparent 70%),
    var(--pico-card-background-color);
  box-shadow: 0 8px 28px rgba(0, 0, 0, .18);
  text-align: center;
  cursor: pointer;
  outline: none;
  transition: border-color 0.2s, box-shadow 0.2s, transform 0.2s, background-color 0.2s;

  &:hover,
  &:focus-visible {
    border-color: color-mix(in srgb, var(--pico-primary) 55%, transparent);
    box-shadow: 0 10px 34px rgba(0, 0, 0, .24);

    .drop-icon { transform: translateY(-2px); }
    .drop-button { background: var(--pico-primary); color: var(--pico-primary-inverse); }
  }

  &.dragging {
    border-color: var(--pico-primary);
    background-color: color-mix(in srgb, var(--pico-primary) 10%, var(--pico-card-background-color));
    transform: scale(1.01);

    .drop-icon { transform: scale(1.1); }
  }

  &.busy {
    cursor: progress;
    border-color: color-mix(in srgb, var(--pico-primary) 40%, transparent);
    transform: none;
  }

  &.rejected {
    border-color: $danger;
    background-color: color-mix(in srgb, #{$danger} 8%, var(--pico-card-background-color));

    .drop-icon { color: $danger; background: color-mix(in srgb, #{$danger} 14%, transparent); }
  }
}

.drop-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 3.6rem;
  height: 3.6rem;
  margin-bottom: 0.4rem;
  border-radius: 50%;
  background: color-mix(in srgb, var(--pico-primary) 15%, transparent);
  color: var(--pico-primary);
  font-size: 1.9rem;
  transition: transform 0.2s;
}

.drop-title {
  font-size: 1rem;
  color: var(--pico-contrast);
}

.drop-sub {
  font-size: 0.78rem;
  color: var(--pico-muted-color);
}

.drop-button {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  margin-top: 0.75rem;
  padding: 0.4rem 1rem;
  border-radius: 999px;
  border: 1px solid var(--pico-primary);
  color: var(--pico-primary);
  font-size: 0.8rem;
  font-weight: 600;
  transition: background 0.2s, color 0.2s;
}

.drop-hint {
  margin-top: 0.3rem;
  font-size: 0.68rem;
  color: var(--pico-muted-color);
}

.progress {
  width: min(320px, 80%);
  height: 4px;
  margin-top: 0.8rem;
  border-radius: 999px;
  overflow: hidden;
  background: color-mix(in srgb, var(--pico-primary) 15%, transparent);
}

.progress-bar {
  height: 100%;
  border-radius: 999px;
  background: var(--pico-primary);
  transition: width 0.2s ease;

  // Pendant l'analyse côté serveur, la durée est inconnue : une barre qui défile.
  &.indeterminate {
    width: 35% !important;
    animation: slide 1.1s ease-in-out infinite;
  }
}

@keyframes slide {
  0%   { transform: translateX(-100%); }
  100% { transform: translateX(300%); }
}
</style>
