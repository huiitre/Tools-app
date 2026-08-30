export const temtemdexRoutes = [
  {
    name: 'temtem_temtemdex',
    path: 'temtemdex',
    component: () => import('@/modules/Temtem/temtemdex/views/TemtemdexView.vue'),
    meta: { label: 'Temtemdex', requireAuth: true },
  },
]
