export function formatNumber(
  value: number,
  locale = 'fr-FR'
): string {
  return value.toLocaleString(locale)
}

export function normalizePositiveIntegerInput(value: string): number {
  const cleaned = value.replace(/\D+/g, '')
  return cleaned === '' ? 0 : parseInt(cleaned, 10)
}
