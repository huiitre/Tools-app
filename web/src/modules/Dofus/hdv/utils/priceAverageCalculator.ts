export interface SnifferItemInstance {
  itemId: number;
  p1: number;
  p10: number;
  p100: number;
}

/**
 * Calcule le prix moyen à l'unité (x1) basé sur toutes les quantités capturées.
 * Seules les valeurs supérieures à 0 sont prises en compte.
 */
export function calculateAverageUnitPrice(instances: SnifferItemInstance[]): number {
  // 1. On crée une liste de prix unitaires
  const unitPrices = instances.flatMap(ins => {
    const p = [];
    if (ins.p1 > 0) p.push(ins.p1);
    // On divise par 10 ou 100, mais on peut ajouter un multiplicateur 
    // pour ignorer les lots s'ils sont 20% plus chers que le x1 (optionnel)
    if (ins.p10 > 0) p.push(ins.p10 / 10);
    if (ins.p100 > 0) p.push(ins.p100 / 100);
    return p;
  });

  if (unitPrices.length === 0) return 0;

  // 2. On trie du plus petit au plus grand
  unitPrices.sort((a, b) => a - b);

  // 3. AU LIEU DE LA MÉDIANE : On prend le MINIMUM
  // Mais pour éviter de choper une erreur de parsing (genre un prix à 100k alors que c'est 2M)
  // on peut prendre la moyenne des 2 ou 3 premiers prix les moins chers.
  
  const topPrices = unitPrices.slice(0, 3); // On prend les 3 moins chers
  const sum = topPrices.reduce((acc, p) => acc + p, 0);
  
  return Math.round(sum / topPrices.length);
}
