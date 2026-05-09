import { valorantRoutes } from '@/modules/Riot/valorant/valorant.routes'

export const routes = [
  {
    name: 'riot',
    path: '/riot',
    component: () => import('@/modules/Riot/Riot.vue'),
    meta: {
      requireAuth: true,
    },
    redirect: { name: 'riot-valorant-shop' },
    children: [
      ...valorantRoutes,
    ],
  },
]
