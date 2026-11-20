import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { AndroidApp } from '../types/adminAndroidApps.types'

export const useAdminAndroidAppsStore = defineStore('adminAndroidApps', () => {
  const apps = ref<AndroidApp[]>([])
  const loading = ref(false)

  // L'API renvoie l'app complète après chaque écriture : on remplace la ligne, ou on l'ajoute.
  function upsertLocally(app: AndroidApp) {
    const index = apps.value.findIndex(candidate => candidate.id === app.id)
    if (index === -1) apps.value.push(app)
    else apps.value[index] = app

    apps.value.sort((a, b) => a.displayOrder - b.displayOrder || a.label.localeCompare(b.label, 'fr'))
  }

  function removeLocally(appId: number) {
    apps.value = apps.value.filter(app => app.id !== appId)
  }

  function removeReleaseLocally(appId: number, releaseId: number) {
    const app = apps.value.find(candidate => candidate.id === appId)
    if (app) app.releases = app.releases.filter(release => release.id !== releaseId)
  }

  return { apps, loading, upsertLocally, removeLocally, removeReleaseLocally }
})
