import { routes as almanaxRoutes } from '@/modules/Dofus/almanax/almanax.routes'
import { bankRoutes } from '@/modules/Dofus/bankmanagement/bankmanagement.routes'
import { routes as catalogueRoutes } from '@/modules/Dofus/catalogue/catalogue.routes'
import { hdvRoutes } from '@/modules/Dofus/hdv/hdv.routes'
import { routes as atelierRoutes } from '@/modules/Dofus/workshop/workshop.routes'
import { logsRoutes } from '@/modules/Dofus/logs/logs.routes'

export const routes = [
  {
    name: 'dofus',
    path: '/dofus',
    component: () => import('@/modules/Dofus/Dofus.vue'),
    meta: {
      requireAuth: true,
      desktopOnly: true,
    },
    redirect: { name: 'dofus-almanax' },
    children: [
      ...almanaxRoutes,
      ...catalogueRoutes,
      ...atelierRoutes,
      ...hdvRoutes,
      ...bankRoutes,
      ...logsRoutes,
    ],
  }
]