export const temtemSimulatorRoutes = [
  {
    name: 'temtem_simulator',
    path: 'simulator',
    component: () => import('@/modules/Temtem/simulator/views/TemtemSimulatorView.vue'),
    meta: { label: 'Simulateur', requireAuth: true, desktopOnly: true },
  },
]
