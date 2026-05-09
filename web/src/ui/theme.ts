export type Theme = 'light' | 'dark'
export type ThemeMode = Theme | 'auto'

const THEME_MODE_KEY = 'tools-theme-mode'
const DEFAULT_MODE: ThemeMode = 'auto'

function getSystemTheme(): Theme {
  return window.matchMedia('(prefers-color-scheme: dark)').matches
    ? 'dark'
    : 'light'
}

function applyTheme(theme: Theme): void {
  document.documentElement.setAttribute('data-theme', theme)
}

/**
 * Retourne le mode stocké (light / dark / auto)
 */
export function getTheme(): ThemeMode {
  const saved = localStorage.getItem(THEME_MODE_KEY)
  return saved === 'light' || saved === 'dark' || saved === 'auto'
    ? saved
    : DEFAULT_MODE
}

/**
 * Applique le thème en fonction du mode
 */
export function setTheme(mode: ThemeMode): void {
  localStorage.setItem(THEME_MODE_KEY, mode)

  if (mode === 'auto') {
    applyTheme(getSystemTheme())
    return
  }

  applyTheme(mode)
}

/**
 * À appeler UNE FOIS au démarrage de l’app
 * pour gérer les changements système en mode auto
 */
export function initThemeListener(): void {
  const media = window.matchMedia('(prefers-color-scheme: dark)')

  media.addEventListener('change', () => {
    if (getTheme() === 'auto') {
      applyTheme(getSystemTheme())
    }
  })
}
