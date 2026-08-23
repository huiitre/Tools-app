import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { R2rExpeditionDetail, R2rExpeditionSummary, R2rRouteData } from '../types/r2r.types'
import * as r2rFetch from '../fetch/r2r.fetch'

export const useR2rStore = defineStore('r2r', () => {
  const expeditions = ref<R2rExpeditionSummary[]>([])
  const currentExpedition = ref<R2rExpeditionDetail | null>(null)
  const loading = ref(false)

  async function fetchExpeditions() {
    loading.value = true
    try {
      const { data } = await r2rFetch.fetchExpeditions()
      expeditions.value = data
    } finally {
      loading.value = false
    }
  }

  async function loadExpedition(id: string) {
    loading.value = true
    try {
      const { data } = await r2rFetch.fetchExpedition(id)
      const parsed: R2rRouteData = JSON.parse(data.routeData)
      currentExpedition.value = { ...data, parsedRouteData: parsed }
    } finally {
      loading.value = false
    }
  }

  function closeExpedition() {
    currentExpedition.value = null
  }

  async function saveProgress(systemIndex: number, bodiesDone: number[]) {
    if (!currentExpedition.value) return
    const id = currentExpedition.value.id
    currentExpedition.value.currentSystemIndex = systemIndex
    currentExpedition.value.currentBodiesDone = bodiesDone
    const summary = expeditions.value.find(e => e.id === id)
    if (summary) summary.currentSystemIndex = systemIndex
    await r2rFetch.updateProgress(id, systemIndex, bodiesDone)
  }

  async function importExpedition(formData: FormData): Promise<string> {
    const { data } = await r2rFetch.importExpedition(formData)
    await fetchExpeditions()
    return data.id
  }

  async function renameExpedition(id: string, name: string) {
    await r2rFetch.renameExpedition(id, name)
    const summary = expeditions.value.find(e => e.id === id)
    if (summary) summary.name = name
    if (currentExpedition.value?.id === id) currentExpedition.value.name = name
  }

  async function exportExpedition(id: string, filename: string) {
    const response = await r2rFetch.exportExpedition(id)
    const url = URL.createObjectURL(new Blob([response.data]))
    const a = document.createElement('a')
    a.href = url
    a.download = filename
    a.click()
    URL.revokeObjectURL(url)
  }

  async function removeExpedition(id: string) {
    await r2rFetch.deleteExpedition(id)
    expeditions.value = expeditions.value.filter(e => e.id !== id)
    if (currentExpedition.value?.id === id) currentExpedition.value = null
  }

  return {
    expeditions,
    currentExpedition,
    loading,
    fetchExpeditions,
    loadExpedition,
    closeExpedition,
    saveProgress,
    importExpedition,
    renameExpedition,
    exportExpedition,
    removeExpedition,
  }
})