import type { Item } from '@/modules/Dofus/item/types/item.types'

//* Icône « boss » : exportée par doduda (rangs de guilde), donc conservée à chaque mise à jour du jeu.
//* L'API sert toutes ses images depuis cet hôte, quel que soit l'environnement.
export const BOSS_ICON_URL = 'https://assets.tools.huiitre.fr/tools_dofus/dofus3/img/misc/guildrank/58-28.png'

//* Vrai si l'objet tombe d'au moins un monstre de ses zones de drop, et que ce sont tous des boss.
//* Calculé sur les zones (comme l'infobulle) : l'API écarte des doublons de boss sans zone ni flag.
export function isBossOnlyDrop(item: Pick<Item, 'farmZones'>): boolean {
  const monsters = (item.farmZones ?? []).flatMap(zone => zone.monsters)
  return monsters.length > 0 && monsters.every(monster => monster.boss)
}
