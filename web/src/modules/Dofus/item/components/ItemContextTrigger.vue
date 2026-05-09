<script setup lang="ts">
import { ref, nextTick, computed } from 'vue'
import type { Item } from '@/modules/Dofus/item/types/item.types'
import { useDofusConfigStore } from '@/modules/Dofus/preferences/preferences.store'
import { storeToRefs } from 'pinia'
import ItemContextFloating from '@/modules/Dofus/item/components/ItemContextFloating.vue'
import { useItemPrices } from '@/modules/Dofus/almanax/composables/useItemPrices'

const props = defineProps<{
  item?: Item
}>()

/* ========================= CONFIG ========================= */
const dofusConfig = useDofusConfigStore()
const { priceDisplayMode, showItemContextOnHover } =
  storeToRefs(dofusConfig)

/* ========================= PRICE (LECTURE SEULE) ========================= */
const { get } = useItemPrices()
const price = computed(() =>
  props.item ? get(props.item.id) : undefined
)

/* ========================= STATE ========================= */
const visible = ref(false)
const x = ref(0)
const y = ref(0)

const tooltipHeight = ref(0)
const tooltipWidth = ref(0)

/* ========================= POSITION ========================= */

const updatePosition = (e: MouseEvent) => {
  const offset = 12
  const edgeMargin = 16 // marge avant les bords (haut/bas/gauche/droite)

  const viewportHeight = window.innerHeight
  const viewportWidth = window.innerWidth

  const cursorX = e.clientX
  const cursorY = e.clientY

  // =========================
  // VERTICAL
  // =========================
  const yDown = cursorY + offset

  // si en bas on dépasse (avec marge), on passe au-dessus
  if (yDown + tooltipHeight.value > viewportHeight - edgeMargin) {
    const yUp = cursorY - tooltipHeight.value - offset
    // clamp pour ne jamais coller / dépasser en haut
    y.value = Math.max(edgeMargin, yUp)
  } else {
    // clamp pour ne jamais coller / dépasser en bas
    y.value = Math.min(viewportHeight - edgeMargin - tooltipHeight.value, yDown)
  }

  // =========================
  // HORIZONTAL
  // =========================
  const xRight = cursorX + offset

  // si à droite on dépasse (avec marge), on passe à gauche
  if (xRight + tooltipWidth.value > viewportWidth - edgeMargin) {
    const xLeft = cursorX - tooltipWidth.value - offset
    // clamp pour ne jamais coller / dépasser à gauche
    x.value = Math.max(edgeMargin, xLeft)
  } else {
    // clamp pour ne jamais coller / dépasser à droite
    x.value = Math.min(viewportWidth - edgeMargin - tooltipWidth.value, xRight)
  }
}

/* ========================= EVENTS ========================= */
const onEnter = async (e: MouseEvent) => {
  if (!props.item) return
  if (!showItemContextOnHover.value) return

  visible.value = true
  await nextTick()

  const el = document.querySelector(
    '.item-context-floating',
  ) as HTMLElement | null

  tooltipHeight.value = el?.offsetHeight ?? 0
  tooltipWidth.value = el?.offsetWidth ?? 0

  updatePosition(e)
}

const onMove = (e: MouseEvent) => {
  if (!visible.value) return
  updatePosition(e)
}

const onLeave = () => {
  visible.value = false
}
</script>

<template>
  <span
    :class="{ 'hover-enabled': showItemContextOnHover }"
    @mouseenter="onEnter"
    @mousemove="onMove"
    @mouseleave="onLeave"
  >
    <slot />
  </span>

  <Teleport to="body">
    <ItemContextFloating
      v-if="item && showItemContextOnHover"
      :item="item"
      :price="price"
      :visible="visible"
      :x="x"
      :y="y"
      :priceDisplayMode="priceDisplayMode"
    />
  </Teleport>
</template>
