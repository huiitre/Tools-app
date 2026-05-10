import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useRiotStore, type RiotRegion } from '@/modules/Riot/riot.store'
import type { ValorantStoreOffer, ValorantStoreBundle, ValorantNightMarket } from '../valorant.types'
import {
  fetchStore,
  refreshToAccessToken,
  isAccessTokenExpired,
} from '../fetch/valorantShop.fetch'
import { fetchStoreHistory, addToStoreHistory } from '../fetch/valorantUserSkins.fetch'

export type View = 'form' | 'loading' | 'shop'
export type AuthMode = 'access' | 'refresh'

export const REGIONS: { value: RiotRegion; label: string }[] = [
  { value: 'eu', label: 'EU — Europe' },
  { value: 'na', label: 'NA — Amérique du Nord' },
  { value: 'ap', label: 'AP — Asie-Pacifique' },
  { value: 'kr', label: 'KR — Corée' },
  { value: 'br', label: 'BR — Brésil' },
  { value: 'latam', label: 'LATAM — Amérique latine' },
]

export function useValorantShop() {
  const riotStore = useRiotStore()

  const view = ref<View>('form')
  const skins = ref<ValorantStoreOffer[]>([])
  const bundles = ref<ValorantStoreBundle[]>([])
  const nightMarket = ref<ValorantNightMarket | null>(null)
  const isRenewing = ref(false)
  const error = ref<string | null>(null)
  const remainingMs = ref(0)
  const bundleNow = ref(Date.now())

  let timerInterval: ReturnType<typeof setInterval> | null = null
  let renewalActive = false

  const formattedTime = computed(() => {
    const total = Math.max(0, remainingMs.value)
    const h = Math.floor(total / 3_600_000)
    const m = Math.floor((total % 3_600_000) / 60_000)
    const s = Math.floor((total % 60_000) / 1_000)
    return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`
  })

  const sleep = (ms: number) => new Promise<void>(resolve => setTimeout(resolve, ms))

  function startTimer(seconds: number) {
    stopTimer()
    const expiresAt = Date.now() + seconds * 1_000
    remainingMs.value = expiresAt - Date.now()
    timerInterval = setInterval(() => {
      const now = Date.now()
      remainingMs.value = Math.max(0, expiresAt - now)
      bundleNow.value = now
      if (remainingMs.value === 0) {
        stopTimer()
        startRenewal()
      }
    }, 1_000)
  }

  function stopTimer() {
    if (timerInterval !== null) {
      clearInterval(timerInterval)
      timerInterval = null
    }
  }

  function stopRenewal() {
    renewalActive = false
    isRenewing.value = false
  }

  async function ensureAccessToken(): Promise<string | null> {
    const current = riotStore.accessToken
    if (current && !isAccessTokenExpired(current)) return current

    try {
      const { accessToken } = await refreshToAccessToken()
      riotStore.setAccessToken(accessToken)
      return accessToken
    } catch {
      // Si le refresh échoue (périmé en base), on déconnecte
      riotStore.clearAll()
      return null
    }
  }

  async function syncShopHistory(resolvedSkins: ValorantStoreOffer[], remainingSeconds: number) {
    if (!resolvedSkins.length) return
    try {
      const expirationMs = Date.now() + (remainingSeconds * 1000)
      const shopDate = new Date(expirationMs - 43200 * 1000).toISOString().split('T')[0]
      
      await addToStoreHistory(resolvedSkins.map(o => o.skin.id), shopDate)
      
      const history = await fetchStoreHistory()
      riotStore.setStoreHistory(history)
    } catch {
      // Silently fail
    }
  }

  async function startRenewal() {
    if (renewalActive) return
    renewalActive = true
    isRenewing.value = true

    let attempts = 0
    while (renewalActive && attempts < 30) {
      await sleep(10_000)
      if (!renewalActive) break
      attempts++

      try {
        const token = await ensureAccessToken()
        const store = await fetchStore(token ?? undefined, riotStore.region)
        
        // On considère que c'est un nouveau shop si les IDs ont changé
        // Mais avec l'API, on peut juste comparer directement les données
        skins.value = store.offers
        bundles.value = store.bundles
        nightMarket.value = store.nightMarket
        
        startTimer(store.remainingSeconds)
        syncShopHistory(store.offers, store.remainingSeconds)
        stopRenewal()
        return
      } catch {
        // retry
      }
    }
    if (renewalActive) stopRenewal()
  }

  async function loadShop() {
    view.value = 'loading'
    error.value = null

    try {
      const token = await ensureAccessToken()
      const store = await fetchStore(token ?? undefined, riotStore.region)

      skins.value = store.offers
      bundles.value = store.bundles
      nightMarket.value = store.nightMarket

      // Sync des skins pour l'ownership/watchlist dans le store Pinia
      const allSkins = [
        ...store.offers.map(o => o.skin),
        ...store.bundles.flatMap(b => b.items.map(i => i.skin)),
        ...(store.nightMarket?.offers.map(o => o.skin) ?? []),
      ]
      riotStore.syncFromSkins(allSkins)

      startTimer(store.remainingSeconds)
      await syncShopHistory(store.offers, store.remainingSeconds)
      view.value = 'shop'
    } catch (e: any) {
      error.value = e?.message ?? 'Erreur lors du chargement de la boutique'
      view.value = 'form'
    }
  }

  async function handleSubmit(token: string, region: RiotRegion, mode: AuthMode) {
    view.value = 'loading'
    error.value = null
    try {
      if (mode === 'refresh') {
        // Liaison initiale via Refresh Token
        const { accessToken } = await refreshToAccessToken(token)
        riotStore.setAccessToken(accessToken)
      } else {
        // Usage direct d'un Access Token (durée 1h, non persistant serveur)
        riotStore.setAccessToken(token)
      }
      
      riotStore.setRegion(region)
      await loadShop()
    } catch (e: any) {
      error.value = e?.message ?? 'Token invalide ou expiré'
      riotStore.clearAll()
      view.value = 'form'
    }
  }

  function reset() {
    stopTimer()
    stopRenewal()
    riotStore.clearAll()
    skins.value = []
    bundles.value = []
    nightMarket.value = null
    error.value = null
    view.value = 'form'
  }

  function currentRegionLabel() {
    return REGIONS.find(r => r.value === riotStore.region)?.label ?? riotStore.region.toUpperCase()
  }

  onMounted(async () => {
    // Tentative de chargement automatique (hydratation silencieuse via session serveur)
    await loadShop()
  })

  onBeforeUnmount(() => {
    stopTimer()
    stopRenewal()
  })

  return {
    view,
    skins,
    bundles,
    nightMarket,
    isRenewing,
    error,
    bundleNow,
    formattedTime,
    handleSubmit,
    reset,
    currentRegionLabel,
  }
}
