//* Module Traduction, servi par l'API Core.
import { clientCore } from '@/services/axiosInstance'
import type { TranslationLanguage, TranslationResult } from '../types/translation.types'

export async function fetchTranslationLanguages(): Promise<TranslationLanguage[]> {
  const { data } = await clientCore.get<TranslationLanguage[]>('/translation/languages')
  return data
}

// `signal` : une frappe plus récente annule la traduction en cours.
export async function translateTexts(
  texts: string[],
  source: string | null,
  target: string,
  signal?: AbortSignal,
): Promise<TranslationResult> {
  const { data } = await clientCore.post<TranslationResult>(
    '/translation/translate',
    { texts, source, target },
    { signal },
  )
  return data
}
