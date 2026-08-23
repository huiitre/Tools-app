const UNITS = ['o', 'Ko', 'Mo', 'Go', 'To']

export function formatBytes(bytes: number, locale = 'fr-FR'): string {
  if (bytes <= 0) return '0 o'
  const exponent = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), UNITS.length - 1)
  const value = bytes / 1024 ** exponent
  return `${value.toLocaleString(locale, { maximumFractionDigits: exponent === 0 ? 0 : 1 })} ${UNITS[exponent]}`
}
