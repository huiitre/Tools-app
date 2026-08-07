export const itemsRoutes = [
  {
    name: 'palworld-items',
    path: 'items',
    component: () => import('@/modules/Palworld/items/views/PalworldItemsView.vue'),
    meta: { label: 'Objets', requireAuth: true },
  },
]
