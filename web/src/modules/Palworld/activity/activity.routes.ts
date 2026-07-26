export const activityRoutes = [
  {
    name: 'palworld-activity',
    path: 'activity',
    component: () => import('@/modules/Palworld/activity/views/PalworldActivityView.vue'),
    meta: { label: 'Activité serveur', requireAuth: true },
  },
]
