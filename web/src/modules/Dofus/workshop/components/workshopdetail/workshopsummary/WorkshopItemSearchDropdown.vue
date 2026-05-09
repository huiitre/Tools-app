<script setup lang="ts">
import { useWorkshopDetailStore } from '@/modules/Dofus/workshop/store/workshopDetail.store'
import { useAddItemsToWorkshop } from '@/modules/Dofus/workshop/fetch/workshopItem.fetch'
import { storeToRefs } from 'pinia'
import { ref, watch, onMounted, onUnmounted } from 'vue'
import type { Item } from '@/modules/Dofus/item/types/item.types'
import { useSearchCraftableItems } from '@/modules/Dofus/workshop/fetch/workshopItem.fetch'
import { getItemImageByResolution } from '@/modules/Dofus/item/utils/itemImageSelector'
import { AssetResolution } from '@/modules/Dofus/item/types/assetResolution.enum'

const workshopDetailStore = useWorkshopDetailStore()
const { isOwner, workshopId } = storeToRefs(workshopDetailStore)

const searchQuery = ref('')
const items = ref<Item[]>([])
let timeout: ReturnType<typeof setTimeout> | null = null

const isOpen = ref(false)
const searchInput = ref<HTMLInputElement | null>(null)
const dropdownRef = ref<HTMLElement | null>(null)

async function performSearch(query: string) {
  if (!workshopId.value) return
  
  try {
    items.value = await useSearchCraftableItems(workshopId.value, query)
  } catch (error) {
    console.error('Search error:', error)
    items.value = []
  }
}

watch(searchQuery, (value) => {
  if (timeout) clearTimeout(timeout)

  if (!value || value.length < 2 || !workshopId.value) {
    items.value = []
    isOpen.value = false
    return
  }

  timeout = setTimeout(async () => {
    await performSearch(value)
    isOpen.value = items.value.length > 0
  }, 300)
})

async function selectItem(item: Item) {
  if (!workshopId.value) return

  try {
    await workshopDetailStore.addItems([item.id])
    
    if (searchQuery.value.length >= 2) {
      await performSearch(searchQuery.value)
      
      if (items.value.length === 0) {
        isOpen.value = false
        searchQuery.value = ''
      }
    }
  } catch (error) {
    console.error('Add item error:', error)
  }
}

function handleClickOutside(event: MouseEvent) {
  const target = event.target as Node
  if (
    dropdownRef.value && 
    !dropdownRef.value.contains(target) && 
    searchInput.value && 
    !searchInput.value.contains(target)
  ) {
    isOpen.value = false
  }
}

function getItemImage(item: Item): string {
  return getItemImageByResolution(item.images, AssetResolution.X2)?.url || ''
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
})
</script>

<template>
  <div class="search-wrapper">
    <input
      ref="searchInput"
      type="search"
      v-model="searchQuery"
      @focus="isOpen = searchQuery.length >= 2 && items.length > 0"
      placeholder="Rechercher ou ajouter..."
      class="search-input"
      :disabled="!isOwner"
    >

    <div 
      v-if="isOpen && items.length > 0"
      ref="dropdownRef"
      class="search-dropdown"
    >
      <div
        v-for="item in items"
        :key="item.id"
        class="search-item"
        @click="selectItem(item)"
      >
        <img :src="getItemImage(item)" :alt="item.name" />
        <div class="item-info">
          <div class="item-name">{{ item.name }}</div>
          <div class="item-meta">{{ item.type.name }} - Niv. {{ item.level }}</div>
        </div>
      </div>
    </div>

    <div 
      v-if="isOpen && searchQuery.length >= 2 && items.length === 0"
      ref="dropdownRef"
      class="search-dropdown"
    >
      <div class="search-empty">Aucun résultat</div>
    </div>
  </div>
</template>

<style scoped>
.search-wrapper {
  position: relative;
  flex: 1;
}

.search-input {
  width: 100%;
  margin: 0;
  padding: 0.35rem 0.6rem;
  font-size: 0.8rem;
  height: auto;
}

.search-dropdown {
  position: absolute;
  top: calc(100% + 0.25rem);
  left: 0;
  right: 0;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  max-height: 320px;
  overflow-y: auto;
  z-index: 100;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.3);
}

.search-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.5rem 0.75rem;
  cursor: pointer;
  transition: background 0.15s;
  border-bottom: 1px solid var(--pico-muted-border-color);
}

.search-item:last-child {
  border-bottom: none;
}

.search-item:hover {
  background: var(--pico-card-sectioning-background-color);
}

.search-item img {
  width: 32px;
  height: 32px;
  object-fit: contain;
  flex-shrink: 0;
}

.item-info {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  min-width: 0;
  flex: 1;
}

.item-name {
  font-size: 0.8rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.item-meta {
  font-size: 0.7rem;
  color: var(--pico-muted-color);
}

.search-empty {
  padding: 0.75rem;
  text-align: center;
  font-size: 0.8rem;
  color: var(--pico-muted-color);
}

.search-dropdown::-webkit-scrollbar {
  width: 6px;
}

.search-dropdown::-webkit-scrollbar-track {
  background: var(--pico-card-background-color);
}

.search-dropdown::-webkit-scrollbar-thumb {
  background: var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
}

.search-dropdown::-webkit-scrollbar-thumb:hover {
  background: var(--pico-muted-color);
}
</style>