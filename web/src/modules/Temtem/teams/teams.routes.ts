export const temtemTeamsRoutes = [
  {
    name: 'temtem_teams',
    path: 'teams',
    component: () => import('@/modules/Temtem/teams/views/TemtemTeamsView.vue'),
    meta: { label: 'Mes équipes', requireAuth: true },
  },
]
