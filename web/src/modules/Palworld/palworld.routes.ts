import { serverRoutes } from '@/modules/Palworld/server/palworld.server.routes'
import { tierlistRoutes } from '@/modules/Palworld/tierlist/tierlist.routes'
import { paldexRoutes } from '@/modules/Palworld/paldex/paldex.routes'
import { activityRoutes } from '@/modules/Palworld/activity/activity.routes'

export const routes = [
  {
    name: 'palworld',
    path: '/palworld',
    component: () => import('@/modules/Palworld/Palworld.vue'),
    meta: { requireAuth: true },
    redirect: { name: 'palworld-server' },
    children: [
      ...serverRoutes,
      ...tierlistRoutes,
      ...paldexRoutes,
      ...activityRoutes,
    ],
  },
]
