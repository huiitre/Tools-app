// "oeuf" doit matcher "Œuf" : la ligature œ/æ n'est pas décomposée par normalize('NFD')
// (contrairement aux accents), donc il faut la remplacer explicitement par oe/ae.
export function normalizeSearchText(text: string): string {
  return text
    .toLowerCase()
    .replace(/œ/g, 'oe')
    .replace(/æ/g, 'ae')
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
}
