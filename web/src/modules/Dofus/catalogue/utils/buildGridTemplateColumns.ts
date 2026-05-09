import type { BuildGridOptions } from '@/modules/Dofus/catalogue/types/catalogue.types'



export function buildGridTemplateColumns({
  columns,
  containerWidth,
  leadingFixed = [],
  trailingFixed = [],
}: BuildGridOptions): string {
  const fixed = [...leadingFixed, ...trailingFixed]
  const fixedTotal = fixed.reduce((a, b) => a + b, 0)

  const visible = columns.filter(c => c.visible)
  if (!visible.length) {
    return [...leadingFixed, ...trailingFixed].map(px => `${px}px`).join(' ')
  }

  /* =========================
     PHASE 1 — minSize
  ========================= */

  const sizes = visible.map(c => c.minSize ?? c.size ?? 80)
  let used = fixedTotal + sizes.reduce((a, b) => a + b, 0)

  /* =========================
     PHASE 2 — COMPRESSION
     (quand minSize déborde)
  ========================= */

  if (used > containerWidth) {
    let overflow = used - containerWidth

    // colonnes autorisées à être compressées
    const compressibles = visible.map((c, i) => ({
      index: i,
      min: Math.max(40, Math.floor((c.minSize ?? 80) * 0.6)),
      weight:
        c.key === 'description' ? 3 :
        c.key === 'name' ? 2 :
        c.key.includes('craft_') ? 1 :
        0,
    })).filter(c => c.weight > 0)

    let safety = 0

    while (overflow > 0 && compressibles.length && safety < 1000) {
      safety++

      const totalWeight = compressibles.reduce((a, b) => a + b.weight, 0)

      for (const c of compressibles) {
        if (overflow <= 0) break

        const share = Math.ceil((c.weight / totalWeight) * overflow)
        const reducible = sizes[c.index] - c.min
        if (reducible <= 0) continue

        const delta = Math.min(share, reducible)
        sizes[c.index] -= delta
        overflow -= delta
      }

      // enlever ceux qui ne peuvent plus réduire
      for (let i = compressibles.length - 1; i >= 0; i--) {
        if (sizes[compressibles[i].index] <= compressibles[i].min) {
          compressibles.splice(i, 1)
        }
      }
    }
  }

  /* =========================
     PHASE 3 — EXPANSION
     (si place restante)
  ========================= */

  used = fixedTotal + sizes.reduce((a, b) => a + b, 0)

  if (used < containerWidth) {
    let extra = containerWidth - used

    const growables = visible.map((c, i) => ({
      index: i,
      max: c.maxSize ?? Infinity,
      weight:
        c.key === 'description' ? 3 :
        c.key === 'name' ? 2 :
        c.key.includes('craft_') ? 1 :
        0,
    })).filter(c => c.weight > 0)

    while (extra > 0 && growables.length) {
      const totalWeight = growables.reduce((a, b) => a + b.weight, 0)
      let distributed = false

      for (const g of growables) {
        if (extra <= 0) break

        const share = Math.ceil((g.weight / totalWeight) * extra)
        const growable = g.max - sizes[g.index]
        if (growable <= 0) continue

        const delta = Math.min(share, growable)
        sizes[g.index] += delta
        extra -= delta
        distributed = true
      }

      if (!distributed) break
    }
  }

  /* =========================
     GRID FINAL
  ========================= */

  return [
    ...leadingFixed.map(px => `${px}px`),
    ...sizes.map(px => `${px}px`),
    ...trailingFixed.map(px => `${px}px`),
  ].join(' ')
}
