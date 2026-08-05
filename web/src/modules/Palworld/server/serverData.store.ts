import { defineStore } from 'pinia'
import { fetchServerInventory } from './fetch/palworldServerData.fetch'
import type { PalworldServerInventory, PalworldServerPalInventory } from './types/palworldServerData.types'

const SELECTED_PLAYER_IDS_KEY = 'palworld.server-data.selected-player-ids'

function selectedGuildIds(inventory: PalworldServerInventory | null, playerIds: string[]) {
  if (!inventory) return new Set<string>()
  return new Set(inventory.guilds
    .filter(guild => guild.players.some(player => playerIds.includes(player.playerUid)))
    .map(guild => guild.guildId))
}

function selectedPals(inventory: PalworldServerInventory | null, playerIds: string[]): PalworldServerPalInventory[] {
  if (!inventory || playerIds.length === 0) return []
  const guildIds = selectedGuildIds(inventory, playerIds)
  const baseIds = new Set(inventory.guilds
    .filter(guild => guildIds.has(guild.guildId))
    .flatMap(guild => guild.bases.map(base => base.baseId)))
  return inventory.pals.filter(pal =>
    (pal.ownerPlayerUid !== null && playerIds.includes(pal.ownerPlayerUid))
    || (pal.baseId !== null && baseIds.has(pal.baseId)))
}

function playerPalCount(inventory: PalworldServerInventory | null, playerId: string): number {
  if (!inventory) return 0

  const guild = inventory.guilds.find(guild =>
    guild.players.some(player => player.playerUid === playerId))
  const baseIds = new Set(guild?.bases.map(base => base.baseId) ?? [])

  return inventory.pals.filter(pal =>
    pal.ownerPlayerUid === playerId || (pal.baseId !== null && baseIds.has(pal.baseId))
  ).length
}

export const usePalworldServerDataStore = defineStore('palworld-server-data', {
  state: () => ({
    inventory: null as PalworldServerInventory | null,
    selectedPlayerIds: [] as string[],
    loading: false,
    error: null as string | null,
  }),

  getters: {
    lastSyncedAt: state => state.inventory?.lastSyncedAt ?? null,
    pals: state => state.inventory?.pals ?? [],
    guilds: state => state.inventory?.guilds ?? [],
    palIds: state => new Set(state.inventory?.pals.flatMap(pal => pal.palId === null ? [] : [pal.palId]) ?? []),
    selectedPals: state => selectedPals(state.inventory, state.selectedPlayerIds),
    selectedPalIds(): Set<number> {
      return new Set(this.selectedPals.flatMap(pal => pal.palId === null ? [] : [pal.palId]))
    },
    selectedPalCounts(): Map<number, number> {
      const counts = new Map<number, number>()
      for (const pal of this.selectedPals) {
        if (pal.palId !== null) counts.set(pal.palId, (counts.get(pal.palId) ?? 0) + 1)
      }
      return counts
    },
    playerPalCount: state => (playerId: string) => playerPalCount(state.inventory, playerId),
  },

  actions: {
    async ensureLoaded() {
      if (this.inventory || this.loading) return
      this.loading = true
      this.error = null
      try {
        this.inventory = await fetchServerInventory()
        this.restoreSelectedPlayers()
      } catch {
        this.error = 'Impossible de charger les données du serveur Palworld.'
      } finally {
        this.loading = false
      }
    },

    setSelectedPlayerIds(playerIds: string[]) {
      this.selectedPlayerIds = [...new Set(playerIds)]
      localStorage.setItem(SELECTED_PLAYER_IDS_KEY, JSON.stringify(this.selectedPlayerIds))
    },

    toggleSelectedPlayer(playerId: string) {
      this.setSelectedPlayerIds(this.selectedPlayerIds.includes(playerId)
        ? this.selectedPlayerIds.filter(id => id !== playerId)
        : [...this.selectedPlayerIds, playerId])
    },

    clearSelectedPlayers() {
      this.setSelectedPlayerIds([])
    },

    restoreSelectedPlayers() {
      const raw = localStorage.getItem(SELECTED_PLAYER_IDS_KEY)
      if (!raw) return
      try {
        const ids = JSON.parse(raw)
        if (!Array.isArray(ids)) return
        const knownIds = new Set(this.guilds.flatMap(guild => guild.players.map(player => player.playerUid)))
        this.selectedPlayerIds = ids.filter((id): id is string => typeof id === 'string' && knownIds.has(id))
      } catch {
        localStorage.removeItem(SELECTED_PLAYER_IDS_KEY)
      }
    },
  },
})
