export const inventoryRoutes = [
  {
    name: 'palworld-inventory',
    path: 'inventory',
    component: () => import('@/modules/Palworld/inventory/views/PalworldInventoryView.vue'),
    meta: { label: 'Mes Pals', requireAuth: true },
  },
]
