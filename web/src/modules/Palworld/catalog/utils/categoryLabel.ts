const CATEGORY_LABELS: Record<string, string> = {
  Blueprint: 'Plan',
  Weapon: 'Arme',
  Armor: 'Armure',
  Essential: 'Essentiel',
  Consume: 'Consommable',
  Material: 'Matériau',
  Accessory: 'Accessoire',
  Food: 'Nourriture',
  Ammo: 'Munition',
  SpecialWeapon: 'Arme spéciale',
  Glider: 'Deltaplane',
  CaptureItemModifier: 'Modificateur de capture',
  MonsterEquipWeapon: 'Arme de monstre',
}

export function categoryLabel(category: string | null): string {
  if (!category) return 'Autre'
  return CATEGORY_LABELS[category] ?? category
}
