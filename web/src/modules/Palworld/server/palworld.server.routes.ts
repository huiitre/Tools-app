export const serverRoutes = [
  {
    name: 'palworld-server',
    path: 'server',
    component: () => import('@/modules/Palworld/server/views/PalworldServerDashboard.vue'),
    meta: { label: 'Serveur', requireAuth: true },
  },
]
