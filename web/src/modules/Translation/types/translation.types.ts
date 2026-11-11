//* Miroir des vues de l'API (api/Modules/Translation).

export type TranslationLanguage = {
  code: string
  name: string
  // Langues vers lesquelles celle-ci peut être traduite.
  targets: string[]
}

export type DetectedLanguage = {
  language: string
  confidence: number
}

export type TranslationResult = {
  texts: string[]
  source: string | null
  target: string
  // Présent seulement quand la langue source était à détecter.
  detected: DetectedLanguage | null
  characters: number
  durationMs: number
  provider: string
}

// « auto » : détection par le moteur.
export type SourceLanguage = 'auto' | string

// Même limite que l'API (TranslationRequest.MaxCharacters).
export const TRANSLATION_MAX_CHARACTERS = 5000
