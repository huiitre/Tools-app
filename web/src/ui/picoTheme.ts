import { useEnv } from '@/composables/useEnv'

export const PICO_THEMES = [
  'azure',
  'amber',
  'blue',
  'cyan',
  'fuchsia',
  'green',
  'grey',
  'indigo',
  'jade',
  'orange',
  'pink',
  'pumpkin',
  'purple',
  'red',
  'sand',
  'slate',
  'violet',
  'yellow',
  'zinc'
] as const

export type PicoTheme = typeof PICO_THEMES[number]

const PICO_THEME_KEY = 'tools-pico-theme'
const DEFAULT_PICO_THEME: PicoTheme = 'azure'
const LINK_ID = 'pico-theme'

function applyPicoTheme(theme: PicoTheme): void {
  const link = document.getElementById(LINK_ID) as HTMLLinkElement | null
  if (!link) return

  const { isElectron } = useEnv()
  const base = isElectron ? './themes' : '/themes'

  link.href = theme === 'azure'
    ? `${base}/pico.min.css`
    : `${base}/pico.${theme}.min.css`
}

export function getPicoTheme(): PicoTheme {
  const saved = localStorage.getItem(PICO_THEME_KEY)
  return PICO_THEMES.includes(saved as PicoTheme)
    ? (saved as PicoTheme)
    : DEFAULT_PICO_THEME
}

export function setPicoTheme(theme: PicoTheme): void {
  if (!PICO_THEMES.includes(theme)) return

  localStorage.setItem(PICO_THEME_KEY, theme)
  applyPicoTheme(theme)
}

export function initPicoTheme(): void {
  applyPicoTheme(getPicoTheme())
}
