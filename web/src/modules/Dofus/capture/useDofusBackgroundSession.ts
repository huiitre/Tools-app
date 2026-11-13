import { computed, watch } from 'vue'
import { useAuthStore } from '@/modules/Auth/auth.store'
import { RoleCode } from '@/modules/Auth/types/auth.types'
import { useEnv } from '@/composables/useEnv'
import { useOS } from '@/composables/useOS'
import { useDofusStore } from '@/modules/Dofus/dofus.store'
import { useFetchGameVersions } from '@/modules/Dofus/game/fetch/game.fetch'
import { useHdvSnifferStore } from '@/modules/Dofus/hdv/hdvSniffer.store'
import { useCaptureStore } from './capture.store'

//* Tout ce qui tourne en tâche de fond pour le module Dofus sous Electron, quelle que soit la
//* page ouverte : écoute des prix HDV (MITM Retro et capture Dofus 3) et capture tshark.
//* Démarré à la connexion d'un compte qui a le module, arrêté dès qu'il ne l'a plus.
export function useDofusBackgroundSession() {
  //* La fenêtre switcher charge la même application mais n'expose que `window.switcher` :
  //* seule la fenêtre principale pilote la session de fond.
  const { isElectron } = useEnv()
  if (!isElectron || !window.electron) return

  const { isLinux } = useOS()
  const auth = useAuthStore()

  const allowed = computed(() => !!auth.user && auth.hasModuleAccess('DOFUS', RoleCode.USER))

  //* Les prix sont rangés sous le jeu et le serveur sélectionnés : la sélection doit être connue
  //* même si la personne n'a encore ouvert aucune page Dofus.
  const loadGameSelection = async () => {
    const dofus = useDofusStore()
    if (dofus.gameVersions.length > 0) return

    dofus.hydrateFromStorage()
    try {
      const { data } = await useFetchGameVersions()
      dofus.setGameVersions(data)
    } catch (e) {
      console.error('[DofusBackgroundSession] Lecture des versions de jeu impossible', e)
    }
  }

  const start = async () => {
    await loadGameSelection()
    useHdvSnifferStore().setupListener()
    if (isLinux) await useCaptureStore().init()
  }

  const stop = async () => {
    await useCaptureStore().shutdown()
    await window.electron?.closeSwitcher()
  }

  watch(allowed, (now, before) => {
    if (now) start()
    else if (before) stop()
  }, { immediate: true })

  //* Déconnexion du compte Tools, avec ou sans module Dofus : la fenêtre switcher ne reste pas
  //* ouverte sur une session terminée.
  watch(() => auth.user, (user, previous) => {
    if (!user && previous) window.electron?.closeSwitcher()
  })
}
