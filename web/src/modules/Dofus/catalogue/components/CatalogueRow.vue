<script setup lang="ts">
import { computed, ref, nextTick } from 'vue'
import CatalogueHeader from './CatalogueHeader.vue'
import { useCatalogueStore } from '@/modules/Dofus/catalogue/catalogue.store'
import { useCatalogueGrid } from '@/modules/Dofus/catalogue/composables/useCatalogueGrid'
import { AssetResolution } from '@/modules/Dofus/item/types/assetResolution.enum'
import { getItemImageByResolution } from '@/modules/Dofus/item/utils/itemImageSelector'
import { useItemPrices } from '@/modules/Dofus/almanax/composables/useItemPrices'
import { formatNumber } from '@/utils/formatNumber'
import { useClipboard } from '@/composables/useClipboard'
import { useImagePreview } from '@/composables/useImagePreview'
import toast from '@/services/toast'
import { useMutationItemPrices } from '@/modules/Dofus/item/fetch/item.fetch'
import { useUIStore } from '@/stores/ui.store'
import { Item } from '@/modules/Dofus/item/types/item.types'
import ItemContextTrigger from '@/modules/Dofus/item/components/ItemContextTrigger.vue'
import PriceAgeColor from '@/modules/Dofus/item/components/PriceAgeColor.vue'
import { useAuthStore } from '@/modules/Auth/auth.store'
import { RoleCode } from '@/modules/Auth/types/auth.types'
import { getPriceAgeStatus } from '@/modules/Dofus/item/utils/itemPriceSelector'
import { PriceDisplayMode } from '@/modules/Dofus/preferences/types/priceDisplayMode.enum'

defineOptions({ name: 'CatalogueRow' })

const props = defineProps<{
  item: Item
  depth: number
  maxDepth: number
}>()

/* ========================= STORES ========================= */
const catalogueStore = useCatalogueStore()
const { copy } = useClipboard()
const { open: openImagePreview } = useImagePreview()
const uiStore = useUIStore()
const authStore = useAuthStore()

const canWrite = computed(() =>
  authStore.hasModuleAccess('DOFUS', RoleCode.USER)
)

/* ========================= COPY COLUMNS DEF ========================= */
const copyableKeys = ['id', 'asset_id', 'type', 'name', 'level', 'description']
const handleCellClick = (key: string) => {
  if (!copyableKeys.includes(key)) return
  const value = getCellValue(key)
  const col = visibleColumns.value.find(c => c.key === key)
  copy(value, col?.label)
}

/* ========================= GRID ========================= */
const { gridTemplateColumns, getIndentPx } = useCatalogueGrid()
const visibleColumns = computed(() => catalogueStore.visibleCatalogueColumns)

/* ========================= TREE ========================= */
const openedItems = ref<Set<number>>(new Set())

const isOpen = (id: number) => openedItems.value.has(id)

const toggleItem = async (id: number) => {
  if (openedItems.value.has(id)) {
    openedItems.value.delete(id)
  } else {
    openedItems.value.add(id)
    await catalogueStore.fetchIngredients(id)
  }
}

const canExpand = computed(() => props.item.hasRecipe && props.depth < props.maxDepth - 1)
const shouldShowChildren = computed(() => canExpand.value && isOpen(props.item.id))
const children = computed(() => catalogueStore.getIngredients(props.item.id))

/* ========================= IMAGE ========================= */
const imageUrl = computed(() => {
  const img = getItemImageByResolution(props.item.images ?? [], AssetResolution.X2)
  return img?.url ?? ''
})

/* ========================= PRICES ========================= */
const { get, refreshRecursive } = useItemPrices()
const prices = computed(() => get(props.item.id))

const userPriceStatus = computed(() => prices.value ? getPriceAgeStatus(prices.value, PriceDisplayMode.USER) : null)
const communityPriceStatus = computed(() => prices.value ? getPriceAgeStatus(prices.value, PriceDisplayMode.COMMUNITY) : null)
const lastPriceStatus = computed(() => prices.value ? getPriceAgeStatus(prices.value, PriceDisplayMode.LAST) : null)

/* ========================= INLINE EDIT USER PRICE ========================= */
const isEditingPrice = ref(false)
const editPriceValue = ref('')
const priceInputRef = ref<HTMLInputElement | null>(null)

const startEditPrice = async () => {
  editPriceValue.value = String(prices.value?.userPrice ?? 0)
  isEditingPrice.value = true
  await nextTick()
  const input = Array.isArray(priceInputRef.value)
    ? priceInputRef.value[0]
    : priceInputRef.value
  if (input) {
    input.focus()
    input.select()
  }
}

const savePrice = async () => {
  if (!isEditingPrice.value) return

  const sanitized = editPriceValue.value.replace(/[^0-9]/g, '')
  const newPrice = parseInt(sanitized, 10)

  if (isNaN(newPrice) || newPrice < 0) {
    toast.error('Prix invalide')
    isEditingPrice.value = false
    editPriceValue.value = ''
    return
  }

  const oldPrice = prices.value?.userPrice ?? 0
  if (newPrice === oldPrice) {
    isEditingPrice.value = false
    editPriceValue.value = ''
    return
  }

  try {
    uiStore.setLoading(true)
    await useMutationItemPrices([{ itemId: props.item.id, price: newPrice }])
    await refreshRecursive([props.item.id])

    toast.success('Prix mis à jour')
  } catch (e: any) {
    toast.error(e?.message ?? 'Erreur lors de la mise à jour')
  } finally {
    uiStore.setLoading(false)
    isEditingPrice.value = false
    editPriceValue.value = ''
  }
}

const handlePriceKeydown = (e: KeyboardEvent) => {
  if (e.key === 'Enter') {
    e.preventDefault()
    ;(e.target as HTMLInputElement).blur()
  } else if (e.key === 'Escape') {
    e.preventDefault()
    isEditingPrice.value = false
    editPriceValue.value = ''
  }
}

const handlePriceInput = (e: Event) => {
  const input = e.target as HTMLInputElement
  input.value = input.value.replace(/[^0-9]/g, '')
  editPriceValue.value = input.value
}

/* ========================= CELL RENDERERS ========================= */
const getCellValue = (key: string): string | number => {
  const craftKeys = ['craft_user_price', 'craft_community_price', 'craft_last_price', 'craft_calculated_price']
  if (craftKeys.includes(key) && !props.item.hasRecipe) {
    return '—'
  }

  const renderers: Record<string, () => string | number> = {
    id: () => props.item.id,
    asset_id: () => props.item.assetId,
    type: () => props.item.type.name,
    name: () => props.item.name,
    quantity: () => props.item.quantity ?? '—',
    description: () => props.item.description,
    level: () => props.item.level,
    user_price: () => formatNumber(prices.value?.userPrice ?? 0) + ' ₭',
    community_average_price: () => formatNumber(prices.value?.communityAveragePrice ?? 0) + ' ₭',
    last_updated_price: () => formatNumber(prices.value?.lastUpdatedPrice ?? 0) + ' ₭',
    craft_user_price: () => formatNumber(prices.value?.craftUserPrice ?? 0) + ' ₭',
    craft_community_price: () => formatNumber(prices.value?.craftCommunityPrice ?? 0) + ' ₭',
    craft_last_price: () => formatNumber(prices.value?.craftLastPrice ?? 0) + ' ₭',
    craft_calculated_price: () => formatNumber(prices.value?.craftCalculatedPrice ?? 0) + ' ₭',
  }
  return renderers[key]?.() ?? ''
}

const isRightAligned = (key: string) => key.includes('price') || key.startsWith('craft_')
</script>

<template>
  <!-- ROW -->
  <div
    class="catalogue-card"
    :style="{
      marginLeft: `${getIndentPx(depth)}px`,
      gridTemplateColumns,
    }"
  >
    <!-- CHEVRON -->
    <div class="cell chevron" @click="canExpand && toggleItem(item.id)">
      <i
        v-if="canExpand"
        class="mdi"
        :class="isOpen(item.id) ? 'mdi-chevron-down' : 'mdi-chevron-right'"
      />
    </div>

    <!-- IMAGE -->
    <ItemContextTrigger :item="item">
      <div class="cell image" @click="imageUrl && openImagePreview(imageUrl, item.name)">
        <img v-if="imageUrl" :src="imageUrl" :alt="item.name" />
      </div>
    </ItemContextTrigger>

    <!-- COLONNES DYNAMIQUES -->
    <template v-for="col in visibleColumns" :key="col.key">
      <!-- USER PRICE : éditable -->
      <div
        v-if="col.key === 'user_price'"
        class="cell right editable"
        @click="!isEditingPrice && startEditPrice()"
      >
        <input
          v-if="isEditingPrice && canWrite"
          ref="priceInputRef"
          :value="editPriceValue"
          type="text"
          inputmode="numeric"
          pattern="[0-9]*"
          class="price-input"
          @input="handlePriceInput"
          @blur="savePrice"
          @keydown="handlePriceKeydown"
        />
        <PriceAgeColor v-else :status="userPriceStatus">{{ getCellValue(col.key) }}</PriceAgeColor>
      </div>

      <!-- COMMUNITY PRICE -->
      <div
        v-else-if="col.key === 'community_average_price'"
        class="cell right"
        :title="String(getCellValue(col.key))"
      >
        <PriceAgeColor :status="communityPriceStatus">{{ getCellValue(col.key) }}</PriceAgeColor>
      </div>

      <!-- LAST PRICE -->
      <div
        v-else-if="col.key === 'last_updated_price'"
        class="cell right"
        :title="String(getCellValue(col.key))"
      >
        <PriceAgeColor :status="lastPriceStatus">{{ getCellValue(col.key) }}</PriceAgeColor>
      </div>

      <!-- AUTRES COLONNES -->
      <div
        v-else
        class="cell"
        :class="{
          right: isRightAligned(col.key),
          strong: col.key === 'craft_calculated_price',
          name: col.key === 'name',
          copyable: copyableKeys.includes(col.key),
        }"
        :title="String(getCellValue(col.key))"
        @click="handleCellClick(col.key)"
      >
        {{ getCellValue(col.key) }}
      </div>
    </template>

    <!-- CHECKBOX -->
    <div class="cell checkbox-cell">
      <input
        v-if="item.hasRecipe"
        type="checkbox"
        :checked="catalogueStore.isItemSelected(item.id)"
        @change="catalogueStore.toggleItemSelection(item.id)"
      />
    </div>
  </div>

  <!-- CHILD HEADER -->
  <CatalogueHeader v-if="shouldShowChildren" :depth="depth + 1" />

  <!-- CHILD ROWS -->
  <template v-if="shouldShowChildren">
    <CatalogueRow
      v-for="child in children"
      :key="child.id"
      :item="child"
      :depth="depth + 1"
      :maxDepth="maxDepth"
    />
  </template>
</template>

<style scoped lang="scss">
.catalogue-card {
  display: grid;
  align-items: center;
  column-gap: 0.6rem;
  padding: 0.45rem 0.6rem;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 0.45rem;
}

.catalogue-card:hover {
  box-shadow: inset 0 0 0 2px var(--pico-primary-border);
}

.cell {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  min-width: 0;
}

.chevron {
  text-align: center;
  cursor: pointer;
}

.image img {
  width: 28px;
  height: 28px;
  cursor: pointer;
}

.name {
  font-weight: 600;
}

.right {
  text-align: right;
}

.strong {
  font-weight: 600;
}

.editable {
  cursor: pointer;

  &:hover {
    color: var(--pico-primary);
  }
}

.price-input {
  width: 100%;
  height: 100%;
  padding: 0.3rem 0.3rem;
  margin: 0;
  font-size: inherit;
  line-height: 1;
  text-align: right;
  border: 1px solid var(--pico-primary);
  border-radius: 0.25rem;
  background: var(--pico-background-color);
  color: var(--pico-color);
  outline: none;
  box-sizing: border-box;

  &::-webkit-inner-spin-button,
  &::-webkit-outer-spin-button {
    -webkit-appearance: none;
    margin: 0;
  }

  -moz-appearance: textfield;
}

.checkbox-cell {
  text-align: center;

  input[type="checkbox"] {
    margin: 0;
    cursor: pointer;
  }
}
</style>