export const r2rRoutes = [
  {
    name: 'elite_dangerous_r2r',
    path: 'r2r',
    component: () => import('@/modules/EliteDangerous/r2r/R2RPage.vue'),
    meta: { label: 'Road to Riches', requireAuth: true },
  },
]