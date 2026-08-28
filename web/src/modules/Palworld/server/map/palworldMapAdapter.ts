import palworldMapImg from '@/assets/img/Palworld/palworld_map.png'
import palworldMapWorldTreeImg from '@/assets/img/Palworld/palworld_map_worldtree.png'
import type { GameServerMapAdapter, GameServerMapPosition } from '@/modules/Core/GameServers/map/mapAdapter'
import { fetchGuilds } from '../fetch/palworldServerData.fetch'

// Bornes du monde relevées sur les cartes du jeu. Des bases flottantes en mer sont possibles
// depuis les mises à jour récentes : un point proche du bord n'est pas une erreur de calibration.
const LANDSCAPE_MIN_X = -1099400
const LANDSCAPE_MIN_Y = -724400
const LANDSCAPE_MAX_X = 349400
const LANDSCAPE_MAX_Y = 724400

const WORLD_TREE_MIN_X = 347351.5
const WORLD_TREE_MIN_Y = -818197
const WORLD_TREE_MAX_X = 689148.5
const WORLD_TREE_MAX_Y = -476400

function normalize(
  positionX: number,
  positionY: number,
  minX: number,
  minY: number,
  maxX: number,
  maxY: number,
): { xPercent: number; yPercent: number } | null {
  const nx = (positionX - minX) / (maxX - minX)
  const ny = (positionY - minY) / (maxY - minY)

  if (nx < 0 || nx > 1 || ny < 0 || ny > 1) {
    return null
  }

  // L'axe écran X suit la position Y du monde, l'axe écran Y suit la position X inversée.
  return { xPercent: ny * 100, yPercent: (1 - nx) * 100 }
}

// Le direct et le snapshot n'écrivent pas le même identifiant de guilde : le premier rend
// « 8B721BFB084A4BBC8AF0AEEE4A9BE0A0 », le second « 8b721bfb-084a-4bbc-8af0-aeee4a9be0a0 ».
// L'appariement se fait donc ici, au format que l'API du dashboard renvoie.
function toDirectGuildId(guildId: string): string {
  return guildId.replace(/-/g, '').toUpperCase()
}

export const palworldMapAdapter: GameServerMapAdapter = {
  maps: [
    { id: 'palpagos', label: 'Palpagos Islands', image: palworldMapImg },
    { id: 'worldTree', label: "L'Arbre Monde", image: palworldMapWorldTreeImg },
  ],

  // Palpagos est testée en premier : les deux jeux de bornes ne se recoupent que sur une marge
  // infime, inatteignable en jeu.
  resolve(positionX: number, positionY: number): GameServerMapPosition | null {
    const palpagos = normalize(
      positionX, positionY,
      LANDSCAPE_MIN_X, LANDSCAPE_MIN_Y, LANDSCAPE_MAX_X, LANDSCAPE_MAX_Y,
    )
    if (palpagos) {
      return { mapId: 'palpagos', ...palpagos }
    }

    const worldTree = normalize(
      positionX, positionY,
      WORLD_TREE_MIN_X, WORLD_TREE_MIN_Y, WORLD_TREE_MAX_X, WORLD_TREE_MAX_Y,
    )
    if (worldTree) {
      return { mapId: 'worldTree', ...worldTree }
    }

    return null
  },

  // Les bases restent visibles quand toute la guilde est déconnectée : ses membres ne peuvent
  // alors venir que du snapshot serveur, jamais du direct.
  async loadGroups(): Promise<Record<string, string[]>> {
    const guilds = await fetchGuilds()
    return Object.fromEntries(
      guilds.map(guild => [toDirectGuildId(guild.guildId), guild.players.map(player => player.name)]),
    )
  },
}
