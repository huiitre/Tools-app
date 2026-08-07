export const catalogRoutes = [
  {
    name: 'palworld-catalog',
    path: 'catalog',
    component: () => import('@/modules/Palworld/catalog/views/PalworldCatalogView.vue'),
    meta: { label: 'Catalogue', requireAuth: true },
  },
]
