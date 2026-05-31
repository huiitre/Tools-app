import { r2rRoutes } from '@/modules/EliteDangerous/r2r/r2r.routes'

export const routes = [
  {
    name: 'elite_dangerous',
    path: '/elite_dangerous',
    component: () => import('@/modules/EliteDangerous/EliteDangerous.vue'),
    meta: { requireAuth: true },
    redirect: { name: 'elite_dangerous_r2r' },
    children: [
      ...r2rRoutes,
    ],
  },
]