<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, computed } from 'vue';
import { useFloating, offset } from '@floating-ui/vue';
import { useFetchItemTypes } from '@/modules/Dofus/item/fetch/item.fetch';
import type { ItemType } from '@/modules/Dofus/item/types/item.types';

const props = defineProps<{
  modelValue: number[];
}>();

const emit = defineEmits(['update:modelValue']);

const isOpen = ref(false);
const reference = ref<HTMLElement | null>(null);
const floating = ref<HTMLElement | null>(null);
const itemTypes = ref<ItemType[]>([]);
const isLoading = ref(false);
const typeSearchQuery = ref('');

const { floatingStyles } = useFloating(reference, floating, {
  placement: 'bottom-start',
  middleware: [offset(5)],
});

const loadItemTypes = async () => {
  if (itemTypes.value.length > 0) return;
  isLoading.value = true;
  try {
    const { data } = await useFetchItemTypes();
    itemTypes.value = data.sort((a, b) => a.name.localeCompare(b.name));
  } finally {
    isLoading.value = false;
  }
};

const toggleOpen = () => {
  isOpen.value = !isOpen.value;
  if (isOpen.value) loadItemTypes();
};

const onClickOutside = (e: MouseEvent) => {
  const target = e.target as HTMLElement;
  if (reference.value && floating.value && !reference.value.contains(target) && !floating.value.contains(target)) {
    isOpen.value = false;
  }
};

const filteredTypes = computed(() => {
  if (!typeSearchQuery.value.trim()) return itemTypes.value;
  const q = typeSearchQuery.value.toLowerCase();
  return itemTypes.value.filter(t => t.name.toLowerCase().includes(q));
});

const isAllSelected = computed(() => {
  return itemTypes.value.length > 0 && props.modelValue.length === itemTypes.value.length;
});

const toggleAll = () => {
  if (isAllSelected.value) {
    emit('update:modelValue', []);
  } else {
    emit('update:modelValue', itemTypes.value.map(t => t.id));
  }
};

const toggleType = (typeId: number) => {
  const newValue = [...props.modelValue];
  const index = newValue.indexOf(typeId);
  if (index === -1) {
    newValue.push(typeId);
  } else {
    newValue.splice(index, 1);
  }
  emit('update:modelValue', newValue);
};

onMounted(() => {
  document.addEventListener('click', onClickOutside);
});

onBeforeUnmount(() => {
  document.removeEventListener('click', onClickOutside);
});
</script>

<template>
  <div class="bank-type-filter">
    <button
      ref="reference"
      class="filter-btn"
      :class="{ 'has-filters': modelValue.length > 0 && !isAllSelected }"
      @click="toggleOpen"
      title="Filtrer par type"
    >
      <span class="mdi mdi-filter-variant"></span>
      <span v-if="modelValue.length > 0 && !isAllSelected" class="filter-count">{{ modelValue.length }}</span>
    </button>

    <div
      v-if="isOpen"
      ref="floating"
      :style="floatingStyles"
      class="floating-panel"
    >
      <div class="filter-panel">
        <header>
          <strong>Types</strong>
          <button class="outline secondary xsmall-btn" @click="toggleAll">
            {{ isAllSelected ? 'Aucun' : 'Tous' }}
          </button>
        </header>

        <div class="search-type-wrapper">
          <input 
            type="text" 
            v-model="typeSearchQuery" 
            placeholder="Rechercher un type..." 
            class="type-search-input"
          />
          <span 
            v-if="typeSearchQuery" 
            class="mdi mdi-close clear-search" 
            @click="typeSearchQuery = ''"
          ></span>
        </div>

        <div v-if="isLoading" aria-busy="true" class="loader"></div>
        
        <div v-else class="types-list">
          <label v-for="type in filteredTypes" :key="type.id" class="type-item">
            <input 
              type="checkbox" 
              :checked="modelValue.includes(type.id)"
              @change="toggleType(type.id)"
            />
            {{ type.name }}
          </label>
          <p v-if="filteredTypes.length === 0" class="no-result">Aucun type trouvé</p>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.bank-type-filter {
  display: inline-flex;
}

.filter-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2.25rem;
  height: 2.25rem;
  padding: 0;
  margin: 0;
  border-radius: var(--pico-border-radius);
  border: 1px solid transparent;
  background: transparent;
  color: var(--pico-muted-color);
  cursor: pointer;
  transition: all 0.15s ease;
  position: relative;

  &:hover {
    color: var(--pico-primary);
    background-color: var(--pico-card-background-color);
    border-color: var(--pico-muted-border-color);
  }

  .mdi { font-size: 1.2rem; }
  
  &.has-filters {
    color: var(--pico-primary);
  }

  .filter-count {
    position: absolute;
    top: 2px;
    right: 2px;
    font-size: 0.6rem;
    font-weight: 800;
    background: var(--pico-primary);
    color: white;
    padding: 0 4px;
    border-radius: 10px;
    min-width: 14px;
    height: 14px;
    display: flex;
    align-items: center;
    justify-content: center;
  }
}

.floating-panel {
  position: absolute;
  z-index: 1000;
  width: 240px;
  padding: 0.75rem;
  background: var(--pico-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  box-shadow: var(--pico-card-box-shadow);
}

.filter-panel {
  header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.5rem;
    strong { font-size: 0.75rem; text-transform: uppercase; }
  }

  .search-type-wrapper {
    position: relative;
    margin-bottom: 0.75rem;

    .type-search-input {
      height: 28px;
      padding: 0 1.5rem 0 0.5rem;
      font-size: 0.75rem;
      margin: 0;
    }

    .clear-search {
      position: absolute;
      right: 0.4rem;
      top: 50%;
      transform: translateY(-50%);
      font-size: 0.9rem;
      opacity: 0.5;
      cursor: pointer;
      &:hover { opacity: 1; color: var(--pico-primary); }
    }
  }

  .xsmall-btn {
    padding: 0.1rem 0.4rem;
    font-size: 0.65rem;
    margin: 0;
    width: auto;
  }

  .loader { margin: 1rem 0; }

  .types-list {
    max-height: 250px;
    overflow-y: auto;
    display: flex;
    flex-direction: column;
    gap: 0.2rem;

    .type-item {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.75rem;
      margin: 0;
      padding: 0.2rem 0;
      cursor: pointer;

      input { margin: 0; scale: 0.8; }
      &:hover { color: var(--pico-primary); }
    }

    .no-result { font-size: 0.7rem; text-align: center; opacity: 0.5; margin: 1rem 0; }
  }
}
</style>
