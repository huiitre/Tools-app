<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useAdminAndroidAppsStore } from '../store/adminAndroidApps.store'
import {
  analyzeAndroidAppImport,
  cancelAndroidAppImport,
  fetchAndroidApps,
} from '../fetch/adminAndroidApps.fetch'
import {
  ANDROID_APK_MAX_BYTES,
  type AndroidApp,
  type AndroidAppImportAnalysis,
} from '../types/adminAndroidApps.types'
import AndroidAppsHeader from '../components/AndroidAppsHeader.vue'
import AndroidAppRow from '../components/AndroidAppRow.vue'
import AndroidAppImportModal from '../components/AndroidAppImportModal.vue'
import AndroidApkDropZone from '../components/AndroidApkDropZone.vue'
import { formatBytes } from '@/utils/formatBytes'
import { ApiException } from '@/services/ApiException'
import toast from '@/services/toast'

// Apps Android distribuées par huihuistore (le store des TV). Seul l'admin importe les APK et
// choisit la version proposée ; les TV lisent le catalogue public.
const store = useAdminAndroidAppsStore()

const uploading = ref(false)
const uploadProgress = ref(0)
const analysis = ref<AndroidAppImportAnalysis | null>(null)

const load = async () => {
  store.loading = true
  try {
    store.apps = await fetchAndroidApps()
  } catch {
    toast.error('Impossible de charger les apps Android')
  } finally {
    store.loading = false
  }
}

const imported = (app: AndroidApp) => {
  store.upsertLocally(app)
  const release = app.releases.find(candidate => candidate.isRecommended)
  toast.success(`${app.label} importée${release ? ` · version ${release.versionName ?? release.versionCode} proposée sur le store` : ''}`)
}

// Premier temps : l'APK monte et l'API le lit. La popup s'ouvre ensuite à chaque fois : rien n'est
// créé sans qu'on ait vu ce qui a été lu et ce qui change par rapport à la version en place.
const onFile = async (file: File) => {
  if (file.size > ANDROID_APK_MAX_BYTES) {
    toast.error(`APK trop lourd : ${formatBytes(file.size)} (${formatBytes(ANDROID_APK_MAX_BYTES)} au plus)`)
    return
  }

  uploading.value = true
  uploadProgress.value = 0
  try {
    analysis.value = await analyzeAndroidAppImport(file, ratio => { uploadProgress.value = ratio })
  } catch (e) {
    toast.error(e instanceof ApiException ? e.message : "Erreur lors de l'import de l'APK")
  } finally {
    uploading.value = false
  }
}

const onConfirmed = (app: AndroidApp) => {
  analysis.value = null
  imported(app)
}

// Popup fermée : le fichier en attente est supprimé tout de suite plutôt qu'à son expiration.
const onCancel = async () => {
  const importId = analysis.value?.importId
  analysis.value = null
  if (importId) await cancelAndroidAppImport(importId).catch(() => undefined)
}

onMounted(load)
</script>

<template>
  <main class="admin-android-apps">
    <AndroidApkDropZone :busy="uploading" :progress="uploadProgress" @file="onFile" />

    <p class="total-hint">{{ store.apps.length }} app{{ store.apps.length > 1 ? 's' : '' }} sur le store</p>

    <template v-if="store.loading">
      <div v-for="i in 4" :key="i" class="skeleton-row" />
    </template>

    <template v-else-if="store.apps.length">
      <AndroidAppsHeader />
      <AndroidAppRow v-for="app in store.apps" :key="app.id" :app="app" />
    </template>

    <div v-else class="empty">
      <i class="mdi mdi-android" />
      Aucune app : importez un APK pour commencer
    </div>

    <AndroidAppImportModal v-if="analysis" :analysis="analysis" @confirmed="onConfirmed" @cancel="onCancel" />
  </main>
</template>

<style scoped lang="scss">
.admin-android-apps {
  padding: 0.75rem;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  font-size: 0.85rem;
}

.total-hint {
  margin: 0.4rem 0 0.1rem;
  color: var(--pico-muted-color);
  font-size: 0.75rem;
}

.skeleton-row {
  height: 38px;
  border-radius: 0.45rem;
  background: linear-gradient(
    90deg,
    var(--pico-card-background-color) 0%,
    var(--pico-muted-border-color) 50%,
    var(--pico-card-background-color) 100%
  );
  background-size: 200% 100%;
  animation: shimmer 1.6s ease-in-out infinite;
}

@keyframes shimmer {
  0%   { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

.empty {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 3rem;
  color: var(--pico-muted-color);
  font-size: 0.9rem;

  i { font-size: 1.25rem; }
}
</style>
