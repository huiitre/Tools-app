// Le moteur nomme les langues en anglais ; on les affiche en français (« fr » → « Français »).
const displayNames = (() => {
  try {
    return new Intl.DisplayNames(['fr'], { type: 'language' })
  } catch {
    return null
  }
})()

export function languageName(code: string, fallback?: string): string {
  const name = displayNames?.of(code) ?? fallback ?? code
  return name.charAt(0).toLocaleUpperCase('fr') + name.slice(1)
}
