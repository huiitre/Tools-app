export const shopRoutes = [
  {
    name: 'palworld-shop',
    path: 'shop',
    component: () => import('@/modules/Palworld/shop/views/PalworldShopView.vue'),
    meta: { label: 'Marchands', requireAuth: true },
  },
]
