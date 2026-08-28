// Contrat que remplit chaque jeu sachant situer ses joueurs sur une carte. Volontairement
// minuscule : le jeu déclare ses cartes et sait projeter une position brute dessus, rien de plus.
// Le rendu, la colonne latérale et les couleurs de groupe sont communs à tous les jeux.
export interface GameServerMapDefinition {
  id: string
  label: string
  image: string
}

export interface GameServerMapPosition {
  mapId: string
  xPercent: number
  yPercent: number
}

export interface GameServerMapAdapter {
  maps: GameServerMapDefinition[]

  // Retourne null quand la position ne tombe sur aucune carte connue du jeu.
  resolve(positionX: number, positionY: number): GameServerMapPosition | null

  // Optionnelle : les membres de chaque groupe (guilde, tribu…), y compris déconnectés, indexés
  // par l'identifiant que l'API renvoie sur les joueurs et les constructions. Sans elle, la carte
  // ne connaît que les joueurs connectés.
  loadGroups?(): Promise<Record<string, string[]>>
}
