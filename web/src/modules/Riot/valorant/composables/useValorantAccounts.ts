import { ref } from 'vue'
import { useRiotStore, type RiotRegion } from '@/modules/Riot/riot.store'
import { listAccounts, linkAccount, unlinkAccount, renameAccount } from '../fetch/valorantAccounts.fetch'

// État partagé entre tous les composants (nav, vues) : un seul formulaire de liaison à la fois.
const loaded = ref(false)
const loading = ref(false)
const showLinkForm = ref(false)
const linkError = ref<string | null>(null)

export function useValorantAccounts() {
  const riotStore = useRiotStore()

  async function ensureLoaded() {
    if (loaded.value || loading.value) return
    loading.value = true
    try {
      const accounts = await listAccounts()
      riotStore.setAccounts(accounts)
      if (accounts.length && (riotStore.selectedAccountId == null || !accounts.some(a => a.id === riotStore.selectedAccountId))) {
        riotStore.setSelectedAccountId(accounts[0].id)
      }
      loaded.value = true
    } catch {
      riotStore.setAccounts([])
    } finally {
      loading.value = false
    }
  }

  function openLinkForm() {
    linkError.value = null
    showLinkForm.value = true
  }

  function closeLinkForm() {
    showLinkForm.value = false
    linkError.value = null
  }

  async function submitLink(token: string, region: RiotRegion) {
    linkError.value = null
    try {
      const { account } = await linkAccount(token, region)
      riotStore.addAccount(account)
      riotStore.setSelectedAccountId(account.id)
      riotStore.setRegion(region)
      riotStore.clearAccountSession()
      showLinkForm.value = false
    } catch (e: any) {
      linkError.value = e?.response?.data?.message ?? e?.message ?? 'Refresh token invalide ou expiré'
    }
  }

  async function rename(accountId: number, label: string) {
    const updated = await renameAccount(accountId, label)
    riotStore.addAccount(updated)
  }

  async function remove(accountId: number) {
    try {
      await unlinkAccount(accountId)
    } catch {
      // Même si l'appel échoue (déjà supprimé côté serveur), on nettoie l'état local
    }
    riotStore.removeAccount(accountId)
    if (riotStore.selectedAccountId === accountId) {
      riotStore.setSelectedAccountId(riotStore.accounts[0]?.id ?? null)
      riotStore.clearAccountSession()
    }
  }

  return {
    loading,
    showLinkForm,
    linkError,
    ensureLoaded,
    openLinkForm,
    closeLinkForm,
    submitLink,
    rename,
    remove,
  }
}
