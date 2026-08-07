<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useShopStore } from '../shop.store'
import { useShopCartStore } from '../shopCart.store'
import MerchantList from '../components/MerchantList.vue'
import MerchantOffers from '../components/MerchantOffers.vue'
import ShopCartPanel from '../components/ShopCartPanel.vue'

const shopStore = useShopStore()
const cartStore = useShopCartStore()
const selectedId = ref<string | null>(null)

const selectedMerchant = computed(() => shopStore.merchants.find(m => m.externalId === selectedId.value) ?? null)

watch(() => shopStore.merchants, (merchants) => {
  if (!selectedId.value && merchants.length) selectedId.value = merchants[0].externalId
}, { immediate: true })

onMounted(() => {
  shopStore.ensureLoaded()
  shopStore.hydrateFavorites()
  cartStore.hydrate()
})
</script>

<template>
  <div class="shop">
    <div v-if="shopStore.error" class="error-banner">
      <i class="mdi mdi-alert-circle-outline" />
      {{ shopStore.error }}
    </div>

    <div v-else-if="shopStore.loading" class="status">
      <span class="spinner" />
      Chargement des marchands…
    </div>

    <div v-else class="shop-layout">
      <aside class="shop-panel shop-merchants">
        <MerchantList :merchants="shopStore.merchants" :selected-id="selectedId" @select="id => selectedId = id" />
      </aside>

      <section class="shop-panel shop-offers">
        <MerchantOffers
          v-if="selectedMerchant"
          :merchant="selectedMerchant"
          @add-to-cart="(itemSlug, quantity) => cartStore.addToCart(selectedMerchant!.externalId, itemSlug, quantity)"
        />
        <p v-else class="empty">Sélectionnez un marchand.</p>
      </section>

      <aside class="shop-panel shop-cart-panel">
        <ShopCartPanel />
      </aside>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.shop {
  padding: 1.5rem;
  max-width: 1500px;
  margin: 0 auto;
  width: 100%;
  box-sizing: border-box;
  height: 100%;
  min-height: 0;
  overflow: hidden;
}

@media (max-width: 640px) {
  .shop { padding: 1rem; }
}

.shop-layout {
  display: grid;
  grid-template-columns: 240px 1fr 320px;
  gap: 1rem;
  height: 100%;
  min-height: 0;
}

@media (max-width: 1100px) {
  .shop {
    height: auto;
    overflow: visible;
  }

  .shop-layout {
    grid-template-columns: 1fr;
    height: auto;
  }
}

.shop-panel {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 10px;
  padding: 1rem;
  height: 100%;
  min-height: 0;
  overflow-y: auto;
}

.shop-merchants {
  // Le scroll est géré en interne par MerchantList.vue (barre de recherche fixe + liste scrollable),
  // pas par ce panneau — sinon double scrollbar potentielle.
  overflow: hidden;
}

@media (max-width: 1100px) {
  .shop-panel {
    height: auto;
    max-height: 60vh;
  }
}

.status {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  padding: 3rem 0;
  color: var(--pico-muted-color);
  font-size: 0.9rem;
}

.spinner {
  width: 20px;
  height: 20px;
  border: 2px solid var(--pico-card-border-color);
  border-top-color: var(--pico-primary);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.error-banner {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  border-radius: 8px;
  background: color-mix(in srgb, #e53e3e 10%, transparent);
  border: 1px solid color-mix(in srgb, #e53e3e 25%, transparent);
  color: #e53e3e;
  font-size: 0.875rem;
}

.empty {
  text-align: center;
  color: var(--pico-muted-color);
  padding: 2rem 0;
}
</style>
