import { defineStore } from 'pinia'
import { computed, ref, watch } from 'vue'
import { fetchTranslationLanguages } from '../fetch/translation.fetch'
import type { SourceLanguage, TranslationLanguage } from '../types/translation.types'

// Les dernières langues choisies sont un confort propre à ce navigateur : localStorage suffit.
const PREFERENCES_KEY = 'translation.languages'

function readPreferences(): { source: SourceLanguage; target: string } | null {
  try {
    const raw = localStorage.getItem(PREFERENCES_KEY)
    return raw ? JSON.parse(raw) : null
  } catch {
    return null
  }
}

export const useTranslationStore = defineStore('translation', () => {
  const languages = ref<TranslationLanguage[]>([])
  const loading = ref(false)
  const loaded = ref(false)

  const saved = readPreferences()
  const source = ref<SourceLanguage>(saved?.source ?? 'auto')
  const target = ref<string>(saved?.target ?? 'en')

  // Liste chargée une fois par session : elle ne change qu'avec les modèles du moteur.
  async function ensureLanguages() {
    if (loaded.value || loading.value) return
    loading.value = true
    try {
      languages.value = await fetchTranslationLanguages()
      loaded.value = true
      sanitize()
    } finally {
      loading.value = false
    }
  }

  const byCode = computed(() => new Map(languages.value.map(language => [language.code, language])))

  // Cibles possibles depuis la source choisie ; en détection, toutes les langues connues.
  const targetCodes = computed(() => {
    if (source.value === 'auto') return languages.value.map(language => language.code)
    return (byCode.value.get(source.value)?.targets ?? []).filter(code => code !== source.value)
  })

  // Une préférence enregistrée peut désigner une langue que le moteur ne charge plus.
  function sanitize() {
    if (source.value !== 'auto' && !byCode.value.has(source.value)) source.value = 'auto'
    if (!targetCodes.value.includes(target.value)) {
      target.value = targetCodes.value.find(code => code === 'en') ?? targetCodes.value[0] ?? 'en'
    }
  }

  watch([source, target], () => {
    try {
      localStorage.setItem(PREFERENCES_KEY, JSON.stringify({ source: source.value, target: target.value }))
    } catch {
      // Stockage indisponible (navigation privée) : la préférence n'est simplement pas gardée.
    }
  })

  watch(source, () => {
    if (loaded.value) sanitize()
  })

  return { languages, loading, loaded, source, target, byCode, targetCodes, ensureLanguages }
})
