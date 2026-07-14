export const tierlistRoutes = [
  {
    name: 'palworld-tierlist',
    path: 'tierlist',
    component: () => import('@/modules/Palworld/tierlist/views/PalworldTierListView.vue'),
    meta: { label: 'Tier List', requireAuth: true },
  },
]
