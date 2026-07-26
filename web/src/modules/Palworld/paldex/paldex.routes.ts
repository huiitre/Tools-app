export const paldexRoutes = [
  {
    name: 'palworld-paldex',
    path: 'paldex',
    component: () => import('@/modules/Palworld/paldex/views/PaldexView.vue'),
    meta: { label: 'Paldex', requireAuth: true },
  },
]
